﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JiraApi
{
    public class GetIssuesResponse
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
        [JsonPropertyName("issues")]
        public List<IssueModel> Issues { get; set; } = new List<IssueModel>();
    }
}