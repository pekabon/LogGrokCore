using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using DryIoc.ImTools;
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

            if (values[2] is not IEnumerable<object> selectedItems || !selectedItems.Any())
                return null;

            return (values[0], values[1]) switch
            {
                (LogMetaInformation meta, ICommand command) => ConvertCore(meta, command, selectedItems),
                (ICommand command, LogMetaInformation meta) => ConvertCore(meta, command, selectedItems),
                _ => null
            };
        }

        private static object? ConvertCore(LogMetaInformation meta, ICommand command, IEnumerable<object> selectedItems)
        {
            var singleObject = selectedItems.SingleOrDefaultIfMany();
            if (singleObject is LogHeaderViewModel)
                return null;

            var isMultiSelection = singleObject == null;

            MenuItem CreateMenuItem(string fieldName, int fieldIndex)
            {
                var menuItem = new MenuItem {Command = new DelegateCommand(() => command.Execute(fieldIndex))};
                if (isMultiSelection)
                {
                    menuItem.Header = fieldName;
                }
                else
                {
                    menuItem.DataContext = singleObject;
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