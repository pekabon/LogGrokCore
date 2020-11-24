using System.Windows;
using System.Windows.Input;

namespace LogGrokCore.Controls.FilterPopup
{
    public static class RoutedCommands
    {
        public static RoutedCommand Cancel = new RoutedUICommand(
            "Cancel", "Cancel", typeof(UIElement), 
            new InputGestureCollection { new KeyGesture(Key.Escape) });
    }
}
