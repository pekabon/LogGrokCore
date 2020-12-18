namespace LogGrokCore.Diagnostics
{
  public static class LogLevelFormatter
  {
      public static string Format(NLog.LogLevel logLevel)
      {
        if (logLevel == NLog.LogLevel.Info)
          return "INF";
        if (logLevel == NLog.LogLevel.Warn)
          return "WRN";
        if (logLevel == NLog.LogLevel.Debug)
          return "DBG";
        if (logLevel == NLog.LogLevel.Error)
          return "ERR";
        if (logLevel == NLog.LogLevel.Fatal)
          return "ERR";
        return "UNK";
      }
  }
}
