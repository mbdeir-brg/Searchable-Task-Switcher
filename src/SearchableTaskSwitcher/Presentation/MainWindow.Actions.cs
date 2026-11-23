using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RunningPrograms
{
    public partial class MainWindow
    {
        private async void PullButton_Click(object sender, RoutedEventArgs e)
        {
            IRepositoryEntry entry = GetRepositoryEntry();
            PullStatus.Visibility = Visibility.Visible;
            if (entry == null || string.IsNullOrEmpty(entry.SolutionPath))
            {
                PullStatus.Text = "Select one visible instance with an open solution to pull.";
                return;
            }

            EnableRepositoryActions(false);
            PullStatus.Text = $"Pulling {entry.SolutionName}…";
            try
            {
                PullStatus.Text = await Task.Run(() => GitRepository.Pull(entry.GetConfirmedSolutionDirectory(), entry.SolutionName));
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
            {
                PullStatus.Text = "Pull failed. Git was not found on PATH.";
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException ||
                                       ex is IOException || ex is UnauthorizedAccessException)
            {
                PullStatus.Text = $"Pull failed for {entry.SolutionName}. {ex.Message}";
            }
            finally
            {
                EnableRepositoryActions(true);
            }
        }

        private async void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            IRepositoryEntry entry = GetRepositoryEntry();
            PublishStatus.Visibility = Visibility.Visible;
            if (entry == null || string.IsNullOrEmpty(entry.SolutionPath))
            {
                PublishStatus.Text = "Select one visible instance with an open solution to publish.";
                return;
            }

            EnableRepositoryActions(false);
            PublishStatus.Text = $"Finding Azure Functions projects in {entry.SolutionName}…";
            try
            {
                List<FunctionProject> projects = await Task.Run(() =>
                    FunctionProject.FindInRepository(entry.GetConfirmedSolutionDirectory()));
                if (projects.Count == 0)
                {
                    PublishStatus.Text = "No Azure Functions projects found in this Git repository.";
                    return;
                }

                var dialog = new PublishWindow(projects) { Owner = this };
                if (dialog.ShowDialog() != true)
                {
                    PublishStatus.Text = "Publish canceled.";
                    return;
                }

                FunctionProject project = dialog.Project;
                FunctionTarget target = dialog.Target;
                PublishStatus.Text = $"Publishing {project.Label} to {target.Destination}…";
                LocalCommand result = await Task.Run(() => project.Publish(target));
                PublishStatus.Text = result.ExitCode == 0
                    ? $"Published {project.Label} to {target.Destination}."
                    : $"Publish failed. {result.FailureReason("Azure Functions Core Tools")}";
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
            {
                PublishStatus.Text = "Publish failed. A required command could not be started. Check that Git is on PATH.";
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException ||
                                       ex is IOException || ex is UnauthorizedAccessException)
            {
                PublishStatus.Text = $"Publish failed. {ex.Message}";
            }
            finally
            {
                EnableRepositoryActions(true);
            }
        }

        private IRepositoryEntry GetRepositoryEntry()
        {
            if (!handler.SupportsRepositories) return null;
            List<ChecklistRow> targets = GetVisibleActionTargets();
            return targets.Count == 1 ? targets[0].Entry as IRepositoryEntry : null;
        }

        private List<ChecklistRow> GetVisibleActionTargets() =>
            ChecklistState.GetTargets(VisibleRows, ProcessList.SelectedItem as ChecklistRow);

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var rows = checklist.Rows;
            List<ChecklistRow> targets = GetVisibleActionTargets();

            CloseStatus.Visibility = Visibility.Visible;
            if (targets.Count == 0)
            {
                CloseStatus.Text = "Check or highlight the visible rows you want to close.";
                return;
            }

            const int previewLimit = 8;
            string preview = string.Join(Environment.NewLine, targets.Take(previewLimit).Select(entry => entry.CloseDescription));
            if (targets.Count > previewLimit)
            {
                preview += $"{Environment.NewLine}and {targets.Count - previewLimit} more";
            }

            var entries = targets.Select(row => row.Entry).ToArray();
            var confirmation = new CloseConfirmationWindow(handler.CloseQuestion(entries), preview)
            {
                Owner = this,
                Title = handler.CloseTitle
            };
            SetProcessListBusy(true);
            try
            {
                if (confirmation.ShowDialog() != true)
                {
                    CloseStatus.Text = "Close canceled.";
                    return;
                }

                CloseStatus.Text = "Waiting for the selection to close…";
                var results = await CloseSelection.RunAsync(entries);
                var closed = results.Where(result => result.Outcome == CloseOutcome.Exited).Select(result => result.Entry).ToHashSet();
                foreach (var row in targets.Where(row => closed.Contains(row.Entry)))
                {
                    rows.Remove(row);
                }

                if (ReferenceEquals(checklist.Rows, rows))
                {
                    UpdateCheckAll();
                    ShowProcessCount();
                }
                int exited = results.Count(result => result.Outcome == CloseOutcome.Exited);
                int waiting = results.Count(result => result.Outcome == CloseOutcome.Waiting);
                int unavailable = results.Length - exited - waiting;
                var messages = new List<string>();
                if (exited > 0)
                {
                    messages.Add($"{exited} closed.");
                }
                if (waiting > 0)
                {
                    messages.Add($"{waiting} still open. Check the application for prompts.");
                }
                if (unavailable > 0)
                {
                    messages.Add($"{unavailable} could not be closed. Check for open dialogs or unavailable controls.");
                }
                CloseStatus.Text = string.Join(" ", messages);
            }
            finally
            {
                FinishProcessListUpdate();
            }
        }

        private void EnableRepositoryActions(bool enabled)
        {
            PullButton.IsEnabled = enabled;
            PublishButton.IsEnabled = enabled;
        }

    }
}
