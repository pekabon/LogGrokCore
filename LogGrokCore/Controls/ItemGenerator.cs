using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LogGrokCore.Controls
{
    internal class ItemGenerator : IDisposable
    {
        private readonly ItemContainerGenerator? _itemContainerGenerator;
        private readonly GeneratorDirection _direction;

        private IDisposable? _batches;
        private IDisposable? _generatorState;
        private int? _lastIndex;

        private Style _itemContainerStyle;
        private IList _sourceCollection;
        private Queue<ListViewItem> _recycled = new Queue<ListViewItem>();
        private Dictionary<ListViewItem, int> _containerIndices = new Dictionary<ListViewItem, int>();
        public ItemGenerator(
            Style itemContainerStyle,
            IList sourceCollection)
        {
            _itemContainerStyle = itemContainerStyle;
            _sourceCollection = sourceCollection;
        }

        public void Recycle(ListViewItem item)
        {
            item.Visibility = Visibility.Collapsed;
            _recycled.Enqueue(item);
            _containerIndices.Remove(item);
        }

        public ListViewItem CreateContainerForItem(object item, out bool isNewlyRealized)
        {
            if (_recycled.TryDequeue(out var recycledContainer))
            {
                //                recycledContainer.DataContext = item;
                //                recycledContainer.Content = item;
                if (recycledContainer.Content?.GetType() != item.GetType())
                    recycledContainer.ApplyTemplate();

                recycledContainer.Visibility = Visibility.Visible;
                isNewlyRealized = false;
                return recycledContainer;
            }

            var container = new ListViewItem
            {
//                Content = item, 
//                DataContext = item, 
//                Style = _itemContainerStyle
            };
            isNewlyRealized = true;
            return container;
        }

        
        public ListViewItem? GenerateNext(int currentIndex, out bool isNewlyRealized)
        {
            if (currentIndex >= _sourceCollection.Count || currentIndex < 0)
            {
                isNewlyRealized = false;
                return null;
            }

            var item = _sourceCollection[currentIndex];
            var container = CreateContainerForItem(item, out isNewlyRealized);
            _containerIndices[container] = currentIndex;
            return container;
        }

        public int GetIndexFromContainer(ListViewItem container)
        {
            if (_containerIndices.TryGetValue(container, out var index))
            {
                return index;
            }

            return -1;
        }

        public void Dispose()
        {
            _generatorState?.Dispose();
            _batches?.Dispose();
        }
    }
}
