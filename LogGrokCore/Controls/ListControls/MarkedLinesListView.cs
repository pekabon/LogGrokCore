using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LogGrokCore.Controls.ListControls
{
   public class MarkedLinesListView : BaseListView
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

        protected override IEnumerable<int> GetSelectedIndices()
        {
            var selectedItems = new HashSet<object>(ReadonlySelectedItems.Cast<object>());
            for (var i = 0; i < Items.Count; i++)
            {
                if (selectedItems.Contains(Items[i]))
                    yield return i;
            }
        }
    }
}