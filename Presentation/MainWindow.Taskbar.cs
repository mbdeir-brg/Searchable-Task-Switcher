using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace RunningPrograms
{
    public partial class MainWindow
    {
        private GlobalShortcut globalShortcut;
        private TaskbarNotification taskbarNotification;
        private WindowState stateBeforeMinimize = WindowState.Normal;
        private bool windowPresented;
        private bool exitRequested;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var source = (HwndSource)PresentationSource.FromVisual(this);
            globalShortcut = new GlobalShortcut(source, HandleGlobalShortcut);

            using (var stream = Application.GetResourceStream(App.ResourceUri("Assets/SearchableTaskSwitcher.ico")).Stream)
            {
                taskbarNotification = new TaskbarNotification(App.DisplayName, stream,
                    () => Dispatcher.BeginInvoke(new Action(ShowFromTaskbar)),
                    () => Dispatcher.BeginInvoke(new Action(OpenShortcutSettings)),
                    () => Dispatcher.BeginInvoke(new Action(ExitApplication)));
            }

            taskbarNotification.ApplyAppearance(WindowsAppearance.IsDark);
            UpdateShortcutHints();
        }

        internal override void ApplyAppearance()
        {
            base.ApplyAppearance();
            taskbarNotification?.ApplyAppearance(WindowsAppearance.IsDark);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            SolutionSearch.Clear();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);

            // Let an owned dialog finish opening before deciding whether focus left the switcher.
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (IsVisible && !IsActive && !OwnedWindows.OfType<Window>().Any(window => window.IsVisible))
                {
                    DockToTaskbar();
                }
            }));
        }

        private void WindowHeading_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState != WindowState.Minimized)
            {
                stateBeforeMinimize = WindowState;
            }

            UpdateWindowPresentation();
        }

        private void UpdateWindowPresentation()
        {
            bool presented = IsVisible && WindowState != WindowState.Minimized;
            if (presented == windowPresented)
            {
                return;
            }

            windowPresented = presented;
            if (presented)
            {
                ResetFilters();
                RequestRefresh();
            }
        }

        private void DockButton_Click(object sender, RoutedEventArgs e)
        {
            DockToTaskbar();
        }

        private void DockToTaskbar()
        {
            if (taskbarNotification == null)
            {
                return;
            }

            taskbarNotification.Visible = true;
            Hide();
        }

        private void HandleGlobalShortcut()
        {
            if (windowPresented && IsActive)
            {
                ShortcutKey shortcut = globalShortcut.ConfiguredShortcut;
                if (TryCycleGitFilter(shortcut.Key, shortcut.Modifiers))
                {
                    return;
                }

                if (ProgramPicker.Items.Count > 1)
                {
                    ProgramPicker.SelectedIndex = (ProgramPicker.SelectedIndex + 1) % ProgramPicker.Items.Count;
                    SolutionSearch.Focus();
                    return;
                }
            }

            ShowFromTaskbar();
        }

        private void ShowFromTaskbar()
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = stateBeforeMinimize;
            }

            if (!IsVisible)
            {
                Show();
            }

            taskbarNotification.Visible = false;
            Window dialog = OwnedWindows.OfType<Window>().FirstOrDefault(window => window.IsVisible);
            if (dialog != null)
            {
                dialog.Activate();
            }
            else if (Activate())
            {
                SolutionSearch.Focus();
            }
        }

        private void ShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            OpenShortcutSettings();
        }

        private void OpenShortcutSettings()
        {
            ShowFromTaskbar();
            var dialog = new ShortcutWindow(globalShortcut.ConfiguredShortcut, globalShortcut.Error, globalShortcut.Change)
            {
                Owner = this
            };
            if (dialog.ShowDialog() == true)
            {
                UpdateShortcutHints();
            }
        }

        private void UpdateShortcutHints()
        {
            string shortcut = globalShortcut.ConfiguredShortcut.ToString();
            ShortcutButton.ToolTip = globalShortcut.Error ?? $"Change the global shortcut. Current shortcut is {shortcut}.";
            OpenWindowShortcutText.Text = globalShortcut.Error == null
                ? $"Open window ({shortcut})"
                : $"Open window ({shortcut}, unavailable)";
            OpenWindowShortcutText.ToolTip = globalShortcut.Error;
            GitFilterShortcutText.Text = $"Cycle Git filters ({GitFilterShortcut})";
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!e.Cancel && !exitRequested && taskbarNotification != null)
            {
                e.Cancel = true;
                Dispatcher.BeginInvoke(new Action(DockToTaskbar));
            }
        }

        private void ExitApplication()
        {
            exitRequested = true;
            try
            {
                Close();
            }
            finally
            {
                exitRequested = false;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            globalShortcut?.Dispose();
            taskbarNotification?.Dispose();
            taskbarNotification = null;
            base.OnClosed(e);
        }
    }
}
