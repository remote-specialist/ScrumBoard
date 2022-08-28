namespace JiraApi
{
    public interface IJiraClient
    {
        Task<List<IssueModel>> GetSprintIssuesAsync(int sprintId);
        Task<List<IssueModel>> GetSprintStoriesAsync(int sprintId);
        Task<List<IssueModel>> GetIssuesAsync(string jqlRequest);
        Task<List<WorklogIssueRecord>> GetWorklogRecordsAsync(List<IssueModel> issues, DateTime after);
        Task<List<SprintAgile>> GetActiveSprintsAsync(int boardId);
    }
}
