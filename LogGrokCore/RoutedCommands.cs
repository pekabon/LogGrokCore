using System.Windows;
using System.Windows.Input;

namespace LogGrokCore
{
    public static class RoutedCommands
    {
        public static readonly RoutedCommand Cancel = new RoutedUICommand(
            "Cancel", "Cancel", typeof(UIElement), 
            new InputGestureCollection { new KeyGesture(Key.Escape) });
    }
}
