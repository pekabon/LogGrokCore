using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace LogGrokCore.Diagnostics
{
  public static class ExceptionsLogger
  {      
      public static void Initialize()
      {
          AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
          AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;     
      }
      
      private static void OnFirstChanceException(object _, FirstChanceExceptionEventArgs args)
      {
          OnException(args.Exception, "first chance exception");
      }
      
      private static void OnUnhandledException(object _, UnhandledExceptionEventArgs args)
      {
          OnException(args.ExceptionObject, "unhandled exception");
      }
      
      private static void OnException(object exceptionObj, string exceptionType)
      {
          var exception = GetException(exceptionObj);
          try
          {
              if (Interlocked.Increment(ref _isProcessingException) >= MaxRecursionDeep ||
                  FirstChanceExceptionsFilter.IsKnown(exception)) return;
              Logger.Error("{0}: {1}", exceptionType, exception); 
              Logger.Flush();
          }
          catch (Exception logException) 
          {
               Debug.WriteLine(
                    "Failed to log {0}: {1}{2}(logException: {3})", 
                    exceptionType,
                    exception,
                    Environment.NewLine,                                    
                    logException);
          }
          finally
          {
            _ = Interlocked.Decrement(ref _isProcessingException);
          }
      }
      
      private static Exception GetException(object exceptionObj)
      {
          return (exceptionObj as Exception) ?? UnknownException;
      }

      private const int MaxRecursionDeep = 3;

      private static readonly Exception UnknownException = new ApplicationException("An unknown exception occurred");
      
      private static readonly Logger Logger = Logger.Get(typeof(ExceptionsLogger).Namespace);
      
      private static int _isProcessingException;
  } 
}
