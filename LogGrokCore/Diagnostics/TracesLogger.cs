using System.Diagnostics;
using System.Linq;

namespace LogGrokCore.Diagnostics
{
  public static class TracesLogger
  {
      public static void Initialize()
      {
          var defaultTraceListeners = Trace.Listeners.OfType<DefaultTraceListener>().ToList();
          foreach (var listener in defaultTraceListeners)
            Trace.Listeners.Remove(listener);
          
          _ = Trace.Listeners.Add(new LoggerToTraceListenerAdapter());
      }
  }
}
