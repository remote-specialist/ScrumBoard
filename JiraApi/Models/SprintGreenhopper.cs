using System.Text.Json.Serialization;

namespace JiraApi.Models
{
    public class SprintGreenhopper
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
