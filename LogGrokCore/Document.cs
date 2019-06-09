using LogGrokCore.Data;
using LogGrokCore.Data.Virtualization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LogGrokCore
{
    public class Document
    {
        private readonly Loader _loader;

        public string FilePath { get; }

        public VirtualList<string> Lines { get; }

        public bool IsLoading => _loader.IsLoading;

        public Document(string filePath)
        {
            FilePath = filePath;
            _loader = new Loader(() => OpenFile(FilePath));
            Lines = new VirtualList<string>(_loader.LineProvider);
        }

        private static Stream OpenFile(string fileName)
        {
            const int bufferSize = 64 * 1024;
            return new FileStream(fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize,
                options: FileOptions.SequentialScan);
        }
    }
}
