//using Microsoft.Diagnostics.EventFlow;
//
// namespace Infrastructure.Logging
// {
//     public static class ApplicationLogging
//     {
//         public static DiagnosticPipeline GetDiagnosticPipeline()
//         {
//             return DiagnosticPipelineFactory.CreatePipeline("eventFlowConfig.json");
//         }
//     }
// }

//         private const string OutputTemplateFormat = "{{Timestamp:yyyy-MM-dd HH:mm:ss.fff}} [{0,18} ] [{{Level,12}}] [{{SourceContext}}] {{Message}}{{NewLine}}{{Exception}}";

//         public static ILogger CreateLogger(
//             Func<string, string> settingsResolver,
//             string serviceName,
//             LoggingLevelSwitch loggingLevelSwitch,
//             string bufferFileLocation,
//             params IDestructuringPolicy[] destructuringPolicies)
//         {
//             if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentNullException(nameof(serviceName));
//             if (loggingLevelSwitch == null) throw new ArgumentNullException(nameof(loggingLevelSwitch));

//             string outputTemplate = string.Format(OutputTemplateFormat, serviceName);

//             var loggerConfiguration = ConfigurationFactory.CreateConfiguration(
//                 settingsResolver: settingsResolver,
//                 serviceName: serviceName,
//                 loggingLevelSwitch: loggingLevelSwitch);

//             if (destructuringPolicies.Length > 0)
//             {
//                 loggerConfiguration = loggerConfiguration.Destructure.With(destructuringPolicies);
//             }

//             return loggerConfiguration
//                 .WriteTo.ColoredConsole(levelSwitch: loggingLevelSwitch, outputTemplate: outputTemplate)
//                 .CreateLogger();
//         }
//     }
// }
