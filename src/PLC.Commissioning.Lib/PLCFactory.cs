using PLC.Commissioning.Lib.Abstractions;
using PLC.Commissioning.Lib.Siemens;
using PLC.Commissioning.Lib.Enums;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

namespace PLC.Commissioning.Lib
{
    /// <summary>
    /// A factory class responsible for creating PLC controllers and configuring logging.
    /// </summary>
    public static class PLCFactory
    {
        private static bool _isLoggerConfigured = false;

        // Default to Debug level via a global LoggingLevelSwitch
        private static LoggingLevelSwitch _levelSwitch =
            new LoggingLevelSwitch(LogEventLevel.Debug);

        /// <summary>
        /// Configures Serilog's logging (console, file, and optional Python sink).
        /// Each call completely re-builds the logger configuration.
        /// </summary>
        /// <param name="pythonCallback">
        ///     If provided, the library will add a Python sink that passes .NET logs
        ///     to this callback (for bridging into Python).
        /// </param>
        /// <param name="writeToConsole">
        ///     If true, writes logs to console (default: true for .NET usage).
        /// </param>
        /// <param name="writeToFile">
        ///     If true, writes logs to a rolling file (default: true).
        /// </param>
        /// <param name="filePath">
        ///     Custom file path. If null, defaults to logs/log_timestamp.txt.
        /// </param>
        /// <param name="logLevel">
        ///     Optional custom log level. If provided, overrides the current level switch.
        /// </param>
        public static void ConfigureLogger(
            Action<string, string> pythonCallback = null,
            bool writeToConsole = true,
            bool writeToFile = true,
            string filePath = null,
            LogLevel? logLevel = null)
        {
            // If user wants to override the log level, update the global switch
            if (logLevel.HasValue)
            {
                _levelSwitch.MinimumLevel = MapLogLevel(logLevel.Value);
            }

            // Build a fresh logger configuration
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_levelSwitch);

            if (writeToConsole)
            {
                loggerConfig.WriteTo.Console();
            }

            if (writeToFile)
            {
                filePath = filePath ?? $"logs/log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                loggerConfig.WriteTo.File(filePath, rollingInterval: RollingInterval.Day);
            }

            if (pythonCallback != null)
            {
                loggerConfig.WriteTo.Sink(new PythonSink(pythonCallback));
            }

            // Create and assign the logger
            Log.Logger = loggerConfig.CreateLogger();
            _isLoggerConfigured = true;
        }

        /// <summary>
        /// Converts our custom LogLevel enum to Serilog's LogEventLevel.
        /// </summary>
        private static LogEventLevel MapLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return LogEventLevel.Verbose;
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Fatal:
                    return LogEventLevel.Fatal;
                default:
                    return LogEventLevel.Debug;
            }
        }

        /// <summary>
        /// Creates a controller of type T for the specified manufacturer.
        /// If no logger is configured yet, calls ConfigureLogger() once with default arguments.
        /// </summary>
        public static T CreateController<T>(Manufacturer manufacturer) where T : class, IPLCController
        {
            // If no one configured the logger yet, do it with defaults
            if (!_isLoggerConfigured)
            {
                ConfigureLogger(); // console + file, no Python sink, default level=Debug
            }

            IPLCController controller;
            switch (manufacturer)
            {
                case Manufacturer.Siemens:
                    controller = new SiemensPLCController();
                    break;
                // other manufacturers...
                default:
                    throw new ArgumentException("Unsupported manufacturer", nameof(manufacturer));
            }

            if (!(controller is T typedController))
            {
                throw new InvalidCastException(
                    $"The controller for {manufacturer} cannot be cast to {typeof(T).Name}");
            }

            return typedController;
        }
    }
}
