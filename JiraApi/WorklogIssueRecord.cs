using Atlassian.Jira;
using System.Net.Mail;

namespace JiraApi
{
    public class WorklogIssueRecord
    {
        private readonly Worklog _worklog;
        private readonly Issue _issue;

        public WorklogIssueRecord(Issue jiraIssue, Worklog worklog)
        {
            _issue = jiraIssue;
            _worklog = worklog;
        }

        public string GetActivityInfo() => $"logged {(decimal)(_worklog.TimeSpentInSeconds / 3600.0)} hours";

        public decimal GetLoggedHours() => (decimal)(_worklog.TimeSpentInSeconds / 3600.0);

        public DateTime GetDate() => _worklog.UpdateDate ?? _worklog.StartDate.Value;

        public string GetUser() => new MailAddress(_worklog.AuthorUser.Email.ToLower()).User;

        public Issue GetJiraIssue() => _issue;

        public Worklog GetWorklog() => _worklog;
    }
}