using System;
using System.Globalization;
using System.Windows.Data;

namespace LogGrokCore.Controls
{
    public class ObjectToTypeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.GetType();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}