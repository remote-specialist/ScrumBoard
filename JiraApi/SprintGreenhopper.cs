using System.Text.Json.Serialization;

namespace JiraApi
{
    public class SprintGreenhopper
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
