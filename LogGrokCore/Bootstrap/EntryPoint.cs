using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using DryIoc;
using Microsoft.Win32;
using Microsoft.Xaml.Behaviors.Media;

namespace LogGrokCore.Bootstrap
{
    static class EntryPoint
    {
        private const string EnableWerOnly = "EnableWerOnly";
        private const string DisableWerOnly = "DisableWerOnly";
        private static string LogGrokErrorReportingKeyPath = $@"SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\{Path.GetFileName(Environment.ProcessPath)}";

        [STAThread]
        public static void Main(string[] args)
        {
            var command = args.SingleOrDefault();

            ConfigureErrorReporting(command);

            if (IsNeedStopExecution(command))
                return;

            var fullPaths = args.Select(Path.GetFullPath).ToArray();
            var manager = new SingleInstanceManager();
            manager.Run(fullPaths);
        }

        private static bool IsNeedStopExecution(string? command)
        {
            return command == EnableWerOnly || command == DisableWerOnly;
        }

        private static void EvaluateIfNeed(Action func, string arguemntToEvaluatedProcess, string? currentCommand)
        {
            try
            {
                func();
            }
            catch (UnauthorizedAccessException)
            {
                if (IsNeedStopExecution(currentCommand))
                    return;

                StartElevatedInstanceWithParam(arguemntToEvaluatedProcess);
            }
        }

        private static void ConfigureErrorReporting(string? command)
        {
            var isErrorReportingEnabled = IsErrorReportingEnabled();
            var isCrashDumpsEnabled = ApplicationSettings.Instance().DebugSettings.EnableCrashDumps;
            var isErrorReportingSettingsWasChanged = IsErrorReportingSettingsWasChanged();

            var isNeedEnableWer = (isCrashDumpsEnabled && !isErrorReportingEnabled) || command == EnableWerOnly || isErrorReportingSettingsWasChanged;
            var isNeedDisableWer = (!isCrashDumpsEnabled && isErrorReportingEnabled) || command == DisableWerOnly;

            if (isNeedEnableWer)
            {
                EvaluateIfNeed(EnableErrorReporting, EnableWerOnly, command);
            }
            else if (isNeedDisableWer)
            {
                EvaluateIfNeed(DisableErrorReporting, DisableWerOnly, command);
            }
        }

        private static bool IsErrorReportingSettingsWasChanged()
        {
            var key = Registry.LocalMachine.OpenSubKey(LogGrokErrorReportingKeyPath);

            if (key == null) 
                return false;

            var maxDumpsCount = ApplicationSettings.Instance().DebugSettings.MaxDumpsCount;

            return (int)key.GetValue("DumpCount", -1) != maxDumpsCount;
        }

        private static bool IsErrorReportingEnabled()
        {
            var key = Registry.LocalMachine.OpenSubKey(LogGrokErrorReportingKeyPath);
            return key != null;
        }

        private static void DisableErrorReporting()
        {
            Registry.LocalMachine.DeleteSubKey(LogGrokErrorReportingKeyPath);
        }

        private static void EnableErrorReporting()
        {

            var key = Registry.LocalMachine.CreateSubKey(LogGrokErrorReportingKeyPath);
            key.SetValue("DumpFolder",
                HomeDirectoryPathProvider.GetDirectoryFullPath("Dumps"),
                RegistryValueKind.ExpandString);

            var maxDumpsCount = ApplicationSettings.Instance().DebugSettings.MaxDumpsCount;
            key.SetValue("DumpCount", maxDumpsCount, RegistryValueKind.DWord);
            key.SetValue("DumpType", 2, RegistryValueKind.DWord);
        }

        private static void StartElevatedInstanceWithParam(string type)
        {
            var processPath = Environment.ProcessPath;
            if (processPath == null)
                return;

            var info = new ProcessStartInfo(processPath)
            {
                UseShellExecute = true,
                Arguments = type,
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
