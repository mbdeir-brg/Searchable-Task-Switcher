using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;

namespace RunningPrograms
{
    internal static class WindowsAppearance
    {
        private const string Personalization = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private static ResourceDictionary darkResources;
        private static DispatcherOperation refresh;

        public static bool IsDark { get; private set; }

        public static void ScheduleRefresh()
        {
            Application app = Application.Current;
            if (app != null && !app.Dispatcher.HasShutdownStarted && refresh?.Status != DispatcherOperationStatus.Pending)
            {
                refresh = app.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(RefreshFromSystem));
            }
        }

        public static void RefreshFromSystem()
        {
            bool dark = false;
            if (!SystemParameters.HighContrast)
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(Personalization))
                    {
                        dark = key?.GetValue("AppsUseLightTheme") is int light && light == 0;
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException)
                {
                    // Windows defaults to light when the preference is unavailable.
                }
            }

            Apply(dark);
        }

        private static void Apply(bool dark)
        {
            if (IsDark == dark)
            {
                return;
            }

            var dictionaries = Application.Current.Resources.MergedDictionaries;
            if (dark)
            {
                if (darkResources == null)
                {
                    darkResources = new ResourceDictionary { Source = App.ResourceUri("Themes/Dark.xaml") };
                }

                dictionaries.Add(darkResources);
            }
            else
            {
                dictionaries.Remove(darkResources);
            }

            IsDark = dark;
            foreach (var window in Application.Current.Windows.OfType<ThemedWindow>().ToArray())
            {
                window.ApplyAppearance();
            }
        }
    }

    public class ThemedWindow : Window
    {
        private const int SettingChange = 0x001A;
        private const int SystemColorChange = 0x0015;
        private const int ThemeChange = 0x031A;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RedrawWindow(IntPtr window, IntPtr updateRectangle, IntPtr updateRegion, uint flags);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(OnWindowsMessage);
            ApplyAppearance();
        }

        private static IntPtr OnWindowsMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == SettingChange || message == ThemeChange || message == SystemColorChange)
            {
                WindowsAppearance.ScheduleRefresh();
            }

            return IntPtr.Zero;
        }

        internal virtual void ApplyAppearance()
        {
            bool useDark = WindowsAppearance.IsDark;
            SetResourceReference(BackgroundProperty, useDark ? (object)"WindowBackground" : SystemColors.WindowBrushKey);
            SetResourceReference(ForegroundProperty, useDark ? (object)"WindowForeground" : SystemColors.WindowTextBrushKey);

            IntPtr handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            int dark = useDark ? 1 : 0;
            const int ImmersiveDarkMode = 20;
            const int EarlierWindows10DarkMode = 19;
            if (DwmSetWindowAttribute(handle, ImmersiveDarkMode, ref dark, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(handle, EarlierWindows10DarkMode, ref dark, sizeof(int));
            }

            const uint Invalidate = 0x0001;
            const uint Frame = 0x0400;
            RedrawWindow(handle, IntPtr.Zero, IntPtr.Zero, Invalidate | Frame);
        }
    }
}
