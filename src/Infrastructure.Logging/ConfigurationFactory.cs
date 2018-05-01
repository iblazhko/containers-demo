// using System;
// using Serilog;
// using Serilog.Core;
// using Serilog.Events;

// namespace Infrastructure.Logging
// {
//     public static class ConfigurationFactory
//     {
//         private const string SettingNameLoggingLevel = "LoggingLevel";

//         private const string PropertyNameService = "Service";
//         private const string PropertyNameEnvironmentId = "EnvironmentId";

//         public static LoggerConfiguration CreateConfiguration(
//             Func<string, string> settingsResolver,
//             string serviceName,
//             LoggingLevelSwitch loggingLevelSwitch)
//         {
//             if (settingsResolver == null) throw new ArgumentNullException(nameof(settingsResolver));
//             if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentNullException(nameof(serviceName));
//             if (loggingLevelSwitch == null) throw new ArgumentNullException(nameof(loggingLevelSwitch));

//             var loggingLevel = settingsResolver(SettingNameLoggingLevel);

//             LogEventLevel logLevel;
//             if (Enum.TryParse(loggingLevel, out logLevel))
//             {
//                 loggingLevelSwitch.MinimumLevel = logLevel;
//             }

//             return new LoggerConfiguration()
//                 .MinimumLevel.ControlledBy(loggingLevelSwitch)
//                 .Enrich.WithProperty(PropertyNameService, serviceName);
//         }
//     }
// }
