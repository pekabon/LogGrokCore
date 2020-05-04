using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LogGrokCore.Controls
{
    public class ListView : System.Windows.Controls.ListView
    {
        public static readonly DependencyProperty ReadonlySelectedItemsProperty =
            DependencyProperty.Register(nameof(ReadonlySelectedItems), typeof(IEnumerable), typeof(ListView));

        public IEnumerable ReadonlySelectedItems
        {
            get => (IEnumerable)GetValue(ReadonlySelectedItemsProperty);
            set => SetValue(ReadonlySelectedItemsProperty, value);
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            ReadonlySelectedItems = SelectedItems.Cast<object>().ToList();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (GetPanel()?.ProcessKeyDown(e.Key) == true)
                e.Handled = true;
            else 
                base.OnKeyDown(e);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (GetPanel()?.ProcessPreviewMouseDown() == true)
            {
                e.Handled = true;
            }
            
            base.OnPreviewMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }

        private VirtualizingStackPanel? _panel;
        private VirtualizingStackPanel? GetPanel()
        {
            _panel ??= this.GetVisualChildren<VirtualizingStackPanel>().FirstOrDefault();
            return _panel;
        }
    }
}