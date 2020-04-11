using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LogGrokCore.Controls
{
    internal class ItemGenerator : IDisposable
    {
        private readonly ItemContainerGenerator _itemContainerGenerator;
        private readonly GeneratorDirection _direction;

        private IDisposable? _batches;
        private IDisposable? _generatorState;
        private int? _lastIndex;

        public ItemGenerator(ItemContainerGenerator itemContainerGenerator, GeneratorDirection direction)
        {
            _itemContainerGenerator = itemContainerGenerator;
            _direction = direction;
        }

        public DependencyObject? GenerateNext(int currentIndex, out bool isNewlyRealized)
        {
            if (currentIndex < 0 || currentIndex >= _itemContainerGenerator.Items.Count)
            {
                isNewlyRealized = false;
                return null;
            }

            _batches ??= _itemContainerGenerator.GenerateBatches();
            var generator = (IItemContainerGenerator) _itemContainerGenerator;
            var supposedPrevIndex = (_direction == GeneratorDirection.Forward)
                ? currentIndex - 1
                : currentIndex + 1;

            if (_generatorState == null || _lastIndex is int idx && idx != supposedPrevIndex)
            {
                _generatorState?.Dispose();
                var position = generator.GeneratorPositionFromIndex(currentIndex);
                _generatorState = generator.StartAt(position, _direction, true);
            }

            var result = generator.GenerateNext(out isNewlyRealized);
            if (result != null) _lastIndex = currentIndex;
            return result;
        }

        public void Dispose()
        {
            _generatorState?.Dispose();
            _batches?.Dispose();
        }
    }
}