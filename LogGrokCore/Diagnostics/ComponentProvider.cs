using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace LogGrokCore.Diagnostics
{
  public static class ComponentProvider
  {
      public static string DetectCurrentComponent()
      {
        var stackTrace = new StackTrace(false);
        var stackFrames = stackTrace.GetFrames();
        var companyNamespace = stackFrames
            .Select(o => o.GetMethod()?.DeclaringType)
            .Where(o => o != null 
                        && o.Assembly != DiagnosticsAssembly 
                        && o.Namespace != null 
                        && o.Namespace.StartsWith(RootNamespacePrefix))
            .Select(o => o?.Namespace)
            .FirstOrDefault();
        
         return companyNamespace ?? UnknownComponent;  
      }
      
      private static readonly Assembly DiagnosticsAssembly  = typeof(Logger).Assembly;
      private const string RootNamespacePrefix = "LogGrok";
      private const string UnknownComponent = "Unknown";
  }
}
