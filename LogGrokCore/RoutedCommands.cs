using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

namespace LogGrokCore
{
    public static class RoutedCommands
    {
        public static readonly RoutedCommand Cancel = new RoutedUICommand(
            "Cancel", "Cancel", typeof(UIElement),
            new InputGestureCollection { new KeyGesture(Key.Escape) });

        public static readonly RoutedCommand SearchText = new RoutedUICommand(
            "Search selection", "Search selection", typeof(UIElement));

        public static readonly RoutedCommand ClearFilters = new RoutedUICommand(
            "Clear filters", "Clear filters", typeof(UIElement));

        public static readonly RoutedUICommand CopyToClipboard;

        public static readonly RoutedCommand ToggleMarks = new RoutedUICommand(
            "Mark", "Mark lines", typeof(UIElement),
            new InputGestureCollection { new KeyGesture(Key.Space) });

        public static void ToggleMarksHandler(IEnumerable<Data.ILineMark> items) 
        {
            var existsUnMarked = false;
            foreach (var item in items)
            {
                if (!item.IsMarked)
                {
                    item.IsMarked = !item.IsMarked;
                    existsUnMarked = true;
                }
            }
            if (!existsUnMarked)
            {
                foreach (var item in items)
                {
                    item.IsMarked = !item.IsMarked;
                }
            }
        }

        static RoutedCommands()
        {
            CopyToClipboard = new RoutedUICommand(
                ApplicationCommands.Copy.Text,
                nameof(CopyToClipboard),
                typeof(UIElement), new InputGestureCollection(ApplicationCommands.Copy.InputGestures));

            ApplicationCommands.Copy.InputGestures.Clear();
        }
    }
}