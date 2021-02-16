using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LogGrokCore.Controls
{
    public class RootDocumentControl : ContentControl
    {
        public static readonly DependencyProperty SearchTextCommandProperty = DependencyProperty.Register(
            "SearchTextCommand", typeof(ICommand), typeof(RootDocumentControl), new PropertyMetadata(default(ICommand)));

        public ICommand? SearchTextCommand
        {
            get => (ICommand?) GetValue(SearchTextCommandProperty);
            set => SetValue(SearchTextCommandProperty, value);
        }
        
        public RootDocumentControl()
        {
            CommandBindings.Add(new CommandBinding(
                RoutedCommands.SearchText,
                (_, args) =>
                {
                    if (args.Parameter is string text && !string.IsNullOrEmpty(text))
                        SearchTextCommand?.Execute(text);
                    args.Handled = true;
                },
                (_, args) =>
                {
                    args.CanExecute = 
                        args.Parameter is string text && !string.IsNullOrEmpty(text) &&
                        (SearchTextCommand?.CanExecute(text) ?? false);
                    args.Handled = true;
                }));
        }
    }
}