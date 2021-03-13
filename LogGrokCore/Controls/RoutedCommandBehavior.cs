using System.Windows;
using System.Windows.Input;
using LogGrokCore.Controls.FilterPopup;

namespace LogGrokCore.Controls
{
    public static class RoutedCommandBehavior
    {
        public static DependencyProperty RoutedCommandToCommandBinding =
            DependencyProperty.RegisterAttached("RoutedCommandToCommandBinding",
                typeof(RoutedCommandToCommandBinding),
                typeof(RoutedCommandBehavior),
                new PropertyMetadata(null, CommandChanged));

        public static RoutedCommandToCommandBinding? GetRoutedCommandToCommandBinding(DependencyObject d) =>
            d.GetValue(RoutedCommandToCommandBinding) as RoutedCommandToCommandBinding;

        public static void SetRoutedCommandToCommandBinding(DependencyObject d, RoutedCommandToCommandBinding value) =>
            d.SetValue(RoutedCommandToCommandBinding, value);
        
        
        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var binding = GetRoutedCommandToCommandBinding(d);
            if (binding == null) return;
            
            var routedCommand = binding.RoutedCommand;
        
            var commandBinding = new CommandBinding(routedCommand, Executed, CanExecute);
            var uiElement = (UIElement)d;
            _ = uiElement.CommandBindings.Add(commandBinding);
        }
        
        private static void Executed(object target, ExecutedRoutedEventArgs e)
        {
            var command = GetRoutedCommandToCommandBinding((DependencyObject)target)?.Command;
            command?.Execute(e.Parameter);
            e.Handled = true;
        }

        private static void CanExecute(object target, CanExecuteRoutedEventArgs  e)
        {
            var command = GetRoutedCommandToCommandBinding((DependencyObject)target)?.Command;
            e.CanExecute = command?.CanExecute(e.Parameter) ?? false;
            e.ContinueRouting = true;
            e.Handled = true;            
        }        
    }
}
