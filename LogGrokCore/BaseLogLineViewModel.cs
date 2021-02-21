using LogGrokCore.Controls;

namespace LogGrokCore
{
    public abstract class BaseLogLineViewModel : ItemViewModel
    {
        private readonly Selection _markedLines;

        protected BaseLogLineViewModel(int index, Selection markedLines)
        {
            Index = index;
            _markedLines = markedLines;
            _markedLines.Changed += () => InvokePropertyChanged(nameof(IsMarked));
        }
        
        public int Index { get; }

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