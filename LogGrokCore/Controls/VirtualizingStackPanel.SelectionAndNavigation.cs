using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace LogGrokCore.Controls
{
    public partial class VirtualizingStackPanel
    {
        private readonly Selection _selection = new Selection();

        private int CurrentPosition
        {
            get => Items.CurrentPosition;
            set
            {
                Items.MoveCurrentToPosition(value);
                _selection.Add(value);
            }
        }

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

        private void UpdateSelection()
        {
            var generator = (ItemContainerGenerator) ItemContainerGenerator;
            if (generator.Status != GeneratorStatus.ContainersGenerated) return;
            foreach (var item in this.GetVisualChildren<ListViewItem>())
            {
                var index = generator.IndexFromContainer(item);
                var isItemSelected = _selection.Contains(index);
                if (item.IsSelected != isItemSelected)
                    item.IsSelected = isItemSelected;
            }
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
            throw new System.NotImplementedException();
        }

        private void ExpandSelectionDown()
        {
            throw new System.NotImplementedException();
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
                case (UIElement uiElement, _, _, double lowerBound)
                    when uiElement != null && lowerBound > screenBound:
                    ScrollDown(lowerBound - screenBound);
                    break;

                case (null, _, _, _) when _visibleItems.Max(v => v.Index) == index - 1:
                    var lastItem = _visibleItems[^1];
                    var nextItem = GenerateOneItemDown(lastItem.LowerBound, index);
                    if (nextItem != null)
                        ScrollDown(nextItem.Value.Height);
                    break;
                case (null, _, _, _):
                    SetVerticalOffset(index);
                    break;
            }
        }

        private VisibleItem? GenerateOneItemDown(double offset, int index)
        {
            BuildVisibleItems(
                new Size(ActualWidth, _visibleItems[^1].LowerBound + 10.0),
                VerticalOffset);

            return _visibleItems[^1];
        }
    }
}