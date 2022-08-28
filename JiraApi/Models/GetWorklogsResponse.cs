using System.Text.Json.Serialization;

namespace JiraApi.Models
{
    public class GetWorklogsResponse
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
        [JsonPropertyName("worklogs")]
        public List<WorklogModel> Worklogs { get; set; } = new List<WorklogModel>();
    }
}
