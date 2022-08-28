using Atlassian.Jira;

namespace JiraApi
{
    public interface IJiraClient
    {
        Task<List<Issue>> GetSprintIssuesAsync(int sprintId);
        Task<List<Issue>> GetSprintStoriesAsync(int sprintId);
        Task<List<Issue>> GetIssuesAsync(string jqlRequest);
        Task<List<Issue>> GetSubTasksAsync(Issue jiraIssue);
        Task<List<WorklogIssueRecord>> GetWorklogRecordsAsync(List<Issue> issues, DateTime after);
        //Task<List<Worklog>> GetWorklogsAsync(Issue issue, DateTime after);//private?
        //Task<List<Worklog>> GetWorklogsAsync(Issue issue);//private?
        Task<List<SprintAgile>> GetActiveSprintsAsync(int boardId);
        decimal GetStoryPoints(Issue issue);
    }
}
