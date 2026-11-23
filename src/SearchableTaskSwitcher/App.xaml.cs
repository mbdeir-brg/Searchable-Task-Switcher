using System;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace RunningPrograms
{
    public partial class App : Application
    {
        public const string DisplayName = "Searchable Task Switcher";
        private const string InstanceMutexName = @"Local\SearchableTaskSwitcher";
        private Mutex instanceMutex;

        public static string VersionLabel { get; } = "v" + (typeof(App).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+', 2)[0]
            ?? typeof(App).Assembly.GetName().Version.ToString(3));
        public static string WindowTitle => $"{DisplayName} {VersionLabel}";

        internal static Uri ResourceUri(string path)
        {
            string assemblyName = typeof(App).Assembly.GetName().Name;
            return new Uri($"/{assemblyName};component/{path}", UriKind.Relative);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var mutex = new Mutex(initiallyOwned: false, InstanceMutexName, out bool createdNew);
            if (!createdNew)
            {
                mutex.Dispose();
                Shutdown();
                return;
            }

            instanceMutex = mutex;
            WindowsAppearance.RefreshFromSystem();
            base.OnStartup(e);

            var window = new MainWindow();
            MainWindow = window;
            if (Array.IndexOf(e.Args, StartupPreferences.StartupArgument) >= 0)
            {
                window.StartInTaskbar();
            }
            else
            {
                window.Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            instanceMutex?.Dispose();
        }
    }
}
