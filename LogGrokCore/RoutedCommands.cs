using System.Windows;
using System.Windows.Input;

namespace LogGrokCore
{
    public static class RoutedCommands
    {
        public static readonly RoutedCommand Cancel = new RoutedUICommand(
            "Cancel", "Cancel", typeof(RoutedCommands), 
            new InputGestureCollection { new KeyGesture(Key.Escape) });
        
        public static readonly RoutedCommand SearchText = new RoutedUICommand(
            "Search selection", "Search selection", typeof(RoutedCommands));

        public static readonly RoutedCommand ClearFilters = new RoutedUICommand(
            "Clear filters", "Clear filters", typeof(RoutedCommands));

        public static readonly RoutedUICommand CopyToClipboard;

        public static readonly RoutedUICommand ToggleMark = new RoutedUICommand(
            "Toggle Mark", "Toggle Mark", typeof(RoutedCommands), 
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
