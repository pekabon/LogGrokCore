using System;
using System.Globalization;
using MahApps.Metro.Converters;

namespace LogGrokCore.Controls
{
    public class FormatTextExtension : MarkupConverter
    {
        protected override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && parameter is string format)
                return string.Format(format, text);
            return value;
        }

        protected override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}