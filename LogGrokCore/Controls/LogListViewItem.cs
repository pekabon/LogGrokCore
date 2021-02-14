using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LogGrokCore.Colors;

namespace LogGrokCore.Controls
{
    public class LogListViewItem : ListViewItem
    {
        private new static readonly DependencyProperty ForegroundProperty = 
            TextElement.ForegroundProperty.AddOwner(typeof(LogListViewItem), 
                new FrameworkPropertyMetadata(SystemColors.ControlTextBrush, 
                    FrameworkPropertyMetadataOptions.Inherits,
                    (_, _) => { },
                    CoerceForegroundProperty));
        
        private new static readonly DependencyProperty BackgroundProperty = 
            Panel.BackgroundProperty.AddOwner(typeof(LogListViewItem), 
                new FrameworkPropertyMetadata(Panel.BackgroundProperty.DefaultMetadata.DefaultValue, 
                    FrameworkPropertyMetadataOptions.Inherits,
                    (_, _) => { },
                    CoerceBackgroundProperty));

        private static object CoerceBackgroundProperty(DependencyObject d, object basevalue)
        {
            return (d as LogListViewItem)?._overrideBackground ?? basevalue;
        }

        private static object CoerceForegroundProperty(DependencyObject d, object basevalue)
        {
            return (d as LogListViewItem)?._overrideForeground ?? basevalue;
        }

        private Brush? _overrideForeground;

        private Brush? _overrideBackground;

        protected override void OnContentChanged(object oldContent, object? newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            var colorSettings = ColorSettings.GetColorSettings(this);
            if (oldContent == newContent || colorSettings == null) return;
            var text = newContent?.ToString();
            if (text == null) return;
            var rule = colorSettings.Rules.FirstOrDefault(r => r.IsMatch(text));
            _overrideForeground = rule?.Foreground;
            _overrideBackground = rule?.Background;
            CoerceValue(ForegroundProperty);
            CoerceValue(BackgroundProperty);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (IsSelected)
                base.OnMouseLeftButtonDown(e);
        }
        
        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (IsSelected)
                base.OnMouseRightButtonDown(e);
        }
    }
}