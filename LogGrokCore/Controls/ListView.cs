using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace LogGrokCore.Controls
{
    
    public class ListView : System.Windows.Controls.ListView
    {
        public static readonly DependencyProperty ReadonlySelectedItemsProperty =
            DependencyProperty.Register(nameof(ReadonlySelectedItems), typeof(IEnumerable), typeof(ListView));

        public ListView()
        {
            Loaded += (o, e) => ScheduleResetColumnsWidth();
            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Copy,
                (o, e) => CopySelectedItemsToClipboard(),
                (o, e) =>
                {
                    if (!ReadonlySelectedItems.Any()) return;
                    e.CanExecute = true;
                    e.Handled = true;
                }
            ));
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

        protected override void OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)
        {
            base.OnItemContainerStyleChanged(oldItemContainerStyle, newItemContainerStyle);
        }

        public IEnumerable<object> ReadonlySelectedItems
        {
            get => ((IEnumerable)GetValue(ReadonlySelectedItemsProperty)).Cast<object>();
            set => SetValue(ReadonlySelectedItemsProperty, value);
        }
        
        public void BringIndexIntoView(in int lineNumber)
        {
            GetPanel()?.NavigateTo(lineNumber);
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

        private int _previousItemCount;
        
        protected override void OnItemsSourceChanged(IEnumerable? oldValue, IEnumerable? newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            if (newValue == null)
            {
                _previousItemCount = 0;
            }
            else
            {
                if (newValue is IGrowingCollection growingCollection)
                {
                    growingCollection.CollectionGrown += _ => _panel?.InvalidateMeasure();
                }

                _ = Dispatcher.BeginInvoke(
                    () =>
                    {
                        if (Items.Count > 0) ScheduleResetColumnsWidth();
                        _previousItemCount = Items.Count;
                    }, 
                    DispatcherPriority.ApplicationIdle);
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (_previousItemCount == 0 && this.Items.Count > 0)
                ScheduleResetColumnsWidth();
            
            _previousItemCount = Items.Count;            
        }

        private void ScheduleResetColumnsWidth()
        {
            double CalculateRemainingSpace(System.Windows.Controls.GridView view)
            {
                
                if (double.IsNaN(ActualWidth))
                    Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    
                return ActualWidth - view.Columns
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
                foreach (var column in view.Columns.Take(view.Columns.Count - 1))
                {
                    column.Width = 1;
                    column.ClearValue(GridViewColumn.WidthProperty);
                }
                _ = Dispatcher.BeginInvoke(() => UpdateLastColumnWidth(view), DispatcherPriority.ApplicationIdle);
            }

            if (View is System.Windows.Controls.GridView gridView)
                _ = Dispatcher.BeginInvoke(() => ResetWidth(gridView), DispatcherPriority.ApplicationIdle);
        }

        private void CopySelectedItemsToClipboard()
        {
            var indices = GetPanel()?.SelectedIndices ?? Enumerable.Empty<int>();
            
            var items =  
                indices
                    .OrderBy(i => i)
                    .Select(i => Items[i]);
            
            var  text = new StringBuilder();
            foreach (var line in items)
            {
                _ = text.Append(line);
                _ = text.Append("\r\n");
            }
            _ = text.Replace("\0", string.Empty);

            TextCopy.ClipboardService.SetText(text.ToString());
        }

        private VirtualizingStackPanel? _panel;
        private VirtualizingStackPanel? GetPanel()
        {
            _panel ??= this.GetVisualChildren<VirtualizingStackPanel>().FirstOrDefault();
            return _panel;
        }
    }
}