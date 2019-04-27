using System;
using System.IO;

namespace LogGrokCore
{
    public class Document
    {
        private readonly Lazy<string> _content;

        public string FilePath { get; }

        public string Content => _content.Value;

        public Document(string filePath)
        {
            FilePath = filePath;

            _content = new Lazy<string>(() => File.ReadAllText(FilePath), true);
        }
    }
}
