using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using DryIoc;
using LogGrokCore.Diagnostics;
using Splat.DryIoc;

namespace LogGrokCore.Bootstrap
{
    public partial class App
    {
        private readonly Container _container;
        public App()
        {
#if DEBUG
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#endif
            TracesLogger.Initialize();
            ExceptionsLogger.Initialize();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _container = new Container();
            LoggerRegistrationHelper.Register(_container);
            _container.UseDryIocDependencyResolver();
            RegisterDependencies(_container);
            
            Trace.TraceInformation("Initialization complete.");

            InitializeComponent();
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = _container.Resolve<MainWindow>();
            mainWindow.Show();
            
            ProcessCommandLine(e.Args.Where(item => item != null));
        }

        private void RegisterDependencies(IRegistrator container)
        {
            container.RegisterDelegate(ApplicationSettings.Load);
            container.Register<MainWindowViewModel>(Reuse.Singleton);
            container.Register<MainWindow>();
        }

        public void ProcessCommandLine(IEnumerable<string> commandLine)
        {            
            var mainVm = _container.Resolve<MainWindowViewModel>();
            foreach (var item in commandLine)
            {
                Dispatcher.BeginInvoke(new Action(() => { mainVm.AddDocument(item); }));
            }
        }
    }
}
