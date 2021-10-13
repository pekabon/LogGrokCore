using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace LogGrokCore.Controls.ListControls
{
    public class ListView : BaseListView
    {
        public static readonly DependencyProperty ReadonlySelectedItemsProperty =
            DependencyProperty.Register(nameof(ReadonlySelectedItems), typeof(IEnumerable), typeof(ListView));

        public ListViewItem GetContainerForItem() => new LogListViewItem(this);

        public ListView()
        {
            CommandBindings.Add(new CommandBinding(RoutedCommands.ToggleMarks,
                (_, args) =>
                {
                    var items = GetSelectedIndices().Select(i => Items[i]).OfType<BaseLogLineViewModel>();
                    foreach (var item in items)
                    {
                        item.IsMarked = !item.IsMarked;
                    }

                    args.Handled = true;
                },
                (_, args) =>
                {
                    args.CanExecute = GetSelectedIndices().Any();
                    args.Handled = true;
                }));
        }

        public void UpdateReadonlySelectedItems(IEnumerable<int> selectedIndices)
        {
            ReadonlySelectedItems =
                selectedIndices
                    .Where(index => index < Items.Count && index >= 0)
                    .Select(index => Items[index]).ToList();
        }

        public void PrepareItemContainer(ListViewItem container, object item)
        {
            var itemContainerStyle = ItemContainerStyle;
            if (container.ReadLocalValue(FrameworkElement.StyleProperty) is Style style
                && style == itemContainerStyle)
                return;
            container.Style = itemContainerStyle;
            PrepareContainerForItemOverride(container, item);
        }

        public IEnumerable<object>? ReadonlySelectedItems
        {
            get => (GetValue(ReadonlySelectedItemsProperty) as IEnumerable)?.Cast<object>();
            set => SetValue(ReadonlySelectedItemsProperty, value);
        }

        public void NavigateTo(int lineNumber)
        {
            GetPanel()?.NavigateTo(lineNumber);
        }

        public void BringIndexIntoView(in int lineNumber)
        {
            GetPanel()?.BringIndexIntoViewPublic(lineNumber);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (GetPanel()?.ProcessKeyDown(e.Key) == true)
                e.Handled = true;
            else 
                base.OnKeyDown(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (GetPanel()?.ProcessMouseDown(e.ChangedButton) == true)
            {
                e.Handled = true;
            }
            
            base.OnMouseDown(e);
        }

        private int _previousItemCount;
        private bool _headerAdusted = false;
        protected override void OnItemsSourceChanged(IEnumerable? oldValue, IEnumerable? newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            if (newValue == null)
            {
                _previousItemCount = 0;
                return;
            }
            
            if (newValue is IGrowingCollection growingCollection)
            {
                _headerAdusted = false;
                growingCollection.CollectionGrown += ScheduleRemeasure;
            }
            
            if (oldValue is IGrowingCollection oldCollection)
            {
                oldCollection.CollectionGrown -= ScheduleRemeasure;
            }

            ScheduleRemeasure(0);
        }

        private void ScheduleRemeasure(int _)
        {
            var itemsCount = Items.Count;
            if (!_headerAdusted && itemsCount > _previousItemCount) ScheduleResetColumnsWidth();
            _previousItemCount = itemsCount;
            _panel?.InvalidateMeasure();
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (_previousItemCount == 0 && Items.Count > 0)
                ScheduleResetColumnsWidth();
            
            _previousItemCount = Items.Count;            
        }

        private void ScheduleResetColumnsWidth()
        {
            var panel = GetPanel();
            if (panel != null) ScheduleResetColumnsWidthCore(panel);
        }

        private void ScheduleResetColumnsWidthCore(VirtualizingStackPanel.VirtualizingStackPanel panel)
        {
            double CalculateRemainingSpace(System.Windows.Controls.GridView view)
            {
                if (double.IsNaN(ActualWidth))
                    Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                return Math.Max(panel.VisibleItemsMaxWidth, ActualWidth) - view.Columns
                                                      .Take(view.Columns.Count - 1)
                                                      .Sum(c => c.ActualWidth) 
                                                  - SystemParameters.ScrollWidth * 2;
            }
              
            void UpdateLastColumnWidth(System.Windows.Controls.GridView view)
            {
                var lastColumn = view.Columns.Last();
                var remainingSpace = CalculateRemainingSpace(view);
                if (lastColumn.ActualWidth < remainingSpace)
                    lastColumn.Width = remainingSpace;                
            }
                      
            void ResetWidth(System.Windows.Controls.GridView view)
            {
                Trace.TraceInformation($"Reset width: {Items.Count}");
                foreach (var column in view.Columns.Take(view.Columns.Count - 1))
                {
                    column.Width = 1;
                    column.ClearValue(GridViewColumn.WidthProperty);
                }
                _ = Dispatcher.BeginInvoke(() => UpdateLastColumnWidth(view), DispatcherPriority.ApplicationIdle);
            }

            _ = Dispatcher.BeginInvoke(() =>
            {
                if (View is System.Windows.Controls.GridView gridView)
                {
                    ResetWidth(gridView);
                    if (GetPanel()?.IsViewportIsCompletelyFilled ?? false)
                    {
                        _headerAdusted = true;
                    }
                }
                else
                {
                    ScheduleResetColumnsWidth();
                }
            }, DispatcherPriority.ApplicationIdle);
        }

        protected override IEnumerable<int> GetSelectedIndices() => GetPanel()?.SelectedIndices ?? Enumerable.Empty<int>();

        private VirtualizingStackPanel.VirtualizingStackPanel? _panel;
        private VirtualizingStackPanel.VirtualizingStackPanel? GetPanel()
        {
            _panel ??= this.GetVisualChildren<VirtualizingStackPanel.VirtualizingStackPanel>().FirstOrDefault();
            return _panel;
        }
    }
}