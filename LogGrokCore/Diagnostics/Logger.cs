using System.Collections.Concurrent;
using System.Reflection;
using NLog;

namespace LogGrokCore.Diagnostics
{
  public class Logger
  {
      private readonly string _component;
      private static readonly ConcurrentDictionary<string, Logger> LoggersCache = new();
      private static readonly NLog.Logger NLogLogger;      

      static Logger()
      {
          NLogLogger = LogManager.GetCurrentClassLogger();
          GlobalDiagnosticsContext.Set("EntryAssembly", Assembly.GetEntryAssembly()?.FullName);
      }
      
      private Logger (string component)
      {
          _component = component;
      }
      
      public static Logger Get(string? component = null)
      {
          var comp = component ?? ComponentProvider.DetectCurrentComponent();
          return LoggersCache.GetOrAdd(comp, c => new Logger(c));
      }
      
      public static void FlushAll() => LogManager.Flush();
            
      public void Debug(string message,  params object[] args) => Log(LogLevel.Debug, message, args); 
     
      public void Info(string message,  params object[] args) => Log(LogLevel.Info, message, args);

      public void Warn(string message, params object[] args) => Log(LogLevel.Warn, message, args);
      
      public void Error(string message, params object[] args) => Log(LogLevel.Error, message, args);

      public void Flush() => LogManager.Flush();
      
      private void Log( LogLevel level, string message, object[] args) 
      {
          var logEvent = LogEventInfo.Create(level, NLogLogger.Name, null, message, args);
          
          logEvent.Properties["component"] = _component;
          logEvent.Properties["levelShort"] = LogLevelFormatter.Format(level);
          
          NLogLogger.Log(logEvent);                    
      }
  }
}
