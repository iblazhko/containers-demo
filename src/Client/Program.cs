using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;

using Microsoft.Extensions.Configuration;

namespace Client.Random
{
    class Program
    {
        private static readonly Command[] allCommands;
        private static string id;

        static Program()
        {
            var list = new List<Command>();
            foreach (var c in Enum.GetValues(typeof(Command)))
            {
                list.Add((Command)c);
            };

            allCommands = list.ToArray();
        }

        static void Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var configuration = configurationBuilder.Build();
            Func<string, string> settingsResolver = (name) => configuration[name];

            var apiUrl = settingsResolver("ApiUrl");
            var maxDelay = TimeSpan.Parse(settingsResolver("MaxDelay"));
            var maxDelayMs = (int)maxDelay.TotalMilliseconds;
            var rnd = new System.Random();

            Console.WriteLine($"REST API Random Test Client. API Url: {apiUrl}");
            var apiClient = new HttpClient();

            while (true)
            {
                var c = GetRandomCommand();
                Console.WriteLine($"Processing command {c}");

                var request = GetRequest(c, apiUrl);
                try
                {
                    Console.WriteLine($"{request.Method} {request.RequestUri}");
                    var response = apiClient.SendAsync(request).Result;
                    Console.WriteLine($"{response.StatusCode}");
                    
                    Thread.Sleep(rnd.Next(maxDelayMs));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process command {c}: {ex.Message}");
                }
            }
        }

        private static HttpRequestMessage GetRequest(Command c, string apiUrl)
        {
            HttpRequestMessage request;
            switch (c)
            {
                case Command.GetAll:
                    request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}/values");
                    break;

                case Command.Add:
                    request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/values") { Content = new StringContent($"{{ \"value\":\"{DateTime.UtcNow}\" }}", Encoding.UTF8, "application/json") };
                    break;

                case Command.GetById:
                    request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}/values/{id}");
                    break;

                case Command.SetById:
                    request = new HttpRequestMessage(HttpMethod.Put, $"{apiUrl}/values/{id}") { Content = new StringContent($"{{ \"value\":\"{DateTime.UtcNow}\" }}", Encoding.UTF8, "application/json") };
                    break;

                case Command.DeleteById:
                    request = new HttpRequestMessage(HttpMethod.Delete, $"{apiUrl}/values/{id}");
                    id = null;
                    break;

                default:
                    Console.WriteLine($"Command {c} not supported");
                    request = new HttpRequestMessage(HttpMethod.Options, $"{apiUrl}");
                    break;
            }

            return request;
        }

        private static void PrintResponse(HttpResponseMessage response, HttpMethod method, Uri requestUri)
        {
            Console.WriteLine("");
            Console.WriteLine($"RESPONSE ({method} {requestUri}):");
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
            Console.WriteLine("");
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
