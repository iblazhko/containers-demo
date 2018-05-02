using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using Client.Random;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Extensions.Configuration;
using static System.Diagnostics.Trace;

namespace Client
{
    using System.Reflection;

    internal static class Program
    {
        private static readonly Command[] allCommands;
        private static string id;

        static Program()
        {
            var list = new List<Command>();
            foreach (var c in Enum.GetValues(typeof(Command)))
            {
                list.Add((Command)c);
            }

            allCommands =  list.ToArray();
        }

        private static void Main()
        {
            using(DiagnosticPipelineFactory.CreatePipeline("eventFlowConfig.json"))
            {
                var configurationBuilder = new ConfigurationBuilder()
                    .SetBasePath(AssemblyDirectory)
                    .AddJsonFile("appsettings.json");

                var configuration = configurationBuilder.Build();
                string SettingsResolver(string name) => configuration[name];
                
                var apiUrl = SettingsResolver("ApiUrl");
                var maxDelay = TimeSpan.Parse(SettingsResolver("MaxDelay"));
                var maxDelayMs = (int)maxDelay.TotalMilliseconds;
                
                var rnd = new System.Random();

                TraceInformation($"REST API Random Test Client. API Url: {apiUrl}");
                
                var apiClient = new HttpClient();
                
                while (true)
                {
                    var c = GetRandomCommand();
                    TraceInformation($"Processing command {c}");
                    var request = GetRequest(c, apiUrl);
                    try
                    {
                        TraceInformation($"{request.Method} {request.RequestUri}");
                        var response = apiClient.SendAsync(request).Result;                        
                        TraceResponse(response, request.Method, request.RequestUri);
                    
                        Thread.Sleep(rnd.Next(maxDelayMs));                        
                    }
                    catch (Exception ex)
                    {
                        TraceError($"Failed to process command {c}: {ex.GetAllMessages()}");                        
                    }                    
                }
            }

            // ReSharper disable once FunctionNeverReturns
        }

        private static HttpRequestMessage GetRequest(Command c, string apiBaseUrl)
        {
            const string valuesApiUrl = "api/values";
            
            HttpRequestMessage request;
            switch (c)
            {
                case Command.GetAll:
                    request = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/{valuesApiUrl}");
                    break;

                case Command.Add:
                    request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl}/{valuesApiUrl}") { Content = new StringContent($"{{ \"value\":\"{DateTime.UtcNow}\" }}", Encoding.UTF8, "application/json") };
                    break;

                case Command.GetById:
                    request = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/{valuesApiUrl}/{id}");
                    break;

                case Command.SetById:
                    request = new HttpRequestMessage(HttpMethod.Put, $"{apiBaseUrl}/{valuesApiUrl}/{id}") { Content = new StringContent($"{{ \"value\":\"{DateTime.UtcNow}\" }}", Encoding.UTF8, "application/json") };
                    break;

                case Command.DeleteById:
                    request = new HttpRequestMessage(HttpMethod.Delete, $"{apiBaseUrl}/{valuesApiUrl}/{id}");
                    id = null;
                    break;

                default:
                    TraceError($"Command {c} not supported");
                    request = new HttpRequestMessage(HttpMethod.Options, $"{apiBaseUrl}");
                    break;
            }

            return request;
        }

        private static void TraceResponse(HttpResponseMessage response, HttpMethod method, Uri requestUri)
        {
            var statusCode = response.StatusCode;
            var content = response.Content.ReadAsStringAsync().Result;
            TraceInformation($"RESPONSE ({method} {requestUri}): {statusCode} {content}");
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

        private static Command GetRandomCommand()
        {
            Command? c;

            do
            {
                c = allCommands.TakeRandom();
                switch (c.Value)
                {
                    case Command.GetAll:
                        break;

                    case Command.Add:
                        id = Guid.NewGuid().ToString();
                        break;

                    case Command.DeleteById:
                    case Command.GetById:
                    case Command.SetById:
                        if (string.IsNullOrEmpty(id)) c = null;
                        break;

                    // ReSharper disable once RedundantEmptySwitchSection
                    default:
                        break;
                }
            } while (!c.HasValue);

            return c.Value;
        }

        private enum Command
        {
            GetAll,
            Add,
            GetById,
            SetById,
            DeleteById
        }
    }
}
