using Atlassian.Jira;
using Extensions;
using JiraApi;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Web;

namespace Domain
{
    public class SprintInfo : ISprintInfo
    {
        public readonly IJiraClient _jiraClient;
        public readonly IConfiguration _configuration; 
        
        public SprintInfo(IJiraClient jiraClient, IConfiguration configuration)
        {
            _jiraClient = jiraClient;
            _configuration = configuration;
        }

        public async Task<SprintData> GetChordForSprintAsync(SprintAgile sprint)
        {
            // create sprintData to be filled
            var sprintData = new SprintData
            {
                Id = $"{sprint.OriginBoardId}-{sprint.Id}",
                Start = sprint.GetStart(),
                End = sprint.GetEnd()
            };

            // get sprint issues
            var openSprintIssues = await _jiraClient.GetSprintIssuesAsync(sprint.Id);
            
            // get sprint stories
            var storyPointIssues = await _jiraClient.GetSprintStoriesAsync(sprint.Id);

            // get Jira url
            var url = openSprintIssues.First().Jira.Url;

            // get sprint worklogs
            var sprintWorklogRecords = await _jiraClient.GetWorklogRecordsAsync(openSprintIssues, sprintData.Start);

            // set estimate records
            var estimatedIssues = new List<Issue>();
            
            // get statuses from configuration
            var doneStatusName = _configuration["JiraDoneStatus"];
            doneStatusName ??= "Done";

            var statuses = _configuration.GetSection("JiraStatuses").Get<List<JiraStatus>>();
            statuses ??= new List<JiraStatus>();

            // fill total issues, done issues, get estimated issues
            foreach (var issue in openSprintIssues)
            {
                var isDone = issue.Status.Name == doneStatusName;
                
                // update total story points
                var storyPoints = 0M;
                if (storyPointIssues.Any(i => i.Key.Value == issue.Key.Value))
                {
                    storyPoints = _jiraClient.GetStoryPoints(issue);
                }
                
                // add to stories key list
                if (storyPoints > 0M)
                {
                    sprintData.TotalStoryPoints += storyPoints;
                    sprintData.TotalStoryPointsIssueKeys.Add(issue.Key.Value);
                }

                // increment total issues count
                ++sprintData.TotalIssues;

                // add to all issues key list
                sprintData.TotalIssuesIssueKeys.Add(issue.Key.Value);
                
                // update list of done issues
                if (isDone)
                {
                    ++sprintData.DoneIssues;
                    sprintData.DoneIssuesIssueKeys.Add(issue.Key.Value);
                    if (storyPoints > 0M)
                    {
                        sprintData.DoneStoryPoints += storyPoints;
                        sprintData.DoneStoryPointsIssueKeys.Add(issue.Key.Value);
                    }
                }
                
                // Fill estimated issues
                var estimateInSeconds = issue.TimeTrackingData?.RemainingEstimateInSeconds;
                if (estimateInSeconds.HasValue)
                {
                    var subtasks = await _jiraClient.GetSubTasksAsync(issue);
                    
                    if (estimateInSeconds.GetValueOrDefault() > 0 && subtasks.Count == 0)
                    {
                        estimatedIssues.Add(issue);
                    }
                }
            }

            // create nodes and timeLinks
            var nodes = new List<SprintNode>();
            var timeLinks = new Dictionary<string, Dictionary<string, int>>();

            // iterate worklog activity record and fill nodes
            foreach (var worklogActivityRecord in sprintWorklogRecords)
            {
                // Set Jira status
                var jiraStatus = GetJiraStatus(statuses, worklogActivityRecord.GetJiraIssue());

                // Add user node
                var user = worklogActivityRecord.GetUser();
                var timeSpentInMinutes = (int)(worklogActivityRecord.GetWorklog().TimeSpentInSeconds / 60L);
                var userNodeKey = $"User{user}";
                if (nodes.Any(n => n.Name == userNodeKey))
                {
                    var chordNode = nodes.Single(n => n.Name == userNodeKey);
                    if (!chordNode.IssueKeys.Contains(worklogActivityRecord.GetJiraIssue().Key.Value))
                    {
                        chordNode.IssueKeys.Add(worklogActivityRecord.GetJiraIssue().Key.Value);
                    }

                    chordNode.TotalMinutes += timeSpentInMinutes;
                }
                else
                {
                    var node = new SprintNode
                    {
                        IsIssue = false,
                        Name = userNodeKey,
                        Color = user.GetColor(),
                        TotalMinutes = timeSpentInMinutes,
                        Displayname = user,
                        Users = new List<string>()
                        {
                            user
                        },
                        IssueKeys = new List<string>()
                        {
                            worklogActivityRecord.GetJiraIssue().Key.Value
                        }
                    };

                    nodes.Add(node);
                }

                var issueNodeKey = $"{jiraStatus.Order}-{worklogActivityRecord.GetJiraIssue().Key.Value}"; 

                // Add issue node
                if (nodes.Any(n => n.Name == issueNodeKey))
                {
                    var chordNode = nodes.Single(n => n.Name == issueNodeKey);
                    if (!chordNode.Users.Contains(user))
                    {
                        chordNode.Users.Add(user);
                    }

                    chordNode.TotalMinutes += timeSpentInMinutes;
                }
                else
                {
                    var node = new SprintNode
                    {
                        IssueSummary = worklogActivityRecord.GetJiraIssue().Summary,
                        Color = jiraStatus.Color,
                        IsIssue = true,
                        Name = issueNodeKey,
                        TotalMinutes = timeSpentInMinutes,
                        Displayname = $"{jiraStatus.Name}-{worklogActivityRecord.GetJiraIssue().Key.Value}",
                        Users = new List<string>()
                        {
                            user
                        },
                        IssueKeys = new List<string>()
                        {
                            worklogActivityRecord.GetJiraIssue().Key.Value
                        }
                    };
                    
                    nodes.Add(node);
                }


                // Set timelinks between nodes
                if (!timeLinks.ContainsKey(issueNodeKey))
                {
                    timeLinks[issueNodeKey] = new Dictionary<string, int>()
                    {
                        [userNodeKey] = timeSpentInMinutes
                    };
                }
                else if (!timeLinks[issueNodeKey].ContainsKey(userNodeKey))
                {
                    timeLinks[issueNodeKey][userNodeKey] = timeSpentInMinutes;
                }
                else
                {
                    timeLinks[issueNodeKey][userNodeKey] += timeSpentInMinutes;
                }

                if (!timeLinks.ContainsKey(userNodeKey))
                {
                    timeLinks[userNodeKey] = new Dictionary<string, int>()
                    {
                        [issueNodeKey] = timeSpentInMinutes
                    };
                }
                else if (!timeLinks[userNodeKey].ContainsKey(issueNodeKey))
                {
                    timeLinks[userNodeKey][issueNodeKey] = timeSpentInMinutes;
                }
                else
                {
                    timeLinks[userNodeKey][issueNodeKey] += timeSpentInMinutes;
                }
            }

            // iterate estimated issues and fill nodes
            foreach (var issue in estimatedIssues)
            {
                // Set Jira status
                var jiraStatus = GetJiraStatus(statuses, issue);

                // Get remaining minutes
                int remainingMinutes = 0;
                var estimateInSeconds = issue.TimeTrackingData?.RemainingEstimateInSeconds;
                if (estimateInSeconds.HasValue)
                {
                    remainingMinutes = (int)(estimateInSeconds.GetValueOrDefault() / 60.0);
                }

                var issueNodeKey = $"{jiraStatus.Order}-{issue.Key.Value}";
                if (nodes.Any(n => n.Name == issueNodeKey))
                {
                    nodes.Single(n => n.Name == issueNodeKey).RemainingMinutes = remainingMinutes;
                }
                else
                {
                    var node = new SprintNode
                    {
                        IssueSummary = issue.Summary,
                        Color = jiraStatus.Color,
                        IsIssue = true,
                        Name = issueNodeKey,
                        TotalMinutes = 0,
                        RemainingMinutes = remainingMinutes,
                        Displayname = $"{jiraStatus.Name}-{issue.Key.Value}",
                        Users = new List<string>(),
                        IssueKeys = new List<string>()
                        {
                          issue.Key.Value
                        }
                    };

                    nodes.Add(node);
                }
                if (!timeLinks.ContainsKey(issueNodeKey))
                {
                    timeLinks[issueNodeKey] = new Dictionary<string, int>()
                    {
                        [issueNodeKey] = remainingMinutes
                    };
                }
                else
                {
                    timeLinks[issueNodeKey][issueNodeKey] = remainingMinutes;
                }
            }

            // fill by zeros all other timeLinks
            foreach (var iKey in timeLinks.Keys)
            {
                foreach (var jKey in timeLinks.Keys)
                {
                    if (!timeLinks[iKey].ContainsKey(jKey))
                        timeLinks[iKey][jKey] = 0;
                }
            }

            // order keys
            var timeLinksKeysOrdered = timeLinks.Keys.OrderBy(n => n).ToList();
            
            // order nodes
            nodes = nodes.OrderBy(n => n.Name).ToList();

            // fill matrix data
            var matrix = new int[timeLinksKeysOrdered.Count, timeLinksKeysOrdered.Count];
            var iIndex = 0;
            foreach (var iKey in timeLinksKeysOrdered)
            {
                var jIndex = 0;
                foreach (string jKey in timeLinksKeysOrdered)
                {
                    matrix[iIndex, jIndex] = timeLinks[iKey][jKey];
                    ++jIndex;
                }
                ++iIndex;
            }

            // fill hyperlink and description for nodes
            foreach (var node in nodes)
            {
                var formattedIssueKeys = GetFormattedKeysString(node.IssueKeys);
                node.Hyperlink = node.IsIssue ? url + "browse/" + node.IssueKeys.Single() : url + "issues/?jql=key" + HttpUtility.UrlEncode(" in(" + formattedIssueKeys + ") ORDER BY priority DESC");
                
                if (!node.IsIssue)
                {
                    node.Description = $"{node.Displayname} spent {(Decimal)(node.TotalMinutes / 60.0)} hours on {(Decimal)node.IssueKeys.Count} item(s).";
                }
                else
                {
                    node.Description = $"{(Decimal)(node.TotalMinutes / 60.0)} hours were spent by {node.Users.Count} user(s) on {node.Displayname} {node.IssueSummary}. {(Decimal)(node.RemainingMinutes / 60.0)} hours left.";
                }
            }

            // get total spent\remaining minutes
            int totalSpentMinutes = 0;
            int totalRemainingMinutes = 0;
            foreach (var node in nodes)
            {
                if (node.IsIssue)
                {
                    totalSpentMinutes += node.TotalMinutes;
                    totalRemainingMinutes += node.RemainingMinutes;
                }
            }

            // get averageMinutesByMembers
            List<SprintNode> userNodes = nodes.Where(n => !n.IsIssue).ToList();
            var averageMinutesByMembers = userNodes.Count > 0 ? userNodes.Average(m => m.TotalMinutes) : 0.0;
            
            // fill all the data
            sprintData.Labels = nodes;
            sprintData.Matrix = matrix;
            sprintData.SprintName = sprint.Name;

            sprintData.DaysLeft = DateTime.Now.BusinessDaysUntil(sprint.GetEnd());
            sprintData.TeamMembersCount = userNodes.Count(m => m.TotalMinutes > 0.25 * averageMinutesByMembers);
            sprintData.HoursSpent = (decimal)(totalSpentMinutes / 60.0);
            sprintData.HoursNeeded = (decimal)(totalRemainingMinutes / 60.0);
            sprintData.ProcessedIssues = nodes.Where(n => n.IsIssue && n.TotalMinutes > 0).ToList().Count;
            sprintData.Description = $"Sprint {sprint.Name}. {sprintData.DaysLeft} day(s) till the end of sprint. "
                + $"Users logged {(decimal)(totalSpentMinutes / 60.0)} hours. Remaining work needs {(decimal)(totalRemainingMinutes / 60.0)} hours more.";
            sprintData.TotalStoryPointsLink = sprintData.TotalStoryPointsIssueKeys.Count == 0 ? url ?? "" : url + "issues/?jql=key" + HttpUtility.UrlEncode(" in(" + GetFormattedKeysString(sprintData.TotalStoryPointsIssueKeys) + ") ORDER BY priority DESC");
            sprintData.DoneStoryPointsLink = sprintData.DoneStoryPointsIssueKeys.Count == 0 ? url ?? "" : url + "issues/?jql=key" + HttpUtility.UrlEncode(" in(" + GetFormattedKeysString(sprintData.DoneStoryPointsIssueKeys) + ") ORDER BY priority DESC");
            sprintData.TotalIssuesLink = sprintData.TotalIssuesIssueKeys.Count == 0 ? url ?? "" : url + "issues/?jql=key" + HttpUtility.UrlEncode(" in(" + GetFormattedKeysString(sprintData.TotalIssuesIssueKeys) + ") ORDER BY priority DESC");
            sprintData.DoneIssuesLink = sprintData.DoneIssuesIssueKeys.Count == 0 ? url ?? "" : url + "issues/?jql=key" + HttpUtility.UrlEncode(" in(" + GetFormattedKeysString(sprintData.DoneIssuesIssueKeys) + ") ORDER BY priority DESC");

            var json = JsonConvert.SerializeObject(sprintData);
            return sprintData;
        }

        private static string GetFormattedKeysString(List<string> issueKeys)
        {
            var formatted = string.Empty;
            foreach (var issueKey in issueKeys.Distinct<string>())
            {
                formatted = formatted + "\"" + issueKey + "\",";
            }

            return formatted.Remove(formatted.Length - 1);
        }

        private JiraStatus GetJiraStatus(List<JiraStatus> configuredStatuses, Issue issue)
        {
            string issueStatusName = $"{issue.Status}";
            
            var jiraStatus = configuredStatuses.FirstOrDefault(
                s => string.Equals(s.Name, issueStatusName, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(s.Color) == false
                && string.IsNullOrEmpty(s.Order) == false);

            jiraStatus ??= new JiraStatus
                {
                    Name = issueStatusName,
                    Color = issueStatusName.GetColor(),
                    Order = issueStatusName
                };

            return jiraStatus;
        }
    }
}