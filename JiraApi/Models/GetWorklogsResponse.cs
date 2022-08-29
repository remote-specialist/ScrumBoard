using System.Text.Json.Serialization;

namespace JiraApi.Models
{
    public class GetWorklogsResponse
    {
        [JsonPropertyName("worklogs")]
        public List<WorklogModel>? Worklogs { get; set; }
    }
}
