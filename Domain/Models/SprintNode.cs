using System.Text.Json.Serialization;

namespace Domain.Models
{
    public class SprintNode
    {
        public string Color { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool IsIssue { get; set; }

        public string IssueSummary { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Hyperlink { get; set; } = string.Empty;

        public string Displayname { get; set; } = string.Empty;

        [JsonIgnore]
        public List<string> IssueKeys { get; set; } = new List<string>();

        [JsonIgnore]
        public List<string> Users { get; set; } = new List<string>();

        [JsonIgnore]
        public int TotalMinutes { get; set; }

        [JsonIgnore]
        public int RemainingMinutes { get; set; }
    }
}