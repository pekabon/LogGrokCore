using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LogGrokCore.Controls
{

    public static class DataObjectExtensions
    {
        public static IEnumerable<string> GetFiles(this DataObject dataObject)
        {
            if (dataObject.ContainsFileDropList())
                return GetFilesFromDropList(dataObject);

           
            return Enumerable.Empty<string>();
        }

        private static IEnumerable<string> GetFilesFromDropList(DataObject dataObject)
        {
            if (!dataObject.ContainsFileDropList())
                throw new InvalidOperationException("Data object doesn't contain file drop list");

            return dataObject
                .GetFileDropList()
                .Cast<string>()
                .SelectMany(fileSystemEntry =>
                {
                    if (Directory.Exists(fileSystemEntry))
                        return Directory.EnumerateFiles(fileSystemEntry, "*", SearchOption.AllDirectories);

                    return
                        File.Exists(fileSystemEntry) ? fileSystemEntry.Yield() : Enumerable.Empty<string>();
                });
        }
    }


    public class FileDroppedEventArgs : RoutedEventArgs
    {
        public FileDroppedEventArgs(RoutedEvent routedEvent, IEnumerable<string> files) : base(routedEvent)
        {
            Files = files;
        }

        public IEnumerable<string> Files { get; }
    }

    public static class DragnDropBehavior
    {
        public static RoutedEvent FileDroppedEvent = EventManager.RegisterRoutedEvent(
            "FileDropped", 
            RoutingStrategy.Bubble,
            typeof(FileDroppedEventArgs), 
            typeof(DragnDropBehavior)) ;

        public static DependencyProperty AllowDrop = DependencyProperty.RegisterAttached(
            nameof(AllowDrop),
            typeof(bool),
            typeof(DragnDropBehavior),
            new PropertyMetadata(false, OnAllowDropChanged));

        public static bool GetAllowDrop(UIElement element) => (bool) element.GetValue(AllowDrop);

        public static void SetAllowDrop(UIElement element, bool value) => element.SetValue(AllowDrop, value);

        public static DependencyProperty DropCommand = DependencyProperty.RegisterAttached(
            nameof(DropCommand),
            typeof(ICommand),
            typeof(DragnDropBehavior),
            new PropertyMetadata(null, OnDropCommandChanged));
        
        public static ICommand GetDropCommand(UIElement element) => (ICommand) element.GetValue(DropCommand);

        public static void SetDropCommand(UIElement element, ICommand value) => element.SetValue(DropCommand, value);

        private static void OnAllowDropChanged(DependencyObject d, DependencyPropertyChangedEventArgs  args)
        {
            var value = (bool) args.NewValue;
            var element = (UIElement)d;
            element.AllowDrop = value;
            
            if (value)
                element.Drop += OnDropped;
            else
                element.Drop -= OnDropped;
        }
        
        private static void OnDropCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var element = (UIElement)d;
            
            var handler = new RoutedEventHandler((_, args) => OnDropEventArrived(element, args));
            
            if (args.NewValue == null)
                element.RemoveHandler(FileDroppedEvent, handler);
            else 
                element.AddHandler(FileDroppedEvent, handler);
        }
        
        private static void OnDropEventArrived(UIElement element, RoutedEventArgs  args)
        {
            var arguments = (FileDroppedEventArgs)args;
            var command = GetDropCommand(element);

            command.Execute(arguments.Files);
        }

        private static void OnDropped(object source,  DragEventArgs args)
        {
            var element = (UIElement) source;
            if (!(args.Data is DataObject dataObject)) return;
            var files = dataObject.GetFiles().ToList();
            if (files.Any())
                element.RaiseEvent(new FileDroppedEventArgs(FileDroppedEvent, files));
        }
    }
}
