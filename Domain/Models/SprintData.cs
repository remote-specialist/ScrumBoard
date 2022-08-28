namespace Domain.Models
{
    public class SprintData
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Description { get; set; } = string.Empty;

        public List<List<int>> Matrix { get; set; } = new List<List<int>>();

        public List<SprintNode> Labels { get; set; } = new List<SprintNode>();

        public string SprintName { get; set; } = string.Empty;

        public int DaysLeft { get; set; } = 0;

        public int TeamMembersCount { get; set; } = 0;

        public decimal HoursSpent { get; set; } = 0M;

        public decimal HoursNeeded { get; set; } = 0M;

        public int ProcessedIssues { get; set; } = 0;

        public decimal TotalStoryPoints { get; set; } = 0M;

        public string TotalStoryPointsLink { get; set; } = string.Empty;

        public List<string> TotalStoryPointsIssueKeys { get; set; } = new List<string>();

        public decimal DoneStoryPoints { get; set; } = 0M;

        public string DoneStoryPointsLink { get; set; } = string.Empty;

        public List<string> DoneStoryPointsIssueKeys { get; set; } = new List<string>();

        public int TotalIssues { get; set; } = 0;

        public string TotalIssuesLink { get; set; } = string.Empty;

        public List<string> TotalIssuesIssueKeys { get; set; } = new List<string>();

        public int DoneIssues { get; set; } = 0;

        public string DoneIssuesLink { get; set; } = string.Empty;

        public List<string> DoneIssuesIssueKeys { get; set; } = new List<string>();
    }
}