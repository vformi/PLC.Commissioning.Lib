// PythonSink.cs
using Serilog.Core;
using Serilog.Events;
using System;

namespace PLC.Commissioning.Lib
{
    /// <summary>
    /// A Serilog sink that calls a delegate for each log event.
    /// </summary>
    internal class PythonSink : ILogEventSink
    {
        private readonly Action<string, string> _callback;

        public PythonSink(Action<string, string> callback)
        {
            _callback = callback;
        }

        public void Emit(LogEvent logEvent)
        {
            if (_callback == null) return;
            string message = logEvent.RenderMessage();
            string levelStr = logEvent.Level.ToString(); // e.g. "Debug", "Information"
            _callback(levelStr, message);
        }
    }
}