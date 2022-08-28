using System.Text.Json.Serialization;

namespace JiraApi
{
    public class IssueModel
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
        [JsonPropertyName("fields")]
        public IssueFields Fields { get; set; } = new IssueFields();
        

        public class IssueFields
        {
            [JsonPropertyName("summary")]
            public string Summary { get; set; } = string.Empty;

            [JsonPropertyName("status")]
            public IssueStatus Status { get; set; }

            [JsonPropertyName("aggregatetimeestimate")]
            public long? AggregateTimeEstimate { get; set; }

            [JsonPropertyName("subtasks")]
            public List<IssueSubtask> Subtasks { get; set; }

            // set Story Points field id
            [JsonPropertyName("customfield_10026")]
            public decimal? StoryPoints { get; set; } = 0;

            public class IssueStatus
            {
                [JsonPropertyName("name")]
                public string? Name { get; set; }
            }

            public class IssueSubtask
            {
                [JsonPropertyName("key")]
                public string Key { get; set; } = string.Empty;

                [JsonPropertyName("summary")]
                public string Summary { get; set; } = string.Empty;
            }
        }
    }
}
