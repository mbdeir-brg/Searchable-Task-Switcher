using System;
using System.Windows;

namespace RunningPrograms
{
    public partial class App : Application
    {
        public const string DisplayName = "Searchable Task Switcher";

        internal static Uri ResourceUri(string path)
        {
            string assemblyName = typeof(App).Assembly.GetName().Name;
            return new Uri($"/{assemblyName};component/{path}", UriKind.Relative);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            WindowsAppearance.RefreshFromSystem();
            base.OnStartup(e);
        }
    }
}
