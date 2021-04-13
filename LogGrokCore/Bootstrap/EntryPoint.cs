using System;
using System.IO;
using System.Linq;

namespace LogGrokCore.Bootstrap
{
    static class EntryPoint
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var fullPaths = args.Select(Path.GetFullPath).ToArray();
            var manager = new SingleInstanceManager();
            manager.Run(fullPaths);
        }
    }
}