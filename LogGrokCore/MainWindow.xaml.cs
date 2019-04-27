using MahApps.Metro.Controls;
using ReactiveUI;
using Splat;
using System.Windows;

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
