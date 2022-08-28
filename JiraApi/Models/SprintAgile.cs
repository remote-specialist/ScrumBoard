using System.Text.Json.Serialization;

namespace JiraApi.Models
{
    public class SprintAgile
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("originBoardId")]
        public int OriginBoardId { get; set; }
    }
}
