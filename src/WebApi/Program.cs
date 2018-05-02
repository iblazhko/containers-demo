using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Extensions.Configuration;
using static System.Diagnostics.Trace;

namespace WebApi
{
    using System;
    using System.Reflection;

    internal static class Program
    {
        public static void Main()
        {
            var appDir = AssemblyDirectory;
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(appDir)
                .AddJsonFile("appsettings.json");

            var configuration = configurationBuilder.Build();
            string SettingsResolver(string name) => configuration[name];

            var apiUrl = SettingsResolver("ApiUrl");

            MongoDbConfiguration.ServerAddress = SettingsResolver("MongoDB.ServerAddress");
            MongoDbConfiguration.ServerPort = int.Parse(SettingsResolver("MongoDB.ServerPort"));
            MongoDbConfiguration.DatabaseName = SettingsResolver("MongoDB.DatabaseName");
            MongoDbConfiguration.UserName = SettingsResolver("MongoDB.UserName");
            MongoDbConfiguration.UserPassword = SettingsResolver("MongoDB.UserPassword");

            using(DiagnosticPipelineFactory.CreatePipeline("eventFlowConfig.json"))
            {
                var host = new WebHostBuilder()
                    .UseUrls(apiUrl)
                    .UseKestrel()
                    .UseContentRoot(appDir)
                    .UseStartup<Startup>()
                    .Build();

                TraceInformation($"WebAPI starting at {apiUrl}");
                TraceInformation($"Using MongoDB at {MongoDbConfiguration.ServerAddress}:{MongoDbConfiguration.ServerPort}/{MongoDbConfiguration.DatabaseName}");

                host.Run();
            }
        }

        private static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);

                return Path.GetDirectoryName(path);
            }
        }

    }
}
