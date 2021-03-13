using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace LogGrokCore.Controls
{
    public class RoutedCommandToCommandBindingExtension : MarkupExtension
    {
        public RoutedCommand? RoutedCommand { get; set; }
        
        public Binding? Command { get; set; }
        
        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            if (Command == null || RoutedCommand == null) return null;
            Command.Converter = new InnerConverter(RoutedCommand);
            return Command.ProvideValue(serviceProvider);
        }
        
        
        private class InnerConverter : IValueConverter
        {
            public InnerConverter(RoutedCommand routedCommand)
            {
                _routedCommand = routedCommand;
            }

            public object Convert(object value, Type _t, object _obj, CultureInfo cultureInfo)
            {
                return new RoutedCommandToCommandBinding(_routedCommand, (ICommand) value);
            }
             
             public object ConvertBack(object obj, Type type,object o, CultureInfo cultureInfo)
             {
                 throw new NotSupportedException();
             }
             
             private readonly RoutedCommand _routedCommand;
        }
    }
}
