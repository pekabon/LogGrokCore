using MahApps.Metro.Controls;
using Splat;

namespace LogGrokCore
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            DataContext = Locator.Current.GetService<MainWindowViewModel>(); ;
            InitializeComponent();
        }
    }
}
