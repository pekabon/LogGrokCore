﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using MahApps.Metro.Controls;

namespace LogGrokCore.Controls.ListControls.VirtualizingStackPanel
{
    public partial class VirtualizingStackPanel : VirtualizingPanel, IScrollInfo
    {
        private List<VisibleItem> _visibleItems = new();
        private readonly Stack<ListViewItem> _recycled = new();

        private Size _viewPort;
        private Size _extent;
        private Point _offset;
        private double _viewPortHeightInPixels;

        public VirtualizingStackPanel()
        {
            IGrowingCollection? currentItems = null;

            void UpdateGrowingCollectionSubscription()
            {
                if (ItemsControl.GetItemsOwner(this)?.ItemsSource is not IGrowingCollection newItems ||
                    currentItems == newItems) return;

                currentItems = newItems;
                currentItems.CollectionGrown += _ => InvalidateMeasure();
                currentItems.SourceChanged += () =>
                {
                    _selection.Clear();
                    InvalidateMeasure();
                };
            }

            Loaded += (_, _) =>
            {
                if (GetIsVirtualizingWhenGrouping(this))
                    throw new NotSupportedException("VirtualizingWhenGrouping is not supported");

                // ReSharper disable once UnusedVariable
                var necessaryChildrenTouch = Children;
                var itemContainerGenerator = (ItemContainerGenerator) ItemContainerGenerator;
                itemContainerGenerator.ItemsChanged += (_, _) =>
                {
                    UpdateGrowingCollectionSubscription();

                    if (Items.Count <= 0) return;

                    CurrentPosition = Math.Min(CurrentPosition, Items.Count - 1);
                    
                    foreach (var visibleItem in _visibleItems)
                    {
                        SetIsCurrentItem(visibleItem.Element, visibleItem.Index == CurrentPosition);
                    }
                };

                UpdateGrowingCollectionSubscription();
            };

            _selection.Changed += () =>
            {
                ListView.UpdateReadonlySelectedItems();
                SelectionChanged?.Invoke();
            };
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            UpdateViewPort(availableSize);
            UpdateExtent();
            
            BuildVisibleItems(availableSize, VerticalOffset);

            var maxWidth = 
                _visibleItems.Any() ? _visibleItems.Max(item => item.Element.DesiredSize.Width) : 0.0;
            VisibleItemsMaxWidth = maxWidth;

            var visibleItemsHeight = _visibleItems.Sum(v => v.Height);
            IsViewportIsCompletelyFilled = visibleItemsHeight >= availableSize.Height;
            
            return double.IsPositiveInfinity(availableSize.Height) ? 
                new Size(maxWidth, visibleItemsHeight) : 
                new Size(Math.Max(maxWidth, availableSize.Width), availableSize.Height);
        }
        
        public double VisibleItemsMaxWidth { get; private set; }
        public bool IsViewportIsCompletelyFilled { get; private set; }
      
        protected override Size ArrangeOverride(Size finalSize)
        {
            var screenBound = finalSize.Height;

            var invisibleItemOffset = screenBound;
            foreach (var recycledItem in _recycled)
            {
                var desiredSizeHeight = recycledItem.DesiredSize.Height;
                var childRect = new Rect(0, invisibleItemOffset, recycledItem.DesiredSize.Width, desiredSizeHeight);
                recycledItem.Arrange(childRect);
                invisibleItemOffset += desiredSizeHeight;
            }

            var onScreenCount = _visibleItems
                .Where(v =>
                    GreaterOrEquals(v.LowerBound, 0.0) && LessOrEquals(v.UpperBound, screenBound))
                .Select(v =>
                    (Math.Min(v.LowerBound, screenBound) - Math.Max(v.UpperBound, 0.0)) / (v.LowerBound - v.UpperBound))
                .Sum();

            UpdateViewPort(onScreenCount);
            UpdateExtent();
            
            foreach (var (item, _, upperBound, lowerBound) in _visibleItems)
            {
                var childRect = new Rect(-_offset.X, upperBound, item.DesiredSize.Width, lowerBound - upperBound);
                item.Arrange(childRect);
            }

            return finalSize;
        }

