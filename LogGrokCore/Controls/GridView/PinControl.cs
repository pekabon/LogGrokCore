using System.Windows;
using System.Windows.Controls;

namespace LogGrokCore.Controls.GridView
{
    public class PinControl : CheckBox
    {
        static PinControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PinControl), 
                new FrameworkPropertyMetadata(typeof(PinControl)));
        }
    }
}