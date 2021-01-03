using System.Windows.Controls;
using System.Windows.Input;

namespace LogGrokCore.Controls
{
    public class NotHandlingListViewItem : ListViewItem
    {
        // protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        // {
        //     if (IsSelected)
        //         base.OnMouseLeftButtonDown(e);
        //     
        //     
        // }
        //
        // protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        // {
        //     if (IsSelected)
        //         base.OnMouseRightButtonDown(e);
        // }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);
        }
    }
}