using System.Windows;
using System.Windows.Input;

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