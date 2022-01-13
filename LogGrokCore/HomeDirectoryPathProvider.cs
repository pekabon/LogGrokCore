using System;
using System.IO;

namespace LogGrokCore
{
    public static class HomeDirectoryPathProvider
    {
        public static string GetDataFileFullPath(string dataFileName)
        {
            return Path.Combine(EnsureHomeDirectoryExists(), dataFileName);
        }

        public static string GetDirectoryFullPath(string directoryName)
        {
            var home = EnsureHomeDirectoryExists();
            var dirPath = Path.Combine(home, directoryName);
            Directory.CreateDirectory(dirPath);
            return dirPath;
        }

        private static string EnsureHomeDirectoryExists()
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