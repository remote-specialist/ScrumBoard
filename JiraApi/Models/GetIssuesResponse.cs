using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JiraApi.Models
{
    public class GetIssuesResponse
    {
        [JsonPropertyName("issues")]
        public List<IssueModel>? Issues { get; set; }
    }
}
