using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using Microsoft.Win32;
using RunningPrograms.Handlers.Chrome;
using RunningPrograms.Handlers.Devenv;

namespace RunningPrograms
{
    internal static class ProgramChoices
    {
        internal const string VisualStudio = VisualStudioHandler.ExecutableName;
        internal const string Chrome = ChromeHandler.ExecutableName;
        internal const string SettingsPath = @"Software\RunningPrograms";
        private const string Setting = "WindowPrograms";

        internal static string Normalize(string name)
        {
            name = name.Trim();
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^4];
            }

            return name.ToLowerInvariant() switch
            {
                "visual studio" => VisualStudio,
                "vs code" or "vscode" or "visual studio code" => "code.exe",
                "vs code insiders" or "visual studio code insiders" => "code - insiders.exe",
                "google chrome" => Chrome,
                "microsoft edge" or "edge" => "msedge.exe",
                "" or "." or ".." => null,
                _ => name.EndsWith('.') || name.EndsWith(' ') || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                    ? null : name + ".exe"
            };
        }

        internal static IEnumerable<string> Load()
        {
            string[] saved = null;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(SettingsPath);
                saved = key?.GetValue(Setting) as string[];
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                // The default remains available when user settings cannot be read.
            }

            return new[] { VisualStudio }.Concat((saved ?? Array.Empty<string>()).Select(Normalize)
                .Where(program => program != null)).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        internal static bool Save(IEnumerable<string> programs)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(SettingsPath);
                if (key == null) return false;
                key.SetValue(Setting, programs.ToArray(), RegistryValueKind.MultiString);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                return false;
            }
        }
    }
}
