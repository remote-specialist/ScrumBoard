using System.Net.Mail;

namespace JiraApi
{
    public class WorklogIssueRecord
    {
        private readonly WorklogModel _worklog;
        private readonly IssueModel _issue;

        public WorklogIssueRecord(IssueModel jiraIssue, WorklogModel worklog)
        {
            _issue = jiraIssue;
            _worklog = worklog;
        }

        public string GetActivityInfo() => $"logged {(decimal)(_worklog.TimeSpentSeconds / 3600.0)} hours";

        public decimal GetLoggedHours() => (decimal)(_worklog.TimeSpentSeconds / 3600.0);

        public DateTime GetDate() => _worklog.Updated ?? _worklog.Created;

        public string GetUser() => new MailAddress(_worklog.Author.EmailAddress.ToLower()).User;

        public IssueModel GetJiraIssue() => _issue;

        public WorklogModel GetWorklog() => _worklog;
    }
}