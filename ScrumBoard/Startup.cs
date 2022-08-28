using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using JiraApi;
using Domain;
using StorageApi;

[assembly: FunctionsStartup(typeof(ScrumBoard.Startup))]
namespace ScrumBoard
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Dependency Injection: https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
            var serviceProvider = builder.Services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var jiraClient = new JiraClient(
                configuration["JiraUrl"],
                configuration["JiraUser"],
                configuration["JiraPassword"]);

            builder.Services.AddSingleton<IJiraClient>((s) =>
            {
                return jiraClient;
            }).AddSingleton<ISprintInfo>((s) =>
            {
                return new SprintInfo(jiraClient, configuration);
            }).AddSingleton<IStorageClient>((s) =>
            {
                return new StorageClient(configuration["StorageConnectionString"], configuration["StorageContainer"]);
            });
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }
    }
}
