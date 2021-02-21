using System.Collections;
using System.Windows;
using System.Windows.Controls;
using LogGrokCore.Controls;
using ListView = System.Windows.Controls.ListView;

namespace LogGrokCore.MarkedLines
{
   public class MarkedLinesListView : ListView
    {
        public static readonly DependencyProperty ReadonlySelectedItemsProperty = DependencyProperty.Register(
            "ReadonlySelectedItems", typeof(IEnumerable), typeof(MarkedLinesListView),
            new PropertyMetadata(default(IEnumerable)));

        public IEnumerable ReadonlySelectedItems
        {
            get => (IEnumerable) GetValue(ReadonlySelectedItemsProperty);
            set => SetValue(ReadonlySelectedItemsProperty, value);
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            ReadonlySelectedItems = SelectedItems;
            base.OnSelectionChanged(e);
        }

        protected override ListViewItem GetContainerForItemOverride()
        {
            return new BaseLogListViewItem(this);
        }
    }
}