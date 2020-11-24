using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace LogGrokCore.Controls.FilterPopup
{
    public static class FocusOnOpenPopupBehavior
    {

        public static readonly DependencyProperty DefaultFocusedElementProperty = DependencyProperty.RegisterAttached(
            "DefaultFocusedElement", typeof(UIElement), typeof(FocusOnOpenPopupBehavior), 
            new PropertyMetadata(default(UIElement), DefaultFocusedElementChanged));

        public static void SetDefaultFocusedElement(Popup element, UIElement? value)
        {
            element.SetValue(DefaultFocusedElementProperty, value);
        }

        public static UIElement? GetDefaultFocusedElement(Popup element)
        {
            return element.GetValue(DefaultFocusedElementProperty) as UIElement;
        }
        
        private static void DefaultFocusedElementChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs  args)
        {
            
            var popup = (Popup)dependencyObject;
            
            if(args.OldValue is UIElement)
            {
                popup.Opened -= PopupOpened;
            }
            
            if (args.NewValue is UIElement)
            {
                popup.Opened += PopupOpened;
            }
        }
        
        private static void PopupOpened(object sender, EventArgs args)
        {
            var popup = (Popup)sender; 
            var focusedElement = GetDefaultFocusedElement(popup);
         
            _ = focusedElement?.Focus();   
        }
    }
}
