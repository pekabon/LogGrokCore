using System.IO;
using System.Reflection;

namespace LogGrokCore
{
    public static class PathHelpers
    {
        public static string GetLocalFilePath(string fileName)
        {
            var assemblyPath = Assembly.GetCallingAssembly().Location;
            var directory = Path.GetDirectoryName(assemblyPath);
            
            return directory == null ? fileName : Path.Combine(directory, fileName);
        }
    }
}