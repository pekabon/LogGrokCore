using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LogGrokCore
{
    public static class DataFilePathProvider
    {
        public static string GetDataFileFullPath(string dataFileName)
        {
            return Path.Combine(EnsureDirectoryExists(), dataFileName);
        }
        
        private static string EnsureDirectoryExists()
        {
            var dirName  = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.GetFileNameWithoutExtension(DirName),
                "Data");
            Directory.CreateDirectory(dirName);
            return dirName;
        }
        
        private const string DirName ="LogGrok2";
    }
}