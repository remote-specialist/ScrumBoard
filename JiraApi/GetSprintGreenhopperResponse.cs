using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JiraApi
{
    public class GetSprintGreenhopperResponse
    {
        [JsonPropertyName("sprints")]
        public List<SprintGreenhopper> Sprints { get; set; }
    }
}
