using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
    }
}