using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace LogGrokCore.Controls.FilterPopup
{
    public static class BindPopupBehavior
    {
        public static DependencyProperty ToToggleButtonProperty = DependencyProperty.RegisterAttached(
            "ToToggleButton", typeof(ToggleButton), 
            typeof(BindPopupBehavior),
            new PropertyMetadata(null, OnToggleButtonChanged));

        public static ToggleButton? GetToToggleButton(Popup popup) => 
            popup.GetValue(ToToggleButtonProperty) as ToggleButton;
        
        public static void SetToToggleButton(Popup popup, ToggleButton value) => 
            popup.SetValue(ToToggleButtonProperty, value);
        
        public static DependencyProperty BindedPopupProperty = DependencyProperty.RegisterAttached(
            "BindedPopup", typeof(Popup),
            typeof(BindPopupBehavior), null
            );

        public static Popup? GetBindedPopup(ToggleButton toggleButton) =>
            toggleButton.GetValue(BindedPopupProperty) as Popup;

        public static void SetBindedPopup(ToggleButton toggleButton, Popup? value) =>
            toggleButton.SetValue(BindedPopupProperty, value);
        
        private static void OnToggleButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs args) 
        {
            
            var popup = (Popup)d;
            
            if (args.OldValue is ToggleButton oldToggleButton)
            {
                SetBindedPopup(oldToggleButton, null);
                oldToggleButton.PreviewMouseDown -= OnToggleButtonPreviewMouseDown;
                BindingOperations.ClearBinding(popup, Popup.IsOpenProperty);
            }
            
            if  (args.NewValue is ToggleButton newToggleButton)
            {
                var binding = new Binding
                {
                    Source = newToggleButton, Path = new PropertyPath("IsChecked"), Mode = BindingMode.TwoWay
                };

                SetBindedPopup(newToggleButton, popup);
                newToggleButton.PreviewMouseDown += OnToggleButtonPreviewMouseDown;                
                _ = popup.SetBinding(Popup.IsOpenProperty, binding);
            }
        }        
        
        private static void OnToggleButtonPreviewMouseDown(object s, MouseButtonEventArgs _)
        {
            if (!(s is ToggleButton toggleButton)) return;
            if (!(GetBindedPopup(toggleButton) is { } popup) || !toggleButton.IsEnabled || popup.StaysOpen ||
                !popup.IsOpen) return;
            toggleButton.IsEnabled = false;
            popup.Closed += OnPopupClosed;
        }
        
        private static void OnPopupClosed(object s, EventArgs eventArgs)
        {
            var popup = (Popup)s;;
            var toggleButton = GetToToggleButton(popup);
            if (toggleButton != null)
                toggleButton.IsEnabled = true;
            popup.Closed -= OnPopupClosed;
        }
    }
}
