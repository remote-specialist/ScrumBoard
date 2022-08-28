using Atlassian.Jira;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Timeout;
using RestSharp;
using System.Globalization;

namespace JiraApi
{
    public class JiraClient : IJiraClient
    {
        private readonly Jira _api;
        private readonly Policy _defaultRetryPolicy;

        public JiraClient(string url, string user, string password)
        {
            var settings = new JiraRestClientSettings();
            settings.CustomFieldSerializers["com.atlassian.jira.plugin.system.customfieldtypes:userpicker"] = new Atlassian.Jira.Remote.SingleObjectCustomFieldValueSerializer("displayName");
            settings.CustomFieldSerializers["com.atlassian.jira.plugin.system.customfieldtypes:multiuserpicker"] = new Atlassian.Jira.Remote.MultiObjectCustomFieldValueSerializer("displayName");

            _api = Jira.CreateRestClient(url, user, password, settings);
            _api.Issues.MaxIssuesPerRequest = 1000;

            var policyTimeout = Policy.Timeout(600, TimeoutStrategy.Pessimistic);
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    25,
                    attempt => TimeSpan.FromMilliseconds(1000));

            _defaultRetryPolicy = Policy.Wrap(policyTimeout, retryPolicy);
        }

        public decimal GetStoryPoints(Issue issue)
        {
            decimal storyPoints;
            try
            {
                decimal.TryParse(
                    issue.CustomFields["Story Points"].Values.First(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out storyPoints);
            }
            catch
            {
                storyPoints = 0M;
            }

            return storyPoints;
        }

        public async Task<List<Issue>> GetSprintIssuesAsync(int sprintId) => await GetIssuesAsync($"Sprint = {sprintId}");

        public async Task<List<Issue>> GetSprintStoriesAsync(int sprintId) => await GetIssuesAsync($"Sprint = {sprintId} AND \"Story Points\" is not EMPTY");

        public async Task<List<Issue>> GetIssuesAsync(string jqlRequest)
        {
            var issuesPerPage = 50;
            var count = 0;

            var allIssues = new List<Issue>();

            var totalIssues = 0;
            await _defaultRetryPolicy.Execute(
                async () =>
                {
                    var records = await _api.Issues.GetIssuesFromJqlAsync(jqlRequest, issuesPerPage);
                    totalIssues = records.TotalItems;
                    allIssues.AddRange(records.ToList());
                });

            while (allIssues.Count < totalIssues)
            {
                count += issuesPerPage;
                await _defaultRetryPolicy.Execute(
                    async () =>
                    {
                        var records = await _api.Issues.GetIssuesFromJqlAsync(jqlRequest, issuesPerPage, count);
                        allIssues.AddRange(records);
                    });
            }

            return allIssues;
        }

        public async Task<List<Issue>> GetSubTasksAsync(Issue jiraIssue)
        {
            var result = new List<Issue>();
            await _defaultRetryPolicy.Execute(
                async () =>
                    {
                        var subTasks = await jiraIssue.GetSubTasksAsync();
                        result.AddRange(subTasks);
                    });

            return result;
        }

        public async Task<List<WorklogIssueRecord>> GetWorklogRecordsAsync(List<Issue> issues, DateTime after)
        {
            var result = new List<WorklogIssueRecord>();
            
            var tasks = new List<Task<List<Worklog>>>();
            for(var i = 0; i < issues.Count; i++)
            {
                tasks.Add(GetWorklogsAsync(issues[i], after));
            }

            var worklogs = await Task.WhenAll(tasks);
            for (var i = 0; i < issues.Count; i++)
            {
                foreach (var worklog in worklogs[i])
                {
                    result.Add(new WorklogIssueRecord(issues[i], worklog));
                }
            }

            return result;
        }

        public async Task<List<SprintAgile>> GetActiveSprintsAsync(int boardId)
        {
            var activeAgileSprints = new List<SprintAgile>();

            await _defaultRetryPolicy.Execute(
                async () =>
                {
                    var sprints = new List<SprintAgile>();
                    var greenhopperSprints = await GetActiveGreenhopperSprintsAsync(boardId);
                    foreach (var sprintGreenhopper in greenhopperSprints)
                    {
                        var sprint = await GetSprintAsync(sprintGreenhopper.Id);
                        if (sprint.OriginBoardId == boardId)
                        {
                            sprints.Add(sprint);
                        }
                    }

                    activeAgileSprints.AddRange(sprints);
                });

            return activeAgileSprints;
        }

        private async Task<List<Worklog>> GetWorklogsAsync(Issue issue, DateTime after)
        {
            var result = new List<Worklog>();

            var worklogs = await GetWorklogsAsync(issue);
            foreach (var worklog in worklogs)
            {
                var updateDate = worklog.UpdateDate;
                var createDate = worklog.CreateDate;

                if ((updateDate.HasValue && updateDate.GetValueOrDefault() > after)
                    ||
                    (createDate.HasValue && createDate.GetValueOrDefault() > after))
                {
                    result.Add(worklog);
                }
            }

            return result;
        }

        private async Task<List<Worklog>> GetWorklogsAsync(Issue issue)
        {
            var result = new List<Worklog>();
            await _defaultRetryPolicy.Execute(
                async () =>
                {
                    var worklogs = await issue.GetWorklogsAsync();
                    result.AddRange(worklogs);
                });

            return result;
        }

        private async Task<List<SprintGreenhopper>> GetActiveGreenhopperSprintsAsync(int boardId)
        {
            var restSharpClient = _api.RestClient.RestSharpClient;
            var response = await restSharpClient.ExecuteAsync(
                    new RestRequest($"{restSharpClient.BaseUrl}rest/greenhopper/latest/sprintquery/{boardId}", Method.GET));

            return JObject.Parse(response.Content)
                ["sprints"].ToObject<List<SprintGreenhopper>>().Where((i => i.State.ToUpper() == "ACTIVE")).ToList();
        }

        private async Task<SprintAgile> GetSprintAsync(int sprintId)
        {
            var restSharpClient = _api.RestClient.RestSharpClient;
            var response = await restSharpClient.ExecuteAsync(new RestRequest($"{restSharpClient.BaseUrl}rest/agile/1.0/sprint/{sprintId}", Method.GET));
            return JsonConvert.DeserializeObject<SprintAgile>(response.Content);
        }
    }
}