using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using LogGrokCore.Data;

namespace LogGrokCore.Controls
{
    public class MetaInfoToMenuItemsConverterExtension : MarkupExtension, IMultiValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 3)
                return null;

            switch (values[0], values[1], values[2])
            {
                case (LogMetaInformation meta, ICommand command, IEnumerable<object> selectedItems):
                    return selectedItems.Any() ? ConvertCore(meta, command, selectedItems) : null;
                case (ICommand command, LogMetaInformation meta, IEnumerable<object> selectedItems): 
                    return selectedItems.Any() ? ConvertCore(meta, command, selectedItems) : null;
                default:
                    return null;
            }
        }

        private object? ConvertCore(LogMetaInformation meta, ICommand command, IEnumerable<object> selectedItems)
        {
            var isMultiSelection = selectedItems.Skip(1).Any();
            if (!isMultiSelection)
            {
                if (selectedItems.Single() is LogHeaderViewModel)
                    return null;
            }

            MenuItem CreateMenuItem(string fieldName, int fieldIndex)
            {
                var menuItem = new MenuItem {Command = new DelegateCommand(() => command.Execute(fieldIndex))};
                if (isMultiSelection)
                {
                    menuItem.Header = fieldName;
                }
                else
                {
                    menuItem.DataContext = selectedItems.Single();
                    _ = menuItem.SetBinding(MenuItem.HeaderProperty, new Binding($"[{fieldIndex}]"));
                    menuItem.HeaderStringFormat = $"{fieldName}: {{0}}";
                }
                return menuItem;
            }

            return meta.IndexedFieldNumbers.Select(ind =>
                CreateMenuItem(meta.FieldNames[ind], ind));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}