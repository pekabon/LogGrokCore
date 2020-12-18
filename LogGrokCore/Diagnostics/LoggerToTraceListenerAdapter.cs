using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LogGrokCore.Diagnostics
{
    public class LoggerToTraceListenerAdapter : TraceListener
    {
        private static readonly Logger Logger = Logger.Get(typeof(LoggerToTraceListenerAdapter).Namespace);
        private readonly StringBuilder _message = new();

        public LoggerToTraceListenerAdapter() : base("Logger")
        {
        }
        
        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? msg)
        {
            
            
            var message = msg ?? "<null message>";

            var tracingType = new StackFrame(3, false).GetMethod()?.DeclaringType;
                var callerName = tracingType?.Name.StartsWith("<>") is true ? 
                tracingType.FullName?.Split('.').Last() : tracingType?.Name;
            
            var logger = Logger.Get(callerName);
            
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                return;

            switch (eventType)
            {
                case TraceEventType.Critical:
                    logger.Error(message);
                    break;
                case TraceEventType.Error:
                    logger.Error(message);
                    break;
                case TraceEventType.Warning:
                    logger.Warn(message);
                    break;
                case TraceEventType.Information:
                    logger.Info(message);
                    break;
                case TraceEventType.Verbose:
                    logger.Debug(message);
                    break;
                default:
                    logger.Info(message);
                    break;
            }
        }

        public override void Flush()
        {
            if (_message.Length != 0)
            {
                Console.WriteLine(string.Empty);
            }
        }

        public override void Fail(string? message, string? _)
        {
            Logger.Error(message ?? string.Empty);
        }

        public override void Write(string? message)
        {
            _ = _message.Append(message ?? string.Empty);
        }

        public override void WriteLine(string? message)
        {
            _ = _message.Append(message ?? string.Empty);
            Logger.Info(_message.ToString());
            _ = _message.Clear();
        }
    }
}