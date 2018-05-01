using System;
using Microsoft.Extensions.Logging;
using static System.Diagnostics.Trace;

namespace WebApi
{
    using System.Collections.Concurrent;

    public class TraceLogger : ILogger
    {
        private readonly string name;

        public TraceLogger(string name)
        {
            this.name = name;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = $"{logLevel.ToString()} - {eventId.Id} - {name} - {formatter(state, exception)}";

            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    TraceError(message);
                    break;

                case LogLevel.Warning:
                    TraceWarning(message);
                    break;

                default:
                    TraceInformation(message);
                    break;
            }
        }
    }

    public class TraceLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, TraceLogger> loggers = new ConcurrentDictionary<string, TraceLogger>();

        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, name => new TraceLogger(name));
        }

        public void Dispose()
        {
            loggers.Clear();
        }
    }
}
