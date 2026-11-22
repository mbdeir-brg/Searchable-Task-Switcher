using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace RunningPrograms
{
    internal sealed class GlobalShortcut : IDisposable
    {
        private const int HotKeyMessage = 0x0312;
        private const uint NoRepeat = 0x4000;
        private const int HotKeyAlreadyRegistered = 1409;

        private readonly HwndSource source;
        private readonly Action onInvoked;
        private int registeredId;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr window, int id);

        public GlobalShortcut(HwndSource source, Action onInvoked)
        {
            this.source = source;
            this.onInvoked = onInvoked;
            source.AddHook(HandleMessage);
            ConfiguredShortcut = ShortcutPreferences.Load();
            Error = Register(ConfiguredShortcut, save: false);
        }

        public ShortcutKey ConfiguredShortcut { get; private set; }
        public string Error { get; private set; }

        public string Change(ShortcutKey shortcut)
        {
            if (registeredId != 0 && shortcut.Key == ConfiguredShortcut.Key && shortcut.Modifiers == ConfiguredShortcut.Modifiers)
            {
                return null;
            }

            string error = Register(shortcut, save: true);
            if (error == null)
            {
                ConfiguredShortcut = shortcut;
                Error = null;
            }

            return error;
        }

        private string Register(ShortcutKey shortcut, bool save)
        {
            // Keep the old registration active until Windows accepts the new shortcut and it is saved.
            int nextId = registeredId == 1 ? 2 : 1;
            uint modifiers = NoRepeat;
            if (shortcut.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= 0x0001;
            if (shortcut.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= 0x0002;
            if (shortcut.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= 0x0004;

            if (!RegisterHotKey(source.Handle, nextId, modifiers, (uint)KeyInterop.VirtualKeyFromKey(shortcut.Key)))
            {
                return Marshal.GetLastWin32Error() == HotKeyAlreadyRegistered
                    ? $"{shortcut} is already in use. Choose another shortcut."
                    : $"Windows could not register {shortcut}. Choose another shortcut.";
            }

            if (save && !ShortcutPreferences.Save(shortcut))
            {
                UnregisterHotKey(source.Handle, nextId);
                return "The shortcut could not be saved in your Windows settings.";
            }

            if (registeredId != 0)
            {
                UnregisterHotKey(source.Handle, registeredId);
            }

            registeredId = nextId;
            return null;
        }

        private IntPtr HandleMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == HotKeyMessage && registeredId != 0 && wParam == new IntPtr(registeredId))
            {
                handled = true;
                onInvoked();
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (source.IsDisposed)
            {
                return;
            }

            if (registeredId != 0)
            {
                UnregisterHotKey(source.Handle, registeredId);
                registeredId = 0;
            }

            source.RemoveHook(HandleMessage);
        }
    }

    internal readonly struct ShortcutKey
    {
        public ShortcutKey(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public Key Key { get; }
        public ModifierKeys Modifiers { get; }

        public static bool IsValid(Key key, ModifierKeys modifiers)
        {
            const ModifierKeys supported = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift;
            return (modifiers != ModifierKeys.None || (key >= Key.F1 && key <= Key.F24)) && (modifiers & ~supported) == 0 &&
                   key != Key.F12 && !IsModifierKey(key) &&
                   Enum.IsDefined(typeof(Key), key) && KeyInterop.VirtualKeyFromKey(key) != 0;
        }

        public static bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin;
        }

        public override string ToString()
        {
            string control = Modifiers.HasFlag(ModifierKeys.Control) ? "Ctrl+" : string.Empty;
            string alt = Modifiers.HasFlag(ModifierKeys.Alt) ? "Alt+" : string.Empty;
            string shift = Modifiers.HasFlag(ModifierKeys.Shift) ? "Shift+" : string.Empty;
            return control + alt + shift + (Key == Key.Space ? "Space" : new KeyConverter().ConvertToInvariantString(Key));
        }
    }
}
