using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace RunningPrograms
{
    internal static class StartupPreferences
    {
        internal const string StartupArgument = "--startup";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const int MaxCommandLength = 260;

        internal static bool IsEnabled()
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(App.DisplayName) is string command && !string.IsNullOrWhiteSpace(command);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                return false;
            }
        }

        internal static bool TrySetEnabled(bool enabled)
        {
            try
            {
                if (!enabled)
                {
                    using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                    key?.DeleteValue(App.DisplayName, throwOnMissingValue: false);
                    return true;
                }

                string command = CreateStartupCommand(Environment.ProcessPath, typeof(App).Assembly.Location);
                if (command == null)
                {
                    return false;
                }

                using RegistryKey startup = Registry.CurrentUser.CreateSubKey(RunKey);
                if (startup == null)
                {
                    return false;
                }

                startup.SetValue(App.DisplayName, command, RegistryValueKind.String);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                return false;
            }
        }

        private static string CreateStartupCommand(string processPath, string assemblyPath)
        {
            if (string.IsNullOrEmpty(processPath))
            {
                return null;
            }

            string command = $"\"{processPath}\"";
            if (string.Equals(Path.GetFileName(processPath), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(assemblyPath))
                {
                    return null;
                }

                command += $" \"{assemblyPath}\"";
            }

            command += $" {StartupArgument}";
            return command.Length <= MaxCommandLength ? command : null;
        }
    }
}
