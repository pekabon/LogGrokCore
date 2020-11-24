using System.Windows;
using System.Windows.Controls;

namespace LogGrokCore.Controls.FilterPopup
{
    public class ResizeablePopupContent : ContentControl
    {        
        static ResizeablePopupContent()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizeablePopupContent), 
                new FrameworkPropertyMetadata(typeof(ResizeablePopupContent)));
        }
    }
}
