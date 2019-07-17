using System.Text;
using DryIoc;
using Splat.DryIoc;

namespace LogGrokCore
{
    public partial class App
    {
        public App()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var container = new Container();
            container.UseDryIocDependencyResolver();
            RegisterDependencies(container);
        }

        private static void RegisterDependencies(IRegistrator container)
        {
            container.Register<MainWindowViewModel>();
        }
    }
}
