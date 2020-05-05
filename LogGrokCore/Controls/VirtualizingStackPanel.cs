using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace LogGrokCore.Controls
{
    public partial class VirtualizingStackPanel : VirtualizingPanel, IScrollInfo
    {
        private readonly TranslateTransform _trans = new TranslateTransform();
        private List<VisibleItem> _visibleItems = new List<VisibleItem>();
        private readonly HashSet<UIElement> _recycled = new HashSet<UIElement>();

        private Size _viewPort;
        private Size _extent;
        private Point _offset;
        private double _viewPortHeightInPixels;
        private int _currentPosition;

        public VirtualizingStackPanel()
        {
            RenderTransform = _trans;

            Loaded += (o, e) =>
            {
                var necessaryChildrenTouch = this.Children;
                var itemContainerGenerator = (ItemContainerGenerator) ItemContainerGenerator;
                itemContainerGenerator.ItemsChanged += (o, e) =>
                {
                    if (Items.Count <= 0) return;
                    _currentPosition = Math.Min(_currentPosition, Items.Count - 1);
                    Items.MoveCurrentToPosition(_currentPosition);
                };

                Items.CurrentChanged += OnCurrentItemChanged;
            };
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _viewPort.Width = availableSize.Width;
            UpdateExtent();
            var count = Items.Count;
            if (count > 0)
                BuildVisibleItems(availableSize, VerticalOffset);

            return (double.IsPositiveInfinity(availableSize.Height)) ? new Size(1, 1) : availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var firstVisibleIndex = (int) Math.Floor(VerticalOffset);

            var elementsBefore = _visibleItems.TakeWhile(item => item.Index < firstVisibleIndex);
            var visibleElements =
                _visibleItems.SkipWhile(item => item.Index < firstVisibleIndex).ToList();

            var renderOffset = visibleElements.TakeFirst() switch
            {
                (var firstItem, var firstIndex, _, _) v
                => (firstIndex - VerticalOffset) * firstItem.DesiredSize.Height,
                _ => 0
            };

            _trans.Y = renderOffset;
            var offset = 0.0;

            foreach (var visibleItem in visibleElements)
            {
                var item = visibleItem.Element;
                var top = visibleItem.UpperBound - renderOffset;
                var bottom = visibleItem.LowerBound - renderOffset;
                var childRect = new Rect(0, top, _extent.Width, bottom - top);
                item.Arrange(childRect);
            }

            var itemOffsets = visibleElements.Select(item => (item.Index, item.UpperBound, item.LowerBound)).ToList();

            offset = 0.0;

            itemOffsets.Reverse();
            foreach (var v in elementsBefore.Reverse())
            {
                var (item, index, _, _) = v;
                var height = item.DesiredSize.Height;
                var childRect = new Rect(0, offset - height, _extent.Width, height);
                item.Arrange(childRect);

                var newOffset = offset - height;
                itemOffsets.Add((index, newOffset + renderOffset, offset + renderOffset));
                offset = newOffset;
            }

            itemOffsets.Reverse();

            var screenBound = finalSize.Height;

            var onScreenCount = _visibleItems
                .Where(v =>
                    GreaterOrEquals(v.LowerBound, 0.0) && LessOrEquals(v.UpperBound, screenBound))
                .Select(v =>
                    (Math.Min(v.LowerBound, screenBound) - Math.Max(v.UpperBound, 0.0)) / (v.LowerBound - v.UpperBound))
                .Sum();

            UpdateViewPort(finalSize, onScreenCount);

            return finalSize;
        }

        private void BuildVisibleItems(Size availableSize, double verticalOffset)
        {
            var firstVisibleItemIndex = (int) Math.Floor(verticalOffset);
            var startOffset = firstVisibleItemIndex - verticalOffset;

            var (newVisibleItems, itemsToRecycle) =
                GenerateItemsDownWithRelativeOffset(
                    startOffset, firstVisibleItemIndex, availableSize.Height, _visibleItems, availableSize);

            _visibleItems = newVisibleItems;

            RecycleItems(itemsToRecycle);
        }

        private void RecycleItems(IEnumerable<VisibleItem> itemsToRecycle)
        {
            foreach (var item in itemsToRecycle)
                RecycleItem(item.Element);

            for (var i = InternalChildren.Count - 1; i >= 0; i--)
            {
                var item = InternalChildren[i];
                if (_recycled.Contains(item))
                {
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        private (List<VisibleItem> newVisibleItems, List<VisibleItem> newItemsToRecycle)
            GenerateItemsDownWithRelativeOffset(
                double relativeOffset, int startIndex,
                double heightToBuild, List<VisibleItem> currentVisibleItems, Size availableSize)
        {
            var necessaryChildrenTouch = this.Children;

            var itemContainerGenerator = (ItemContainerGenerator) ItemContainerGenerator;
            using var itemGenerator = new ItemGenerator(itemContainerGenerator, GeneratorDirection.Forward);
            double? currentOffset = null;
            int currentIndex = startIndex;

            var oldItems = new HashSet<VisibleItem>(currentVisibleItems, new GenericEqualityComparer<VisibleItem>());
            var newItems = new List<VisibleItem>();

            while (currentOffset == null || currentOffset < heightToBuild)
            {
                var item = currentVisibleItems.Search(item => item.Index == currentIndex);

                if (item is VisibleItem existingItem)
                {
                    oldItems.Remove(existingItem);

                    var existingItemIndex = itemContainerGenerator.IndexFromContainer(existingItem.Element);
                    if (existingItemIndex >= 0)
                    {
                        currentOffset ??= existingItem.Height * relativeOffset;
                        newItems.Add(existingItem.MoveTo(currentOffset.Value));
                        currentIndex++;
                        currentOffset += existingItem.Height;
                        existingItem.Element.Measure(availableSize);
                        continue;
                    }
                    else
                    {
                        RecycleItem(existingItem.Element);
                    }
                }

                var newItem = itemGenerator.GenerateNext(currentIndex, out var isNewlyRealized);
                if (newItem is UIElement itm)
                {
                    InsertAndMeasureItem(itm, currentIndex, _recycled.Contains(itm), isNewlyRealized);

                    var itemHeight = itm.DesiredSize.Height;
                    currentOffset ??= itemHeight * relativeOffset;
                    newItems.Add(new VisibleItem(itm, currentIndex, currentOffset.Value,
                        currentOffset.Value + itemHeight));
                    currentIndex++;
                    currentOffset += itemHeight;

                    continue;
                }

                break;
            }

            return (newItems, oldItems.ToList());
        }

        private void RecycleItem(UIElement item)
        {
            var generator = (ItemContainerGenerator) (this.ItemContainerGenerator);
            var visualChildIndex = generator.IndexFromContainer(item);
            var childGeneratorPos = ItemContainerGenerator.GeneratorPositionFromIndex(visualChildIndex);

            if (childGeneratorPos.Index >= 0)
                ((IRecyclingItemContainerGenerator) ItemContainerGenerator).Recycle(childGeneratorPos, 1);

            _recycled.Add(item);
        }

        private ItemCollection Items
        {
            get => ItemsControl.GetItemsOwner(this).Items;
        }

        private void InsertAndMeasureItem(UIElement item, int itemIndex, bool isRecycled, bool isNewElement)
        {
            if (InternalChildren == null || !InternalChildren.Cast<UIElement>().Contains(item))
                AddInternalChild(item);

            void UpdateItem(ListBoxItem item)
            {
                var context = Items[itemIndex];
                if (item.DataContext == context && item.Content == context) return;

                item.DataContext = context;
                item.Content = context;
                if (item.IsSelected) item.IsSelected = false;
                item.InvalidateMeasure();
                foreach (var i in item.GetVisualChildren<UIElement>())
                {
                    i.InvalidateMeasure();
                }
            }

            switch ((item, isRecycled, isNewElement))
            {
                case (ListViewItem i, true, false):
                    _ = _recycled.Remove(i);
                    UpdateItem(i);
                    break;
                case (_, _, true):
                    ItemContainerGenerator.PrepareItemContainer(item);
                    break;
                case (ListViewItem i, false, false):
                    UpdateItem(i);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            item.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
        }


        private void UpdateExtent()
        {
            (int, double) GetCountAndWidth(ListView l)
            {
                var viewPortWidth = l.View is System.Windows.Controls.GridView gridView
                    ? gridView.Columns.Sum(c => c.ActualWidth)
                    : _viewPort.Width;
                return (l.Items.Count, viewPortWidth);
            }

            var (count, width) = ItemsControl.GetItemsOwner(this) is ListView l ? GetCountAndWidth(l) : (0, 0);
            var extent = new Size(width > 0 ? Math.Max(width, _viewPort.Width) : 0, count);
            if (extent != _extent)
            {
                _extent = extent;
                SetVerticalOffset(_offset.Y);
                ScrollOwner?.InvalidateScrollInfo();
            }
        }

        private void UpdateViewPort(Size availableSize, double visibleItemCount)
        {
            var viewPort = new Size(availableSize.Width, visibleItemCount);
            _viewPortHeightInPixels = availableSize.Height;

            if (viewPort == _viewPort) return;

            _viewPort = viewPort;
            ScrollOwner?.InvalidateScrollInfo();
            SetVerticalOffset(VerticalOffset);
        }

        public bool CanVerticallyScroll { get; set; }

        public bool CanHorizontallyScroll { get; set; }

        public double ExtentWidth => _extent.Width;

        public double ExtentHeight => _extent.Height;

        public double ViewportWidth => _viewPort.Width;

        public double ViewportHeight => _viewPort.Height;

        public double HorizontalOffset => _offset.X;

        public double VerticalOffset => _offset.Y;

        public ScrollViewer? ScrollOwner { get; set; }

        public void LineDown() => ScrollDown(20);

        public void LineLeft() => SetHorizontalOffset(HorizontalOffset - _viewPort.Width / 2);

        public void LineRight() => SetHorizontalOffset(HorizontalOffset + _viewPort.Width / 2);

        public void LineUp() => ScrollUp(20);

        public Rect MakeVisible(Visual visual, Rect rectangle) => new Rect();

        public void MouseWheelDown() => ScrollDown(ScrollUnitPixels);

        public void MouseWheelLeft() => SetHorizontalOffset(HorizontalOffset - _viewPort.Width / 2.0);

        public void MouseWheelRight() => SetHorizontalOffset(HorizontalOffset + _viewPort.Width / 2.0);

        public void MouseWheelUp() => ScrollUp(ScrollUnitPixels);

        public void PageDown() => ScrollDown(_viewPortHeightInPixels);

        public void PageLeft() => SetHorizontalOffset(_offset.X - _viewPort.Width);

        public void PageRight() => SetHorizontalOffset(_offset.X + _viewPort.Width);

        public void PageUp() => ScrollUp(_viewPortHeightInPixels);

        public void SetHorizontalOffset(double offset)
        {
            var fixedOffset =
                offset switch
                {
                    _ when offset < 0 || _viewPort.Width > _extent.Width => 0.0,
                    _ when offset + _viewPort.Width >= _extent.Width => _extent.Width - _viewPort.Width,
                    _ => offset
                };
            _offset.X = fixedOffset;
            _trans.X = -fixedOffset;
        }

        public void SetVerticalOffset(double offset)
        {
            UpdateExtent();

            var fixedVerticalOffset = offset switch
            {
                _ when offset < 0 || _viewPort.Height >= _extent.Height => 0.0,
                _ when (offset + _viewPort.Height >= _extent.Height) => _extent.Height - _viewPort.Height,
                _ => offset
            };

            var newOffset = new Point(_offset.X, fixedVerticalOffset);

            if (newOffset == _offset) return;

            _offset = newOffset;
            ScrollOwner?.InvalidateScrollInfo();
            InvalidateMeasure();
        }

        private void OnCurrentItemChanged(object? sender, EventArgs e)
        {
        //     if (Items.CurrentPosition == -1)
        //         Items.MoveCurrentToPosition(_currentPosition);
        //     else
        //         _currentPosition = Items.CurrentPosition;
                    
            // TODO
            // throw new NotImplementedException();
        }

        private void ScrollUp(double distance)
        {
            var firstVisibleItem =
                _visibleItems.Search(v => LessOrEquals(v.UpperBound, 0)
                                          && v.LowerBound > 0);

            if (firstVisibleItem == null) return;

            var itemContainerGenerator = (ItemContainerGenerator) ItemContainerGenerator;
            using var itemGenerator = new ItemGenerator(itemContainerGenerator, GeneratorDirection.Backward);

            var firstVisibleItemValue = firstVisibleItem.Value;
            var currentOffset = firstVisibleItemValue.UpperBound;
            var currentIndex = firstVisibleItemValue.Index;
            var builtDistance = -distance;

            while (currentOffset > -distance)
            {
                currentIndex--;
                var newItem = itemGenerator.GenerateNext(currentIndex, out var isNewlyRealized);

                if (newItem is UIElement itm)
                {
                    InsertAndMeasureItem(itm, currentIndex, _recycled.Contains(itm), isNewlyRealized);
                    var itemHeight = itm.DesiredSize.Height;
                    _visibleItems.Add(new VisibleItem(itm, currentIndex, currentOffset - itemHeight, currentOffset));
                    currentOffset -= itemHeight;
                    continue;
                }

                builtDistance = currentOffset;
                break;
            }

            _visibleItems.Sort((a, b) => a.Index - b.Index);

            var itemToScroll = _visibleItems.Search(v => LessOrEquals(v.UpperBound, builtDistance)
                                                         && v.LowerBound > builtDistance);

            Debug.Assert(itemToScroll.HasValue);
            var itemToScrollValue = itemToScroll.Value;

            var delta = (itemToScrollValue.UpperBound - builtDistance) / itemToScrollValue.Height;
            SetVerticalOffset(itemToScrollValue.Index + delta);
        }

        private void ScrollDown(double distance)
        {
            var lastItem =
                _visibleItems.Search(v => v.UpperBound < _viewPortHeightInPixels
                                          && GreaterOrEquals(v.LowerBound, _viewPortHeightInPixels));

            if (lastItem == null) return;

            var lastItemValue = lastItem.Value;
            var currentOffset = lastItemValue.LowerBound;
            var currentIndex = lastItemValue.Index;

            var itemContainerGenerator = (ItemContainerGenerator) ItemContainerGenerator;
            using var itemGenerator = new ItemGenerator(itemContainerGenerator, GeneratorDirection.Forward);
            var builtDistance = distance;
            while (currentOffset - _viewPortHeightInPixels < distance)
            {
                currentIndex++;
                var newItem = itemGenerator.GenerateNext(currentIndex, out var isNewlyRealized);

                if (newItem is UIElement itm)
                {
                    InsertAndMeasureItem(itm, currentIndex, _recycled.Contains(itm), isNewlyRealized);
                    var itemHeight = itm.DesiredSize.Height;
                    _visibleItems.Add(new VisibleItem(itm, currentIndex, currentOffset, currentOffset + itemHeight));
                    currentOffset += itemHeight;
                    continue;
                }

                builtDistance = currentOffset - _viewPortHeightInPixels;
                break;
            }

            var itemToScroll = _visibleItems.Search(v => v.UpperBound < builtDistance
                                                         && GreaterOrEquals(v.LowerBound, builtDistance));

            Debug.Assert(itemToScroll.HasValue);
            var itemToScrollValue = itemToScroll.Value;

            var delta = (builtDistance - itemToScrollValue.UpperBound) / itemToScrollValue.Height;
            SetVerticalOffset(itemToScrollValue.Index + delta);
        }

        private const double Epsilon = 0.00001;

        private const double ScrollUnitPixels = 60;

        private static bool Less(double d1, double d2) => d1 + Epsilon < d2;

        private static bool Greater(double d1, double d2) => d1 > d2 + Epsilon;

        private static bool GreaterOrEquals(double d1, double d2) => !Less(d1, d2);

        private static bool LessOrEquals(double d1, double d2) => !Greater(d1, d2);
    }
}