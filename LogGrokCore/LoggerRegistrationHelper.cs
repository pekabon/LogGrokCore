using DryIoc;
using NLog;
using NLog.Extensions.Logging;

namespace LogGrokCore
{
    public static class LoggerRegistrationHelper
    {
        private static readonly NLogLoggerProvider LoggerProvider;

        static LoggerRegistrationHelper()
        {
            var logConfigPath = PathHelpers.GetLocalFilePath("nlog.config");
            var logFactory = LogManager.LoadConfiguration(logConfigPath);
            LoggerProvider = new NLogLoggerProvider(new NLogProviderOptions(), logFactory);
        }

        public static void Register(Container container)
        {
            
            container.RegisterInstance(LoggerProvider);
            container.Register(Made.Of(
                r => ServiceInfo.Of<NLogLoggerProvider>(),
                (NLogLoggerProvider f) => f.CreateLogger(Arg.Index<string>(0)),
                request => request.Parent.ImplementationType.Name.ToString()));
        }
    }
}