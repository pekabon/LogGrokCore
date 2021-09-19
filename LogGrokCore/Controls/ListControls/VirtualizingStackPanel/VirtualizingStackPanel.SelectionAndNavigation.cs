using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LogGrokCore.Controls.ListControls.VirtualizingStackPanel
{
    public class ElementClickedEventArgs : RoutedEventArgs
    {
        public ElementClickedEventArgs(RoutedEvent routedEvent, object source, int elementIndex) : base(routedEvent, source)
        {
            ElementIndex = elementIndex;

        }
        public int ElementIndex { get; init; }
    }

    public partial class VirtualizingStackPanel
    {
        public static readonly DependencyProperty CurrentPositionProperty = DependencyProperty.Register(
            "CurrentPosition", typeof(int), typeof(VirtualizingStackPanel), 
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnCurrentPositionChanged ));

        public static readonly DependencyProperty IsCurrentItemProperty = DependencyProperty.RegisterAttached(
            "IsCurrentItem", typeof(bool), typeof(VirtualizingStackPanel), new PropertyMetadata(default(bool)));

        public static readonly RoutedEvent ElementClickedEvent = 
            EventManager.RegisterRoutedEvent("ElementClicked", RoutingStrategy.Bubble, 
                typeof(RoutedEventHandler), typeof(VirtualizingStackPanel));
        
        public event RoutedEventHandler ElementClicked
        {
            add => AddHandler(ElementClickedEvent, value);
            remove => RemoveHandler(ElementClickedEvent, value);
        }
        
        public static void SetIsCurrentItem(ListBoxItem listViewItem, bool value)
        {
            listViewItem.SetValue(IsCurrentItemProperty, value);
        }

        public static bool GetIsCurrentItem(ListBoxItem listViewItem)
        {
            return (bool) listViewItem.GetValue(IsCurrentItemProperty);
        }

        private static void OnCurrentPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingStackPanel) d;
            var newValue = (int) e.NewValue;
            panel._selection.Add(newValue);
            panel.UpdateSelection();
            panel.ListView.Items.MoveCurrentToPosition(newValue);
        }

        public int CurrentPosition
        {
            get => (int) GetValue(CurrentPositionProperty);
            set => SetValue(CurrentPositionProperty, value);
        }
        
        private readonly Selection _selection = new();
        private ScrollContentPresenter? _scrollContentPresenter;

        public IEnumerable<int> SelectedIndices => _selection;

        public event Action? SelectionChanged;
        
        public bool ProcessKeyDown(Key key)
        {
            switch (key)
            {
                case Key.Down when Keyboard.Modifiers.HasFlag(ModifierKeys.Shift):
                    ExpandSelectionDown();
                    break;
                case Key.Up when Keyboard.Modifiers.HasFlag(ModifierKeys.Shift):
                    ExpandSelectionUp();
                    break;
                case Key.Down:
                    NavigateDown();
                    break;
                case Key.Up:
                    NavigateUp();
                    break;
                case Key.PageUp:
                    PageUp();
                    break;
                case Key.PageDown:
                    PageDown();
                    break;
                default:
                    return false;
            }

            UpdateSelection();
            return true;
        }

        public bool ProcessPreviewMouseDown(MouseButton changedButton)
        {
            var item = GetItemUnderMouse();
            var suitableVisibleItems = _visibleItems.Where(i => i.Element == item).ToList();

            if (item == null) return false;
            if (!suitableVisibleItems.Any()) return false;

            var element = suitableVisibleItems.Single();
            
            RaiseEvent(new ElementClickedEventArgs(ElementClickedEvent, this, element.Index));
      
            return false;
        }

        public bool ProcessMouseDown(MouseButton changedButton)
        {
            var item = GetItemUnderMouse();
            var suitableVisibleItems = _visibleItems.Where(i => i.Element == item).ToList();

            if (item == null) return false;
            if (changedButton == MouseButton.Right && item.IsSelected) return false;
            if (!suitableVisibleItems.Any()) return false;
            
            var index = suitableVisibleItems.Single().Index;
            
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (item.IsSelected)
                {
                    _selection.Remove(index);
                    item.IsSelected = false;
                    return true;
                }
            } 
            else  if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                _selection.AddRangeToValue(index);
            }
            else
            {
                _selection.Set(index);
            }

            CurrentPosition = index;

            FocusManager.SetFocusedElement(item, item);
            return true;
        }
        
        private Point? GetMousePosition()
        {
            if (ScrollContentPresenter != null)
            {
                return Mouse.GetPosition(ScrollContentPresenter);
            }

            return null;
        }

        private  ListViewItem? GetItemUnderMouse()
        {            
            var mousePosition = GetMousePosition();
            return mousePosition != null ? 
                ScrollContentPresenter?.GetItemUnderPoint<ListViewItem>(mousePosition.Value) : null;
        }

        private void UpdateSelection()
        {
            foreach (var visibleItem in _visibleItems)
            {
                var isItemSelected = _selection.Contains(visibleItem.Index);
                if (visibleItem.Element.IsSelected != isItemSelected)
                    visibleItem.Element.IsSelected = isItemSelected;
            }
        }

        public void NavigateTo(int index)
        {
            _selection.Clear();
            CurrentPosition = index;
            _selection.Add(index);
            BringIndexIntoView(CurrentPosition);
            UpdateSelection();
        }

        private void NavigateUp()
        {
            if (CurrentPosition <= 0) return;
            _selection.Clear();
            CurrentPosition--;
            BringIndexIntoView(CurrentPosition);
            UpdateSelection();
        }

        private void NavigateDown()
        {
            if (CurrentPosition >= Items.Count - 1) return;
            _selection.Clear();
            CurrentPosition++;
            BringIndexIntoViewWhileNavigatingDown(CurrentPosition);
            UpdateSelection();
        }

        private void ExpandSelectionUp()
        {
            if (CurrentPosition <= 0 ) return;

            if (_selection.Bounds is not {min: var min, max: var max})
            {
                return;
            }

            if (CurrentPosition == min)
            {
                CurrentPosition--;
                _selection.Add(CurrentPosition);
            }

            if (CurrentPosition == max)
            {
                _selection.Remove(CurrentPosition);
                CurrentPosition = max;
            }

            BringIndexIntoView(CurrentPosition);
            UpdateSelection();
        }

        private void ExpandSelectionDown()
        {
            if (CurrentPosition >= Items.Count - 1 || 
                _selection.Bounds is not {min: var min, max: var max}) return;

            if (CurrentPosition == max)
            {
                CurrentPosition++;
                _selection.Add(CurrentPosition);
            }

            if (CurrentPosition == min)
            {
                _selection.Remove(CurrentPosition);
                CurrentPosition = min;
            }
            
            BringIndexIntoViewWhileNavigatingDown(CurrentPosition);
            UpdateSelection();
        }

        protected override void BringIndexIntoView(int index)
        {
            if (!_visibleItems.Any(element => element.Index == index
                                              && GreaterOrEquals(element.UpperBound, 0.0)
                                              && LessOrEquals(element.LowerBound, ActualHeight)))
            {
                SetVerticalOffset(index);
            }
        }

        private void BringIndexIntoViewWhileNavigatingDown(int index)
        {
            var screenBound = ActualHeight;
            VisibleItem? existed = _visibleItems.Find(v => v.Index == index);

            switch (existed)
            {
                case ({ }, _, _, { } lowerBound)
                    when lowerBound > screenBound:
                    ScrollDown(lowerBound - screenBound);
                    break;

                case (null, _, _, _) when _visibleItems.Max(v => v.Index) == index - 1:
                    var nextItem = GenerateOneItemDown();
                    if (nextItem != null)
                        ScrollDown(nextItem.Value.Height);
                    break;
                case (null, _, _, _):
                    SetVerticalOffset(index);
                    break;
            }
        }

        private VisibleItem? GenerateOneItemDown()
        {
            BuildVisibleItems(
                new Size(ActualWidth, _visibleItems[^1].LowerBound + 10.0),
                VerticalOffset);

            return _visibleItems[^1];
        }

        private ScrollContentPresenter? ScrollContentPresenter
        {
            get
            {
                var host = ItemsControl.GetItemsOwner(this); 
                _scrollContentPresenter ??= 
                    host
                        .GetVisualChildren<ScrollContentPresenter>()
                        .Where(c => c.Content is ItemsPresenter)
                        .FirstOrDefault(c 
                            => ReferenceEquals(((ItemsPresenter)(c.Content)).TemplatedParent, host));

                return _scrollContentPresenter;
            }
        }
    }
}