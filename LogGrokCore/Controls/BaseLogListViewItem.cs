using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LogGrokCore.Colors;

namespace LogGrokCore.Controls
{
    public class BaseLogListViewItem : ListViewItem
    {
        private readonly ItemsControl _itemsControl;

        private Brush? _overrideForeground;

        private Brush? _overrideBackground;

        public BaseLogListViewItem(ItemsControl itemsControl)
        {
            _itemsControl = itemsControl;
            _itemsControl.Items.CurrentChanged += (_, _) =>
            {
                UpdateIsCurrentProperty();
            };
        }

        public static readonly DependencyProperty IsCurrentItemProperty = DependencyProperty.Register(
            "IsCurrentItem", typeof(bool), typeof(BaseLogListViewItem), 
            new PropertyMetadata(false));

        public bool IsCurrentItem
        {
            get => (bool) GetValue(IsCurrentItemProperty);
            set => SetValue(IsCurrentItemProperty, value);
        }

        private new static readonly DependencyProperty ForegroundProperty = 
            TextElement.ForegroundProperty.AddOwner(typeof(BaseLogListViewItem), 
                new FrameworkPropertyMetadata(SystemColors.ControlTextBrush, 
                    FrameworkPropertyMetadataOptions.Inherits,
                    (_, _) => { },
                    CoerceForegroundProperty));
        
        private new static readonly DependencyProperty BackgroundProperty = 
            Panel.BackgroundProperty.AddOwner(typeof(BaseLogListViewItem), 
                new FrameworkPropertyMetadata(Panel.BackgroundProperty.DefaultMetadata.DefaultValue, 
                    FrameworkPropertyMetadataOptions.Inherits,
                    (_, _) => { },
                    CoerceBackgroundProperty));

        private static object CoerceBackgroundProperty(DependencyObject d, object basevalue)
        {
            return (d as BaseLogListViewItem)?._overrideBackground ?? basevalue;
        }

        private static object CoerceForegroundProperty(DependencyObject d, object basevalue)
        {
            return (d as BaseLogListViewItem)?._overrideForeground ?? basevalue;
        }

        protected override void OnContentChanged(object oldContent, object? newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            UpdateIsCurrentProperty();
            var colorSettings = ColorSettings.GetColorSettings(this);
            if (oldContent == newContent || colorSettings == null) return;
            var text = newContent?.ToString();
            if (text == null) return;
            ColorSettings.ColorRule? rule = null;
            for (var i = 0; i < colorSettings.Rules.Count; i++)
            {
                var colorSettingsRule = colorSettings.Rules[i];
                if (colorSettingsRule.IsMatch(text))
                {
                    rule = colorSettingsRule;
                    break;
                }
            }

            _overrideForeground = rule?.Foreground;
            _overrideBackground = rule?.Background;
            CoerceValue(ForegroundProperty);
            CoerceValue(BackgroundProperty);
        }
        
        private void UpdateIsCurrentProperty()
        {
            IsCurrentItem = Content == _itemsControl.Items.CurrentItem;
        }
    }
}