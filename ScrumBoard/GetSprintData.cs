using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain;
using Extensions;
using JiraApi;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StorageApi;

namespace ScrumBoard
{
    public class GetSprintData
    {
        private readonly IJiraClient _jiraClient;
        private readonly ISprintInfo _sprintInfo;
        private readonly IStorageClient _storageClient;
        private readonly IConfiguration _configuration;

        public GetSprintData(IJiraClient jiraClient, 
            ISprintInfo sprintInfo,
            IConfiguration configuration,
            IStorageClient storageClient)
        {
            _jiraClient = jiraClient;
            _sprintInfo = sprintInfo;
            _configuration = configuration;
            _storageClient = storageClient;
        }

        [FunctionName("GetSprintData")]
        public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            if (!DateTime.Now.IsWorkingTime())
            {
                log.LogInformation($"Give me some rest!");
                return;
            }

            log.LogInformation("Download templates");
            var timeFlowTemplate = await _storageClient.DownloadAsync(_configuration["TimeFlowTemplate"]);
            var timeBoardTemplate = await _storageClient.DownloadAsync(_configuration["TimeBoardTemplate"]);
            log.LogInformation("Download completed");

            log.LogInformation("Read information from Jira started");
            var boards = _configuration.GetSection("JiraProjectBoards").Get<List<int>>();
            var sprints = new List<SprintAgile>();
            foreach(var board in boards)
            {
                var boardSprints = await _jiraClient.GetActiveSprintsAsync(board);
                foreach(var boardSprint in boardSprints)
                {
                    if(sprints.TrueForAll(s => s.Id != boardSprint.Id))
                    {
                        sprints.Add(boardSprint);
                    }
                }
            }

            var tasks = new List<Task<SprintData>>();
            foreach(var sprint in sprints)
            {
                tasks.Add(_sprintInfo.GetChordForSprintAsync(sprint));
            }

            var sprintDatas = await Task.WhenAll(tasks);
            log.LogInformation("Read information from Jira completed");

            log.LogInformation("Prepare table data started");
            var storageUrl = _configuration["StorageUrl"];
            var storageContainer = _configuration["StorageContainer"];
            var sprintRows = new List<SprintTableRow>();
            foreach (var sprintData in sprintDatas)
            {
                var htmlView = timeFlowTemplate.FillTemplate(JsonConvert.SerializeObject(sprintData));
                await _storageClient.UploadAsync($"{sprintData.Id}.html", htmlView, "text/html");
                var row = new SprintTableRow()
                {
                    Name = sprintData.SprintName,
                    Start = sprintData.Start,
                    End = sprintData.End,
                    TimeFlowUrl = $"{storageUrl}/{storageContainer}/{sprintData.Id}.html",
                    DaysLeft = sprintData.DaysLeft,
                    TeamMembersCount = sprintData.TeamMembersCount,
                    HoursSpent = sprintData.HoursSpent,
                    HoursSpentPerMember = sprintData.TeamMembersCount > 0 ? sprintData.HoursSpent / (Decimal)sprintData.TeamMembersCount : 0M,
                    HoursNeeded = sprintData.HoursNeeded,
                    ProcessedIssues = sprintData.ProcessedIssues,
                    DoneIssues = sprintData.DoneIssues,
                    DoneIssuesLink = sprintData.DoneIssuesLink,
                    TotalIssues = sprintData.TotalIssues,
                    TotalIssuesLink = sprintData.TotalIssuesLink,
                    DoneStoryPoints = sprintData.DoneStoryPoints,
                    DoneStoryPointsLink = sprintData.DoneStoryPointsLink,
                    TotalStoryPoints = sprintData.TotalStoryPoints,
                    TotalStoryPointsLink = sprintData.TotalStoryPointsLink
                };
              
                sprintRows.Add(row);
            }

            log.LogInformation("Prepare table data competed");
            var tableData = JsonConvert.SerializeObject(sprintRows);

            log.LogInformation("Upload table data results");
            await _storageClient.UploadAsync("board.html", timeBoardTemplate.FillTemplate(tableData), "text/html");

            log.LogInformation($"C# Timer trigger function completed at: {DateTime.Now}");
        }
    }
}