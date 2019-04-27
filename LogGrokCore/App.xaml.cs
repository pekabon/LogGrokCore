using Splat;
using System.Windows;
using DryIoc;
using Splat.DryIoc;

namespace LogGrokCore
{
    public partial class App : Application
    {
        public App()
        {
            var container = new Container();
            container.UseDryIocDependencyResolver();
            RegisterDependencies(container);
        }

        private void RegisterDependencies(Container container)
        {
            container.Register<MainWindowViewModel>();
        }
    }
}
