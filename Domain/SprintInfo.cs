using Domain.Models;
using Extensions;
using JiraApi;
using JiraApi.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Web;

namespace Domain
{
    public class SprintInfo : ISprintInfo
    {
        private readonly IJiraClient _jiraClient;
        private readonly IConfiguration _configuration;
        private readonly string _jiraUrl;
        private readonly string _defaultColor;
        
        
        public SprintInfo(IJiraClient jiraClient, IConfiguration configuration)
        {
            _jiraClient = jiraClient;
            _configuration = configuration;
            _jiraUrl = configuration["JiraUrl"];
            _defaultColor = "#3F51B5";
        }

        public async Task<SprintData> GetChordForSprintAsync(SprintAgile sprint)
        {
            // create sprintData to be filled
            var sprintData = new SprintData
            {
                Id = $"{sprint.OriginBoardId}-{sprint.Id}",
                Start = sprint.StartDate,
                End = sprint.EndDate
            };

            // get sprint issues
            var openSprintIssues = await _jiraClient.GetSprintIssuesAsync(sprint.Id);
            
            // get sprint stories
            var storyPointIssues = await _jiraClient.GetSprintStoriesAsync(sprint.Id);

            // get sprint worklogs
            var sprintWorklogRecords = await _jiraClient.GetWorklogRecordsAsync(openSprintIssues, sprintData.Start);

            // set estimate records
            var estimatedIssues = new List<IssueModel>();
            
            // get statuses from configuration
            var doneStatusName = _configuration["JiraDoneStatus"];
            doneStatusName ??= "Done";

            var statuses = _configuration.GetSection("JiraStatuses").Get<List<JiraStatus>>();
            statuses ??= new List<JiraStatus>();

            // fill total issues, done issues, get estimated issues
            foreach (var issue in openSprintIssues)
            {
                var isDone = issue.Fields.Status.Name == doneStatusName;
                
                // update total story points
                var storyPoints = 0M;
                if (storyPointIssues.Any(i => i.Key == issue.Key) && issue.Fields.StoryPoints != null)
                {
                    storyPoints = (decimal) issue.Fields.StoryPoints;
                }
                
                // add to stories key list
                if (storyPoints > 0M)
                {
                    sprintData.TotalStoryPoints += storyPoints;
                    sprintData.TotalStoryPointsIssueKeys.Add(issue.Key);
                }

                // increment total issues count
                ++sprintData.TotalIssues;

                // add to all issues key list
                sprintData.TotalIssuesIssueKeys.Add(issue.Key);
                
                // update list of done issues
                if (isDone)
                {
                    ++sprintData.DoneIssues;
                    sprintData.DoneIssuesIssueKeys.Add(issue.Key);
                    if (storyPoints > 0M)
                    {
                        sprintData.DoneStoryPoints += storyPoints;
                        sprintData.DoneStoryPointsIssueKeys.Add(issue.Key);
                    }
                }
                
                // Fill estimated issues
                var estimateInSeconds = issue.Fields.AggregateTimeEstimate;
                if (estimateInSeconds.HasValue)
                {
                    var subtasks = issue.Fields.Subtasks;
                    
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
                var timeSpentInMinutes = (int)(worklogActivityRecord.GetWorklog().TimeSpentSeconds / 60L);
                var userNodeKey = $"User{user}";
                if (nodes.Any(n => n.Name == userNodeKey))
                {
                    var chordNode = nodes.Single(n => n.Name == userNodeKey);
                    if (!chordNode.IssueKeys.Contains(worklogActivityRecord.GetJiraIssue().Key))
                    {
                        chordNode.IssueKeys.Add(worklogActivityRecord.GetJiraIssue().Key);
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
                            worklogActivityRecord.GetJiraIssue().Key
                        }
                    };

                    nodes.Add(node);
                }

                var issueNodeKey = $"{jiraStatus.Order}-{worklogActivityRecord.GetJiraIssue().Key}"; 

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
                        IssueSummary = worklogActivityRecord.GetJiraIssue().Fields.Summary,
                        Color = jiraStatus.Color != null ? jiraStatus.Color : _defaultColor,
                        IsIssue = true,
                        Name = issueNodeKey,
                        TotalMinutes = timeSpentInMinutes,
                        Displayname = $"{jiraStatus.Name}-{worklogActivityRecord.GetJiraIssue().Key}",
                        Users = new List<string>()
                        {
                            user
                        },
                        IssueKeys = new List<string>()
                        {
                            worklogActivityRecord.GetJiraIssue().Key
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
                var estimateInSeconds = issue.Fields.AggregateTimeEstimate;
                if (estimateInSeconds.HasValue)
                {
                    remainingMinutes = (int)(estimateInSeconds.GetValueOrDefault() / 60.0);
                }

                var issueNodeKey = $"{jiraStatus.Order}-{issue.Key}";
                if (nodes.Any(n => n.Name == issueNodeKey))
                {
                    nodes.Single(n => n.Name == issueNodeKey).RemainingMinutes = remainingMinutes;
                }
                else
                {
                    var node = new SprintNode
                    {
                        IssueSummary = issue.Fields.Summary,
                        Color = jiraStatus.Color != null ? jiraStatus.Color : _defaultColor,
                        IsIssue = true,
                        Name = issueNodeKey,
                        TotalMinutes = 0,
                        RemainingMinutes = remainingMinutes,
                        Displayname = $"{jiraStatus.Name}-{issue.Key}",
                        Users = new List<string>(),
                        IssueKeys = new List<string>()
                        {
                          issue.Key
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

            // fill matrix
            var matrix = new List<List<int>>();
            var iIndex = 0;
            foreach (var iKey in timeLinksKeysOrdered)
            {
                var jIndex = 0;
                var raw = new List<int>();
                foreach (string jKey in timeLinksKeysOrdered)
                {
                    raw.Add(timeLinks[iKey][jKey]);
                    ++jIndex;
                }

                matrix.Add(raw);
                ++iIndex;
            }

            // fill hyperlink and description for nodes
            foreach (var node in nodes)
            {
                var formattedIssueKeys = GetFormattedKeysString(node.IssueKeys);
                node.Hyperlink = node.IsIssue ? _jiraUrl + "browse/" + node.IssueKeys.Single() : _jiraUrl + "issues/?jql=key" + HttpUtility.UrlEncode(" in(" + formattedIssueKeys + ") ORDER BY priority DESC");
                
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

            sprintData.DaysLeft = DateTime.Now.BusinessDaysUntil(sprint.EndDate);
            sprintData.TeamMembersCount = userNodes.Count(m => m.TotalMinutes > 0.25 * averageMinutesByMembers);
            sprintData.HoursSpent = (decimal)(totalSpentMinutes / 60.0);
            sprintData.HoursNeeded = (decimal)(totalRemainingMinutes / 60.0);
            sprintData.ProcessedIssues = nodes.Where(n => n.IsIssue && n.TotalMinutes > 0).ToList().Count;
            sprintData.Description = $"Sprint {sprint.Name}. {sprintData.DaysLeft} day(s) till the end of sprint. "
                + $"Users logged {(decimal)(totalSpentMinutes / 60.0)} hours. Remaining work needs {(decimal)(totalRemainingMinutes / 60.0)} hours more.";
            sprintData.TotalStoryPointsLink = sprintData.TotalStoryPointsIssueKeys.Count == 0 ? _jiraUrl ?? "" : _jiraUrl + "issues/?jql=key" + HttpUtility.UrlEncode(" in(" + GetFormattedKeysString(sprintData.TotalStoryPointsIssueKeys) + ") ORDER BY priority DESC");
            sprintData.DoneStoryPointsLink = sprintData.DoneStoryPointsIssueKeys.Count == 0 ? _jiraUrl ?? "" : _jiraUrl + "issues/?jql=key" + HttpUtility.UrlEncode(" in(" + GetFormattedKeysString(sprintData.DoneStoryPointsIssueKeys) + ") ORDER BY priority DESC");
            sprintData.TotalIssuesLink = sprintData.TotalIssuesIssueKeys.Count == 0 ? _jiraUrl ?? "" : _jiraUrl + "issues/?jql=key" + HttpUtility.UrlEncode(" in(" + GetFormattedKeysString(sprintData.TotalIssuesIssueKeys) + ") ORDER BY priority DESC");
            sprintData.DoneIssuesLink = sprintData.DoneIssuesIssueKeys.Count == 0 ? _jiraUrl ?? "" : _jiraUrl + "issues/?jql=key" + HttpUtility.UrlEncode(" in(" + GetFormattedKeysString(sprintData.DoneIssuesIssueKeys) + ") ORDER BY priority DESC");

            var json = JsonSerializer.Serialize(sprintData);
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

        private JiraStatus GetJiraStatus(List<JiraStatus> configuredStatuses, IssueModel issue)
        {
            string issueStatusName = $"{issue.Fields.Status.Name}";
            
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