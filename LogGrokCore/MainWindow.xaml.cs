using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using MahApps.Metro.Controls;
using Splat;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace LogGrokCore
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            Closing += SaveLayout;
            Loaded += LoadLayoout;

            DataContext = Locator.Current.GetService<MainWindowViewModel>();
            InitializeComponent();
        }

        private void LoadLayoout(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(GetSettingsFileName())) return;
            var layoutSerializer = new XmlLayoutSerializer(DockingManager);
            using var reader = new StreamReader(GetSettingsFileName());
            layoutSerializer.LayoutSerializationCallback += (o, args) =>
            {
                if (args.Model is LayoutDocument)
                    args.Cancel = true;
            };
            layoutSerializer.Deserialize(reader);
        }

        private void SaveLayout(object sender, CancelEventArgs e)
        {
            var serializer = new XmlLayoutSerializer(DockingManager);
            using var writer = new StreamWriter(GetSettingsFileName());
            
            serializer.Serialize(writer);
        }

        private string GetSettingsFileName()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Process.GetCurrentProcess().ProcessName);
        }
    }
}