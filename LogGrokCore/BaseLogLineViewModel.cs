using System;
using LogGrokCore.Controls;
using LogGrokCore.MarkedLines;

namespace LogGrokCore
{
    public abstract class BaseLogLineViewModel : ItemViewModel, ILineMark
    {
        private readonly Selection _markedLines;

        protected BaseLogLineViewModel(int index, Selection markedLines)
        {
            Index = index;
            IndexViewModel = new LinePartViewModel(HashCode.Combine(-1, index), Index.ToString());
            _markedLines = markedLines;
            _markedLines.Changed += () => InvokePropertyChanged(nameof(IsMarked));
        }
        
        public int Index { get; }

        public LinePartViewModel IndexViewModel { get; }
        
        public bool IsMarked
        {
            get => _markedLines.Contains(Index);
            set
            {
                if (value)
                    _markedLines.Add(Index);
                else 
                    _markedLines.Remove(Index);
            }
        }
    }
}