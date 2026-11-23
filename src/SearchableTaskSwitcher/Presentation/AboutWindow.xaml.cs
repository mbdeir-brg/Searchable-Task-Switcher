using System.Reflection;

namespace RunningPrograms
{
    public partial class AboutWindow : ThemedWindow
    {
        internal AboutWindow()
        {
            InitializeComponent();

            Assembly assembly = typeof(App).Assembly;
            AppDescription.Text = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
            AppCopyright.Text = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
        }
    }
}
