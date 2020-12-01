using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Wpf.AvalonDock.Controls;

namespace LogGrokCore.Controls
{
    public partial class VirtualizingStackPanel
    {
        private readonly Selection _selection = new();
        private ScrollContentPresenter? _scrollContentPresenter;

        private int CurrentPosition
        {
            get => Items.CurrentPosition;
            set
            {
                Items.MoveCurrentToPosition(value);
                _selection.Add(value);
                UpdateSelection();
            }
        }

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

        public bool ProcessMouseDown()
        {
            var item = GetItemUnderMouse();
            if (item == null) return false;

            var index = _visibleItems.Single(i => i.Element == item).Index;

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
                _selection.Clear();
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
            return mousePosition != null ? GetItemUnderPoint(mousePosition.Value) : null;
        }
        
        private ListViewItem? GetItemUnderPoint(Point p)
        {
            HitTestFilterBehavior VisibilityHitTestFilter(DependencyObject target)
            {
                if (!(target is UIElement uiElement)) 
                    return HitTestFilterBehavior.Continue;
                if(!uiElement.IsHitTestVisible || !uiElement.IsVisible)
                    return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                return HitTestFilterBehavior.Continue;
            }
            
            if (ScrollContentPresenter == null) return null;
            ListViewItem? hitTestResult = null; 
            VisualTreeHelper.HitTest(ScrollContentPresenter, 
                                            VisibilityHitTestFilter,
                                            t =>
                                            {
                                                var foundItem =  t.VisualHit.FindVisualAncestor<ListViewItem>();
                                                if (foundItem == null)
                                                    return HitTestResultBehavior.Continue;
                                                hitTestResult = foundItem;
                                                return HitTestResultBehavior.Stop;

                                            }, 
                                            new PointHitTestParameters(p));

            return hitTestResult;
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
            
            
            if (CurrentPosition == _selection.Min)
            {
                CurrentPosition--;
                _selection.Add(CurrentPosition);
            }

            if (CurrentPosition == _selection.Max)
            {
                _selection.Remove(CurrentPosition);
                CurrentPosition = _selection.Max;
            }

            BringIndexIntoView(CurrentPosition);
            UpdateSelection();
        }

        private void ExpandSelectionDown()
        {
            if (CurrentPosition >= Items.Count - 1) return;

            if (CurrentPosition == _selection.Max)
            {
                CurrentPosition++;
                _selection.Add(CurrentPosition);
            }

            if (CurrentPosition == _selection.Min)
            {
                _selection.Remove(CurrentPosition);
                CurrentPosition = _selection.Min;
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
            var screenBound = this.ActualHeight;
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