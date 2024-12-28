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
    /// Factory class responsible for creating PLC controllers based on the specified manufacturer.
    /// </summary>
    public class PLCFactory
    {
        private static LoggingLevelSwitch _levelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug); // Default to Debug
        private static bool _isLoggerConfigured = false;

        /// <summary>
        /// Configures the logger for the first time, setting up sinks.
        /// </summary>
        private static void ConfigureLogger()
        {
            if (!_isLoggerConfigured)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch) // Use the level switch
                    .WriteTo.Console()
                    .WriteTo.File($"logs/log_{DateTime.Now:yyyyMMdd_HHmmss}.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                _isLoggerConfigured = true;
            }
        }

        /// <summary>
        /// Maps the custom LogLevel enum to Serilog's LogEventLevel.
        /// </summary>
        /// <param name="level">The custom log level.</param>
        /// <returns>The corresponding Serilog log event level.</returns>
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
        /// Sets the logging level dynamically.
        /// </summary>
        /// <param name="level">The new logging level.</param>
        private static void SetLogLevel(LogLevel level)
        {
            _levelSwitch.MinimumLevel = MapLogLevel(level);
        }

        /// <summary>
        /// Creates a controller instance of type <typeparamref name="T"/> for the specified <see cref="Manufacturer"/>.
        /// </summary>
        /// <typeparam name="T">The type of PLC controller that implements <see cref="IPLCController"/>.</typeparam>
        /// <param name="manufacturer">The manufacturer of the PLC to create.</param>
        /// <param name="logLevel">Optional: The logging level to set.</param>
        /// <returns>An instance of type <typeparamref name="T"/> corresponding to the specified manufacturer.</returns>
        /// <exception cref="ArgumentException">Thrown when an unsupported manufacturer is provided.</exception>
        /// <exception cref="InvalidCastException">Thrown when the created controller cannot be cast to the type <typeparamref name="T"/>.</exception>
        public static T CreateController<T>(Manufacturer manufacturer, LogLevel? logLevel = null) where T : class, IPLCController
        {
            ConfigureLogger();

            // Update log level if specified
            if (logLevel.HasValue)
            {
                SetLogLevel(logLevel.Value);
            }

            IPLCController controller;

            switch (manufacturer)
            {
                case Manufacturer.Siemens:
                    controller = new SiemensPLCController();
                    break;
                // Add cases for other manufacturers here:
                // case Manufacturer.Beckhoff:
                //     controller = new BeckhoffPLCController();
                //     break;
                // case Manufacturer.Rockwell:
                //     controller = new RockwellPLCController();
                //     break;
                default:
                    throw new ArgumentException("Unsupported manufacturer", nameof(manufacturer));
            }

            if (!(controller is T typedController))
            {
                throw new InvalidCastException($"The controller for {manufacturer} cannot be cast to {typeof(T).Name}");
            }

            return typedController;
        }
    }
}
