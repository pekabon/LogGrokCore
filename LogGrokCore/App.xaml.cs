using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text;
using DryIoc;
using LogGrokCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Splat.DryIoc;

namespace LogGrokCore
{
    public class ColorSettings
    {
        public object[]? Rules { get; set; }
    }

    public partial class App
    {
        private ColorSettings _olorSettings = new ColorSettings();
        private IConfigurationRoot _configuration;

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
            
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true);
            
            _configuration = builder.Build();
            
            _configuration.GetSection("ColorSettings").Bind(_olorSettings);

            ChangeToken.OnChange(() => _configuration.GetReloadToken(), () =>
            {
                //_configuration.GetSection("ColorSettings").Bind(_olorSettings);
                Console.Write(_olorSettings.Rules?.Length);
            });

            Trace.TraceInformation("Initialization complete.");
        }

        private static void RegisterDependencies(IRegistrator container)
        {
            
            
            container.Register<MainWindowViewModel>();
        }
    }
}
