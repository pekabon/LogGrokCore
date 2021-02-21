using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace LogGrokCore.Diagnostics
{
  public static class FirstChanceExceptionsFilter
  {
      public static bool IsKnown(Exception exception)
      {
          return exception switch
          {
              ArgumentException e => IsKnown(e, KnownArgumentExceptionMethods),
              InvalidOperationException e => IsKnown(e, KnownInvalidOperationExceptionMethods),
              AccessViolationException e => IsKnown(e, KnownAccessViolationExceptionMethods),
              DirectoryNotFoundException e => IsKnown(e, KnownDirectoryNotFoundExceptionMethods),
              Win32Exception e => IsKnown(e, KnownWin32ExceptionMethods),
              _ when exception.GetType().Name == "TypeNameParserException" => true,
              _ => false
          };
      }
      
      private static bool IsKnown(Exception exception , IEnumerable<string> knownMethods)
      {
          return knownMethods.Any(method => 
              exception. StackTrace != null &&
              exception.StackTrace.Contains(method));
      }
      
      private static readonly string[] KnownArgumentExceptionMethods = {
          "System.Drawing.Font.FromLogFont",          
          "System.Windows.Automation.Provider.AutomationInteropProvider.HostProviderFromHandle",          
          "System.Windows.Ink.StrokeCollection",
          "System.Windows.Input.PenThreadWorker.WorkerOperationGetTabletInfo.OnDoWork",
          "MS.Internal.TextFormatting.TextFormatterImp.FormatLine",
          "NLog.Config.Factory`2.CreateInstance"
      };
      
      private static readonly string[]  KnownInvalidOperationExceptionMethods = {      
          "System.Windows.Input.StylusLogic",
          "System.Windows.Window.DragMove",
          "System.Windows.Threading.Dispatcher.WndProcHook",          
      };

      private static readonly string[] KnownAccessViolationExceptionMethods = {
          "MS.Win32.Penimc.UnsafeNativeMethods.GetPenEventMultiple"
      };
      
      private static readonly string[]  KnownDirectoryNotFoundExceptionMethods = {
          "NLog.Internal.FileAppenders.BaseFileAppender.WindowsCreateFile"
      };

      private static readonly string[] KnownWin32ExceptionMethods =
      {
          "MS.Win32.UnsafeNativeMethods.GetWindowText"
      };
  }
}
