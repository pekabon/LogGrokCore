using System.Windows.Controls;
using System.Windows.Input;

namespace LogGrokCore.Controls.ListControls
{
    public class LogListViewItem : BaseLogListViewItem
    {
        public LogListViewItem(ItemsControl itemsControl) : base(itemsControl)
        {
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            SetFocus();
        }
        
        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            SetFocus();
        }

        private void SetFocus()
        {
            var focusedElement = FocusManager.GetFocusedElement(this);
            if (!ReferenceEquals(focusedElement, this))
                Focus();
        }
    }
}