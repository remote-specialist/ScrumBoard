using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JiraApi
{
    public class WorklogModel
    {
        [JsonPropertyName("author")]
        public WorklogAuthor Author { get; set; }
        [JsonPropertyName("created")]
        public DateTime Created { get; set; }
        [JsonPropertyName("updated")]
        public DateTime? Updated { get; set; }
        [JsonPropertyName("timeSpentSeconds")]
        public long TimeSpentSeconds { get; set; }

        public class WorklogAuthor
        {
            [JsonPropertyName("emailAddress")]
            public string EmailAddress { get; set; }
        }
    }
}