        private void BuildVisibleItems(Size availableSize, double verticalOffset)
        {
            var firstVisibleItemIndex = (int) Math.Floor(verticalOffset);
            var startOffset = firstVisibleItemIndex - verticalOffset;
            
            if (InternalChildren.Count == 0)
                _visibleItems.Clear();
            
            var (newVisibleItems, itemsToRecycle) =
                GenerateItemsDownWithRelativeOffset(
                    startOffset, firstVisibleItemIndex, availableSize.Height, _visibleItems);

            _visibleItems = newVisibleItems;

            RecycleItems(itemsToRecycle);
            UpdateSelection();
        }

        private void RecycleItems(IEnumerable<VisibleItem> itemsToRecycle)
        {
            foreach (var (uiElement, _, _, _)  in itemsToRecycle)
            {
                uiElement.Visibility = Visibility.Collapsed;
                _recycled.Push(uiElement);
            }
        }

        private (List<VisibleItem> newVisibleItems, List<VisibleItem> newItemsToRecycle)
            GenerateItemsDownWithRelativeOffset(
                double relativeOffset, int startIndex,
                double heightToBuild, List<VisibleItem> currentVisibleItems)
        {
            double? currentOffset = null;
            var currentIndex = startIndex;

            var oldItems = new HashSet<VisibleItem>(currentVisibleItems, new GenericEqualityComparer<VisibleItem>());
            var newItems = new List<VisibleItem>();

            while ((currentOffset == null || currentOffset < heightToBuild) && currentIndex < Items.Count)
            {
                var index = currentIndex;
                var currentItem =
                    currentVisibleItems.Search(current =>
                        current.Index == index && current is { Element: ContentControl contentControl }
                                               && (contentControl.Content?.Equals(Items[index]) ?? false));

                ListViewItem? itemToAdd;
                
                if (currentItem is {} foundItem)
                {
                    oldItems.Remove(foundItem);
                    itemToAdd = foundItem.Element;
                }
                else
                {
                    itemToAdd = GenerateElement(currentIndex);
                }

                if (itemToAdd == null)
                    break;

                var itemHeight = itemToAdd.DesiredSize.Height;
                currentOffset ??= itemHeight * relativeOffset;
                newItems.Add(new VisibleItem(itemToAdd, currentIndex, currentOffset.Value,
                    currentOffset.Value + itemHeight));
                currentOffset += itemHeight;
                currentIndex++;
            }

            return (newItems, oldItems.ToList());
        }

        private IList Items =>
            ItemsControl.GetItemsOwner(this) switch
            {
                null => new ArrayList(),
                { IsGrouping: true } when this.TryFindParent<GroupItem>() is
                    { Content: CollectionViewGroup group } => group.Items,
                { Items: { } items } => items
            };

        private void InsertAndMeasureItem(ListViewItem item, int itemIndex, bool isNewElement)
        {
            if (!InternalChildren.Cast<UIElement>().Contains(item))
                AddInternalChild(item);

            void UpdateItem(ListBoxItem itm)
            {
                var context = Items[itemIndex];
                if (itm.DataContext == context && itm.Content == context) return;
                itm.DataContext = this.DataContext;
                itm.Content = context;
                if (itm.IsSelected) itm.IsSelected = false;

                item.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            }

            if (isNewElement)
            {
                ListView.PrepareItemContainer(item, Items[itemIndex]!);
                UpdateItem(item);
            }
            else
            {
                UpdateItem(item);
            }
            
        }

        private Size GetExtent()
        {
            if (!(ItemsControl.GetItemsOwner(this) is System.Windows.Controls.ListView listView))
                return new Size(0, 0);

            var extentWidth =
                listView.View switch
                {
                    System.Windows.Controls.GridView gridView => gridView.Columns.Sum(column => column.ActualWidth),
                    _ => 0.0
                };

            return new Size(Math.Max(extentWidth, _viewPort.Width), listView.Items.Count);
        }
        
