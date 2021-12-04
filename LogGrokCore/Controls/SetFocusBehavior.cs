using System.Windows;

namespace LogGrokCore.Controls
{
    public class SetFocusBehavior
    { 
        public static readonly DependencyProperty SetFocusRequestProperty =
            DependencyProperty.RegisterAttached(
                "SetFocusRequest", typeof(SetFocusRequest), typeof(SetFocusBehavior),
                new FrameworkPropertyMetadata(null, OnSetFocusRequestChanged));

        public static SetFocusRequest GetSetFocusRequest(DependencyObject obj)
        {
            return (SetFocusRequest)obj.GetValue(SetFocusRequestProperty);
        }

        public static void SetSetFocusRequest(DependencyObject obj, SetFocusRequest value)
        {
            obj.SetValue(SetFocusRequestProperty, value);
        }

        private static void OnSetFocusRequestChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement uiElement && e.NewValue is SetFocusRequest setFocusRequest)
            {
                setFocusRequest.SetFocus += () => InvokeSetFocus(uiElement);
            }
        }

        private static void InvokeSetFocus(UIElement uiElement)
        {
            uiElement.Focus();
        }
    }
}