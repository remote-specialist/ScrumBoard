namespace Domain.Models
{
    public class SprintTableRow
    {
        public string Name { get; set; } = string.Empty;

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public string TimeFlowUrl { get; set; } = string.Empty;

        public int DaysLeft { get; set; }

        public int TeamMembersCount { get; set; }

        public decimal HoursSpent { get; set; }

        public decimal HoursNeeded { get; set; }

        public decimal HoursSpentPerMember { get; set; }

        public int ProcessedIssues { get; set; }

        public int DoneIssues { get; set; }

        public string DoneIssuesLink { get; set; } = string.Empty;

        public int TotalIssues { get; set; }

        public string TotalIssuesLink { get; set; } = string.Empty;

        public decimal TotalStoryPoints { get; set; }

        public string TotalStoryPointsLink { get; set; } = string.Empty;

        public decimal DoneStoryPoints { get; set; }

        public string DoneStoryPointsLink { get; set; } = string.Empty;
    }
}