        private void UpdateExtent()
        {
            var extent = GetExtent();
            if (extent == _extent) return;
            _extent = extent;
            SetVerticalOffset(_offset.Y);
            ScrollOwner?.InvalidateScrollInfo();
        }

        private void UpdateViewPort(Size availableSize)
        {
            var newViewPort = new Size(availableSize.Width, _viewPort.Height);
            _viewPortHeightInPixels = availableSize.Height;

            SetViewPort(newViewPort);
        }

        private void UpdateViewPort(double visibleItemsCount)
        {
            var newViewPort = new Size(_viewPort.Width, visibleItemsCount);
            SetViewPort(newViewPort);
        }

        private void SetViewPort(Size newViewPort)
        {
            if (newViewPort == _viewPort) return;

            _viewPort = newViewPort;
            ScrollOwner?.InvalidateScrollInfo();
            SetVerticalOffset(VerticalOffset);
        }

        public bool CanVerticallyScroll { get; set; }

        public bool CanHorizontallyScroll { get; set; }

        public double ExtentWidth => GetExtent().Width;

        public double ExtentHeight => GetExtent().Height;

        public double ViewportWidth => _viewPort.Width;

        public double ViewportHeight => _viewPort.Height;

        public double HorizontalOffset => _offset.X;

        public double VerticalOffset => _offset.Y;

        public ScrollViewer? ScrollOwner { get; set; }

        public void LineDown() => ScrollDown(20);

        public void LineLeft() => SetHorizontalOffset(HorizontalOffset - _viewPort.Width / 2);

        public void LineRight() => SetHorizontalOffset(HorizontalOffset + _viewPort.Width / 2);

        public void LineUp() => ScrollUp(20);

        public Rect MakeVisible(Visual visual, Rect rectangle) => new();

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
            InvalidateArrange();
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

        private void ScrollUp(double distance)
        {
            var firstVisibleItem =
                _visibleItems.Search(v => LessOrEquals(v.UpperBound, 0)
                                          && v.LowerBound > 0);

            if (firstVisibleItem == null) return;

            var firstVisibleItemValue = firstVisibleItem.Value;
            var currentOffset = firstVisibleItemValue.UpperBound;
            var currentIndex = firstVisibleItemValue.Index;
            var builtDistance = -distance;

            while (currentOffset > -distance)
            {
                currentIndex--;
                var newItem = GenerateElement(currentIndex);
                if (newItem != null)
                {
                    var itemHeight = newItem.DesiredSize.Height;
                    _visibleItems.Add(new VisibleItem(newItem, currentIndex, currentOffset - itemHeight,
                        currentOffset));
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
            SetVerticalOffset(itemToScrollValue.Index - delta);
        }

        private ListViewItem? GenerateElement(int currentIndex)
        {
            if (currentIndex >= Items.Count || currentIndex < 0)
                return null;
            
            ListViewItem? newItem;
            if (_recycled.Count > 0)
            {
                newItem = _recycled.Pop();
                newItem.Visibility = Visibility.Visible;
                InsertAndMeasureItem(newItem, currentIndex, false);
            }
            else
            {
                newItem = ListView.GetContainerForItem();
                InsertAndMeasureItem(newItem, currentIndex, true);
            }

            return newItem;
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

            var builtDistance = distance;
            
            while (currentOffset - _viewPortHeightInPixels < distance)
            {
                currentIndex++;
                var newItem = GenerateElement(currentIndex);

                if (newItem != null)
                {
                    var itemHeight = newItem.DesiredSize.Height;
                    _visibleItems.Add(new VisibleItem(newItem, currentIndex, currentOffset, currentOffset + itemHeight));
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

        private ListView ListView
        {
            get
            {
                _listView ??= ItemsControl.GetItemsOwner(this) as ListView;
                if (_listView == null) throw new InvalidOperationException();
                return _listView;
            }
        }
        private ListView? _listView;
    }
}