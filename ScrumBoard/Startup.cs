using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using JiraApi;
using Domain;
using StorageApi;
using System.Text;
using Polly.Extensions.Http;
using Polly;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Net;
using System;

[assembly: FunctionsStartup(typeof(ScrumBoard.Startup))]
namespace ScrumBoard
{
    public class Startup : FunctionsStartup
    {
        private static readonly List<HttpStatusCode> TransientHttpStatusCodes =
            new List<HttpStatusCode>
            {
                HttpStatusCode.GatewayTimeout,
                HttpStatusCode.RequestTimeout,
                HttpStatusCode.ServiceUnavailable,
            };

        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Dependency Injection: https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
            var serviceProvider = builder.Services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            var credentials = $"{configuration["JiraUser"]}:{configuration["JiraPassword"]}";
            var byteCredentials = Encoding.UTF8.GetBytes(credentials);

            var jiraClient = builder
                .Services
                .AddHttpClient<IJiraClient, JiraClient>( client =>
                {
                    client.BaseAddress = new Uri(configuration["JiraUrl"]);
                    client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteCredentials));
                })
                .AddPolicyHandler(GetRetryPolicy());

            builder
                .Services
                .AddSingleton<ISprintInfo, SprintInfo>()
                .AddSingleton<IStorageClient>((s) =>
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

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                   .HandleTransientHttpError()
                   .OrResult(msg => TransientHttpStatusCodes.Contains(msg.StatusCode))
                   .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}
