using System;
using System.Configuration;
using LogGrokCore.Data;

namespace LogGrokCore
{
    public enum LogLineMode
    {
        Normal,
        Selectable
    }

    internal class LineViewModel : ItemViewModel
    {
        private readonly string _sourceString;
        private readonly ParseResult _parseResult;
        private LogLineMode _mode;

        public LineViewModel(int index, string sourceString, ILineParser parser)
        {
            Index = index;
            _sourceString = sourceString;
            _parseResult = parser.Parse(sourceString);
        }
        
        public int Index { get; }

        public LogLineMode Mode
        {
            get => _mode;
            set => SetAndRaiseIfChanged(ref _mode, value);
        }

        public string this[int index] => GetValue(index);
        
        public DelegateCommand SwitchToEditModeCommand => 
            new DelegateCommand(() => { Mode = LogLineMode.Selectable; });

        public string GetValue(int index)
        {
            var lineMeta = _parseResult.Get().ParsedLineComponents;
            return _sourceString.Substring(lineMeta.ComponentStart(index), 
                lineMeta.ComponentLength(index))
                    .TrimEnd('\0').TrimEnd();
        }

        public override string ToString()
        {
            return _sourceString;
        }
    }
}