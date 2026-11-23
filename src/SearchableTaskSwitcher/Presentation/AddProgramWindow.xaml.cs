using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RunningPrograms
{
    public partial class AddProgramWindow : ThemedWindow
    {
        private readonly HashSet<string> existingPrograms;

        internal AddProgramWindow(IEnumerable<string> programs)
        {
            existingPrograms = new HashSet<string>(programs, StringComparer.OrdinalIgnoreCase);
            InitializeComponent();
            Loaded += FindApplications;
        }

        internal string Program { get; private set; }

        private async void FindApplications(object sender, RoutedEventArgs e)
        {
            try
            {
                List<RunningApplication> applications = await Task.Run(() => ProgramDiscovery.FindRunning(existingPrograms));
                if (!IsVisible) return;

                if (applications.Count == 0)
                {
                    DiscoveryStatus.Text = "No additional applications have multiple windows or Chrome tabs.";
                    return;
                }

                ApplicationList.ItemsSource = applications;
                DiscoveryStatus.Visibility = Visibility.Collapsed;
                ApplicationList.Visibility = Visibility.Visible;
                ApplicationList.Focus();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
            {
                DiscoveryStatus.Text = "Could not read the list of running applications.";
            }
        }

        private void ApplicationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddButton.IsEnabled = ApplicationList.SelectedItem is RunningApplication;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationList.SelectedItem is not RunningApplication application) return;
            Program = application.Executable;
            DialogResult = true;
        }
    }
}
