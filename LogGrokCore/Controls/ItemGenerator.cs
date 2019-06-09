using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LogGrokCore.Controls
{
    internal class ItemGenerator : IDisposable
    {
        private readonly IItemContainerGenerator _itemContainerGenerator;
        private readonly IDisposable _batches;
        private readonly GeneratorDirection _direction;

        private IDisposable? _generatorState;
        private int? _lastIndex;

        public ItemGenerator(ItemContainerGenerator itemContainerGenerator, GeneratorDirection direction)
        {
            _itemContainerGenerator = itemContainerGenerator;
            _batches = itemContainerGenerator.GenerateBatches();
            _direction = direction;
        }

        public DependencyObject? GenerateNext(int currentIndex, out bool isNewlyRealized)
        {
            var supposedPrevIndex = (_direction == GeneratorDirection.Forward)
                                    ? currentIndex - 1 : currentIndex + 1;
            if (currentIndex < 0)
            {
                isNewlyRealized = false;
                return null;
            }

            if (_generatorState == null || _lastIndex is int idx && idx != supposedPrevIndex)
            {
                _generatorState?.Dispose();
                var position = _itemContainerGenerator.GeneratorPositionFromIndex(currentIndex);
                _generatorState = _itemContainerGenerator.StartAt(position, _direction, true);
            }

            var result = _itemContainerGenerator.GenerateNext(out isNewlyRealized);
            if (result != null) _lastIndex = currentIndex;
            return result;
        }

        public void Dispose()
        {
            _generatorState?.Dispose();
            _batches.Dispose();
        }
    }
}
