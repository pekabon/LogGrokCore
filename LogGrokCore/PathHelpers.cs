using System;
using System.IO;

namespace LogGrokCore
{
    public static class PathHelpers
    {
        public static string GetLocalFilePath(string fileName) => Path.Combine(AppContext.BaseDirectory, fileName);
    }
}