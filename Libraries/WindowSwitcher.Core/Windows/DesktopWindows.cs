using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RunningPrograms
{
    public readonly record struct DesktopWindow(int Id, IntPtr Handle, string Title);

    public static class DesktopWindows
    {
        private const uint Owner = 4;
        private const uint CloseMessage = 0x0010;
        private const int RestoreWindow = 9;

        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool WindowCallback(IntPtr window, IntPtr parameter);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(WindowCallback callback, IntPtr parameter);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowEnabled(IntPtr window);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr window, uint command);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr window);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr window, StringBuilder text, int capacity);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindowAsync(IntPtr window, int command);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr window);

        public static List<DesktopWindow> Read(HashSet<int> processIds = null)
        {
            var windows = new List<DesktopWindow>();
            EnumWindows((window, _) =>
            {
                if (GetWindowThreadProcessId(window, out int id) == 0 || (processIds != null && !processIds.Contains(id)) ||
                    !IsWindowVisible(window) || GetWindow(window, Owner) != IntPtr.Zero)
                {
                    return true;
                }

                int length = GetWindowTextLength(window);
                if (length > 0)
                {
                    var text = new StringBuilder(length + 1);
                    GetWindowText(window, text, text.Capacity);
                    string title = text.ToString().Trim();
                    if (title.Length > 0)
                    {
                        windows.Add(new DesktopWindow(id, window, title));
                    }
                }

                return true;
            }, IntPtr.Zero);
            return windows;
        }

        public static bool IsOpen(IntPtr window, int processId)
        {
            return GetWindowThreadProcessId(window, out int owner) != 0 && owner == processId && IsWindowVisible(window);
        }

        internal static bool Focus(IntPtr window)
        {
            if (IsIconic(window)) ShowWindowAsync(window, RestoreWindow);
            return SetForegroundWindow(window);
        }

        internal static bool RequestClose(IntPtr window, int processId)
        {
            return IsOpen(window, processId) && IsWindowEnabled(window) &&
                   PostMessage(window, CloseMessage, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
