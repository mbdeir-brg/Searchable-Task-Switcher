using System;
using System.Globalization;
using System.IO;
using System.Security;
using System.Windows.Input;
using Microsoft.Win32;

namespace RunningPrograms
{
    internal static class ShortcutPreferences
    {
        private const string SettingName = "GlobalShortcut";

        internal static ShortcutKey Load()
        {
            try
            {
                using RegistryKey settings = Registry.CurrentUser.OpenSubKey(ProgramChoices.SettingsPath);
                var parts = (settings?.GetValue(SettingName) as string)?.Split(',');
                if (parts?.Length == 2 && int.TryParse(parts[0], out int key) && int.TryParse(parts[1], out int modifiers) &&
                    ShortcutKey.IsValid((Key)key, (ModifierKeys)modifiers))
                {
                    return new ShortcutKey((Key)key, (ModifierKeys)modifiers);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException)
            {
                // Use the default when the per-user setting is unavailable.
            }

            return new ShortcutKey(Key.Tab, ModifierKeys.Control);
        }

        internal static bool Save(ShortcutKey shortcut)
        {
            try
            {
                using RegistryKey settings = Registry.CurrentUser.CreateSubKey(ProgramChoices.SettingsPath);
                if (settings == null) return false;

                settings.SetValue(SettingName, string.Format(CultureInfo.InvariantCulture, "{0},{1}",
                    (int)shortcut.Key, (int)shortcut.Modifiers), RegistryValueKind.String);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException)
            {
                return false;
            }
        }
    }
}
