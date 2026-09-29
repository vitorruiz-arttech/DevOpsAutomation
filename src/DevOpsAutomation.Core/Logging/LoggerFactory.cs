using Microsoft.Extensions.Logging;
using Serilog;

namespace DevOpsAutomation.Core.Services
{
    /// <summary>Factory para criar loggers estruturados</summary>
    public static class LoggerFactory
    {
        private static ILoggerFactory loggerFactory;

        static LoggerFactory()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    "logs\\service-.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory()
                .AddSerilog();
        }

        public static ILogger<T> CreateLogger<T>() where T : class
        {
            return loggerFactory.CreateLogger<T>();
        }
    }
}
