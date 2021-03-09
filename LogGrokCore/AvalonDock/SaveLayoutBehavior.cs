using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace LogGrokCore.AvalonDock
{
    public static class SaveLayoutBehavior
    {
        public static readonly DependencyProperty SavedLayoutFilePathProperty = DependencyProperty.RegisterAttached(
            "SavedLayoutFilePath", typeof(string), typeof(SaveLayoutBehavior), 
            new PropertyMetadata(default(string),  SavedLayoutFilePathChanged));

        private static void SavedLayoutFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dm = (DockingManager) d;
            if (e.NewValue is string fileName)
            {
                if (File.Exists(fileName))
                {
                    var serializer = new XmlLayoutSerializer(dm);
                    serializer.LayoutSerializationCallback += (o, a) =>

                    {
                        Console.Write(a.ToString());
                    };
                    using var stream = File.OpenRead(fileName);
                    serializer.Deserialize(stream);
                }

                Dispatcher.CurrentDispatcher.ShutdownStarted +=
                    (sender, args) =>
                    {
                        try
                        {
                            var serializer = new XmlLayoutSerializer(dm);

                            using var stream = File.OpenWrite(fileName);
                            serializer.Serialize(stream);
                            

                        }
                        catch (Exception exception)
                        {
                            Trace.TraceError(exception.ToString());
                        }
                    };
            }
        }

        public static void SetSavedLayoutFilePath(DockingManager dm, string value)
        {
            dm.SetValue(SavedLayoutFilePathProperty, value);
        }

        public static string GetSavedLayoutFilePath(DockingManager dm)
        {
            return (string) dm.GetValue(SavedLayoutFilePathProperty);
        }
    }
}