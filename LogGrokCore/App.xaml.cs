using System.Diagnostics;
using System.Globalization;
using System.Text;
using DryIoc;
using LogGrokCore.Diagnostics;
using Splat.DryIoc;

namespace LogGrokCore
{
    public partial class App
    {
        public App()
        {
#if DEBUG
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#endif
            TracesLogger.Initialize();
            ExceptionsLogger.Initialize();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var container = new Container();
            LoggerRegistrationHelper.Register(container);
            container.UseDryIocDependencyResolver();
            RegisterDependencies(container);

            Trace.TraceInformation("Initialization complete.");
        }

        private static void RegisterDependencies(IRegistrator container)
        {
            container.Register<MainWindowViewModel>();
        }
    }
}
