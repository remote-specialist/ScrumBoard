using System.Text.Json;

namespace JiraApi
{
    public class JiraClient : IJiraClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _options;

        public JiraClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _options = new JsonSerializerOptions();
            _options.Converters.Add(new JiraJsonDateTimeConverter());
        }

        public async Task<List<IssueModel>> GetSprintIssuesAsync(int sprintId) => await GetIssuesAsync($"Sprint={sprintId}");

        public async Task<List<IssueModel>> GetSprintStoriesAsync(int sprintId) => await GetIssuesAsync($"Sprint%3D{sprintId}%20AND%20\"Story%20Points\"%20is%20not%20EMPTY");

        public async Task<List<IssueModel>> GetIssuesAsync(string jqlRequest)
        {
            var itemsPerPage = 50;
            var count = 0;

            var allIssues = new List<IssueModel>();


            var continueSearch = true;
            do
            {
                var uri = $"rest/api/3/search?jql={jqlRequest}&startAt={count}&maxResults={itemsPerPage}";
                var response = await _httpClient.GetAsync(uri);
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GetIssuesResponse>(content);

                if (result != null)
                {
                    count += itemsPerPage;

                    var issues = result.Issues;
                    allIssues.AddRange(issues);
                    if (issues.Count < itemsPerPage)
                    {
                        continueSearch = false;
                    }
                }
                else
                {
                    continueSearch = false;
                }
            }
            while (continueSearch);

            return allIssues;
        }

        public async Task<List<WorklogIssueRecord>> GetWorklogRecordsAsync(List<IssueModel> issues, DateTime after)
        {
            var result = new List<WorklogIssueRecord>();
            
            var tasks = new List<Task<List<WorklogModel>>>();
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

            return activeAgileSprints;
        }

        private async Task<List<WorklogModel>> GetWorklogsAsync(IssueModel issue, DateTime after)
        {
            var result = new List<WorklogModel>();

            var worklogs = await GetWorklogsAsync(issue.Key);
            foreach (var worklog in worklogs)
            {
                var updateDate = worklog.Updated;
                var createDate = worklog.Created;

                if ((updateDate > after)
                    ||
                    (createDate > after))
                {
                    result.Add(worklog);
                }
            }

            return result;
        }

        public async Task<List<WorklogModel>> GetWorklogsAsync(string issueKey)
        {
            var itemsPerPage = 50;
            var count = 0;

            var allWorklogs = new List<WorklogModel>();
            var continueSearch = true;
            do
            {
                var uri = $"rest/api/3/issue/{issueKey}/worklog?startAt={count}&maxResults={itemsPerPage}";
                var response = await _httpClient.GetAsync(uri);
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GetWorklogsResponse>(content, _options);
                
                if (result != null)
                {
                    count += itemsPerPage;
                
                    var worklogs = result.Worklogs;
                    allWorklogs.AddRange(worklogs);
                    if (worklogs.Count < itemsPerPage)
                    {
                        continueSearch = false;
                    }
                }
                else
                {
                    continueSearch = false;
                }
            }
            while (continueSearch);

            return allWorklogs;
        }

        private async Task<List<SprintGreenhopper>> GetActiveGreenhopperSprintsAsync(int boardId)
        {
            var uri = $"rest/greenhopper/latest/sprintquery/{boardId}";
            var response = await _httpClient.GetAsync(uri);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GetSprintGreenhopperResponse>(content);
            if(result == null)
            {
                return new List<SprintGreenhopper>();
            }

            return result.Sprints.Where((i => i.State.ToUpper() == "ACTIVE")).ToList();
        }

        private async Task<SprintAgile> GetSprintAsync(int sprintId)
        {
            var uri = $"rest/agile/1.0/sprint/{sprintId}";
            var response = await _httpClient.GetAsync(uri);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SprintAgile>(content, _options);
            return result;
        }
    }
}