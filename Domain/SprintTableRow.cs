using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class SprintTableRow
    {
        public string Name { get; set; } = string.Empty;

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public string TimeFlowUrl { get; set; } = string.Empty;

        public int DaysLeft { get; set; }

        public int TeamMembersCount { get; set; }

        public Decimal HoursSpent { get; set; }

        public Decimal HoursNeeded { get; set; }

        public Decimal HoursSpentPerMember { get; set; }

        public int ProcessedIssues { get; set; }

        public int DoneIssues { get; set; }

        public string DoneIssuesLink { get; set; } = string.Empty;

        public int TotalIssues { get; set; }

        public string TotalIssuesLink { get; set; } = string.Empty;

        public Decimal TotalStoryPoints { get; set; }

        public string TotalStoryPointsLink { get; set; } = string.Empty;

        public Decimal DoneStoryPoints { get; set; }

        public string DoneStoryPointsLink { get; set; } = string.Empty;
    }
}
