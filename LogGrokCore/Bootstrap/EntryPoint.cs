using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace LogGrokCore.Bootstrap
{
    static class EntryPoint
    {
        private const string SetupWerOnly = "SetupWerOnly";
        
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                SetupWindowsErrorReporting();
            }
            catch (UnauthorizedAccessException)
            {
                if (args.FirstOrDefault() == SetupWerOnly)
                    return;

                StartElevatedInstanceToSetupWer();
            }

            if (args.SingleOrDefault() == SetupWerOnly)
                return;
            
            var fullPaths = args.Select(Path.GetFullPath).ToArray();
            var manager = new SingleInstanceManager();
            manager.Run(fullPaths);
        }

        private static void SetupWindowsErrorReporting()
        {
            var executableName = Path.GetFileName(Environment.ProcessPath);
            var keyName = $@"SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\{executableName}";
            var existingKey = Registry.LocalMachine.OpenSubKey(keyName);
            
            if (existingKey != null)
            {
                return;
            }

            var key = Registry.LocalMachine.CreateSubKey(keyName);
            key.SetValue("DumpFolder",
                HomeDirectoryPathProvider.GetDirectoryFullPath("Dumps"),
                RegistryValueKind.ExpandString);
            key.SetValue("DumpCount", 10, RegistryValueKind.DWord);
            key.SetValue("DumpType", 1, RegistryValueKind.DWord);
        }
        
        private static void StartElevatedInstanceToSetupWer()
        {
            var processPath = Environment.ProcessPath;
            if (processPath == null)
                return;

            var info = new ProcessStartInfo(processPath)
            {
                UseShellExecute = true,
                Arguments = SetupWerOnly,
                Verb = "runas"
            };

            try
            {
                var process = Process.Start(info);
                process?.WaitForExit(30000);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}