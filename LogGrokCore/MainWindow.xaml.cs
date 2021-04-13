using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using LogGrokCore.AvalonDock;
using Splat;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace LogGrokCore
{
    public static class Constants
    {
        public const string MarkedLinesContentId = "###MarkedLinesContentId";

        public const string DefaultAvalonDockLayout =
            @"<?xml version=""1.0"" encoding=""utf-8""?>
                <LayoutRoot>
                    <RootPanel Orientation=""Horizontal"">
                    <LayoutDocumentPane />
                    <LayoutAnchorablePane DockWidth=""300"" DocMinWidth=""300"">
                    <LayoutAnchorable 
                        AutoHideMinWidth=""100"" 
                        AutoHideMinHeight=""100"" 
                        Title=""Marked lines"" 
                        IsSelected=""True"" 
                        ContentId=""###MarkedLinesContentId"" 
                        CanClose=""False"" />
                    </LayoutAnchorablePane>
                    </RootPanel>
                    <TopSide />
                    <RightSide />
                    <LeftSide />
                    <BottomSide />
                    <FloatingWindows />
                    <Hidden />
        </LayoutRoot>";
    }

    public partial class MainWindow
    {
        public MainWindow(MainWindowViewModel mainWindowViewModel)
        {
            DataContext = mainWindowViewModel;
            Closing += SaveLayout;
            Loaded += LoadLayoout;

            InitializeComponent();
        }

        private void LoadLayoout(object sender, RoutedEventArgs e)
        {
            var settingsFileName = GetSettingsFileName();
            using var reader =
                File.Exists(settingsFileName)
                    ? new StreamReader(settingsFileName)
                    : new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(Constants.DefaultAvalonDockLayout)));
            
            var contentProvider = (IContentProvider) DataContext;
            var layoutSerializer = new XmlLayoutSerializer(DockingManager);
            
            layoutSerializer.LayoutSerializationCallback += (_, args) =>
            {
                if (args.Model is LayoutDocument)
                    args.Cancel = true;

                var content = contentProvider.GetContent(args.Model.ContentId);
                if (content != null)
                    args.Content = new ContentControl { Content = content };
            };
            layoutSerializer.Deserialize(reader);
        }

        private void SaveLayout(object sender, CancelEventArgs e)
        {
            var fileName = GetSettingsFileName();
            var serializer = new XmlLayoutSerializer(DockingManager);
            using var writer = new StreamWriter(fileName);
            
            serializer.Serialize(writer);
        }

        private string GetSettingsFileName()
        {
            return Path.Combine(EnsureDirectoryExists(), "layout.settings");
        }

        private static string EnsureDirectoryExists()
        {
            var dirName  = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.GetFileNameWithoutExtension(Assembly.GetCallingAssembly().Modules.First().Name),
                "LayoutSettings");
            Directory.CreateDirectory(dirName);
            return dirName;
        }
    }
}