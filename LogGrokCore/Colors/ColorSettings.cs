using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace LogGrokCore.Colors
{
    public class ColorSettings
    {
        public static readonly DependencyProperty ColorSettingsProperty = DependencyProperty.RegisterAttached(
            "ColorSettings",
            typeof(ColorSettings),
            typeof(ColorSettings),
            new FrameworkPropertyMetadata(default(ColorSettings), FrameworkPropertyMetadataOptions.Inherits));

        public static void SetColorSettings(DependencyObject element, ColorSettings value)
        {
            element.SetValue(ColorSettingsProperty, value);
        }

        public static ColorSettings? GetColorSettings(DependencyObject element)
        {
            return element.GetValue(ColorSettingsProperty) as ColorSettings;
        }

        public class ColorRule
        {
            public ColorRule(Regex regex, Brush? foreground, Brush? background)
            {
                Regex = regex;
                Foreground = foreground;
                Background = background;
            }

            public Brush? Foreground { get; }
            public Brush? Background { get; }
            public Regex Regex { get; }

            public bool IsMatch(string text)
            {
                return Regex.IsMatch(text);
            }
        }

        public IReadOnlyList<ColorRule> Rules { get; }

        private static readonly ConcurrentDictionary<string, Brush?> CachedBruches = new();
        private static readonly ConcurrentDictionary<string, Regex> CachedRegexes = new();
        
        public ColorSettings(Configuration.ColorSettings colorSettingsConfiguration)
        {
            Brush? CreateBrush(string colorString)
            {
                if (string.IsNullOrEmpty(colorString)) return null;
                var color = (Color)ColorConverter.ConvertFromString(colorString);
                var brush = new SolidColorBrush(color);
                
                brush.Freeze();
                return brush;
            }

            ColorRule Convert(Configuration.ColorRule rule)
            {
                return new ColorRule(
                    CachedRegexes.GetOrAdd(rule.RegexString, s => new Regex(s, RegexOptions.Compiled)),
                    CachedBruches.GetOrAdd(rule.ForegroundColor, CreateBrush),
                    CachedBruches.GetOrAdd(rule.BackgroundColor, CreateBrush));
            }

            Rules = colorSettingsConfiguration.Rules.Select(Convert).ToList();
        }
    }
}