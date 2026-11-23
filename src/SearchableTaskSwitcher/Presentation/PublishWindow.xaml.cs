using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace RunningPrograms
{
    public partial class PublishWindow : ThemedWindow
    {
        private static readonly Regex AzureName = new Regex(@"\A[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\z");

        internal PublishWindow(List<FunctionProject> projects)
        {
            InitializeComponent();
            ProjectList.ItemsSource = projects;
            if (projects.Count == 1)
            {
                ProjectList.Visibility = Visibility.Collapsed;
                SingleProject.Text = projects[0].Label;
                SingleProject.Visibility = Visibility.Visible;
                ProjectList.SelectedIndex = 0;
            }
        }

        internal FunctionProject Project => ProjectList.SelectedItem as FunctionProject;
        internal FunctionTarget Target { get; private set; }

        private void ProjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<FunctionTarget> targets = Project?.Targets;
            ProfileList.ItemsSource = targets;
            ProfilePicker.Visibility = targets?.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            if (targets?.Count == 1)
            {
                SetDestination(targets[0]);
            }
            else
            {
                SetDestination(null);
            }
        }

        private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetDestination(ProfileList.SelectedItem as FunctionTarget);
        }

        private void SetDestination(FunctionTarget target)
        {
            AppName.Text = target?.App ?? string.Empty;
            SlotName.Text = target?.Slot ?? string.Empty;
            SubscriptionId.Text = target?.Subscription ?? string.Empty;
            ValidationMessage.Visibility = Visibility.Collapsed;
        }

        private void ConfirmPublish_Click(object sender, RoutedEventArgs e)
        {
            string app = AppName.Text.Trim();
            string slot = SlotName.Text.Trim();
            string subscription = SubscriptionId.Text.Trim();
            string error = null;

            if (Project == null)
            {
                error = "Select an Azure Functions project.";
            }
            else if (!AzureName.IsMatch(app))
            {
                error = "Enter the Azure Function App name.";
            }
            else if (slot.Length > 0 && !AzureName.IsMatch(slot))
            {
                error = "Enter a deployment slot name using letters, numbers, and hyphens.";
            }
            else if (subscription.Length > 0 && !Guid.TryParseExact(subscription, "D", out _))
            {
                error = "Enter the Azure subscription ID as a GUID.";
            }

            if (error != null)
            {
                ValidationMessage.Text = error;
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            Target = new FunctionTarget(app, slot, subscription);
            DialogResult = true;
        }
    }
}
