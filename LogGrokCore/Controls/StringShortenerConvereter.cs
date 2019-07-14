using System;
using System.Globalization;
using System.Windows.Data;

namespace LogGrokCore.Controls
{
    public class StringShortenerConvereter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value, parameter) switch
                {
                    (string val, string param) when int.TryParse(param, out var intParam) 
                        => MakeShortString(val, intParam),
                    _ => throw new InvalidOperationException("Unsupported parameters: value must be string"+
                        $"parameter must be int; actual: value={value}, parameter={parameter}")
                };
        }
        
        private static string MakeShortString(string title, int shortStringLength)
        {
            if (title.Length <= shortStringLength)
                return title;

            return title.Substring(0, shortStringLength / 2) + "\u2026"
                                                            + title.Substring(title.Length - shortStringLength/ 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}