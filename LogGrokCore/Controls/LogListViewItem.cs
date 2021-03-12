using System.Windows.Input;

namespace LogGrokCore.Controls
{
    public class LogListViewItem : BaseLogListViewItem
    {
        public LogListViewItem(ListView itemsControl) : base(itemsControl)
        {
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