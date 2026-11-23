using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace RunningPrograms
{
    public partial class MainWindow : ThemedWindow
    {
        private static readonly ShortcutKey ApplicationCycleShortcut = new ShortcutKey(Key.Space, ModifierKeys.Control);
        private static readonly ShortcutKey GitFilterShortcut = new ShortcutKey(Key.Tab, ModifierKeys.Control | ModifierKeys.Shift);

        private readonly ObservableCollection<string> programs = new ObservableCollection<string>(ProgramChoices.Load());
        private readonly ChecklistState checklist = new ChecklistState();
        private ApplicationHandler handler;
        private bool updatingProcessList;
        private bool refreshPending;
        private bool synchronizingChecks;
        private ChecklistRow lastClickedEntry;
        private ChecklistRow pendingActivation;

        public MainWindow()
        {
            InitializeComponent();
            StartWithWindows.IsChecked = StartupPreferences.IsEnabled();
            SolutionSearch.TextChanged += FiltersChanged;
            GitBoth.Checked += FiltersChanged;
            GitUpToDate.Checked += FiltersChanged;
            GitWorkInProgress.Checked += FiltersChanged;
            GitNotARepo.Checked += FiltersChanged;
            CheckAll.Checked += CheckAllChanged;
            CheckAll.Unchecked += CheckAllChanged;
            IsVisibleChanged += (sender, args) => UpdateWindowPresentation();
            ProgramPicker.ItemsSource = programs;
            ProgramPicker.SelectionChanged += ProgramPicker_SelectionChanged;
            ProgramPicker.SelectedIndex = 0;
        }

        private string SelectedProgram => ProgramPicker.SelectedItem as string ?? ProgramChoices.VisualStudio;
        private IEnumerable<ChecklistRow> VisibleRows => ProcessList.Items.Cast<ChecklistRow>();

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AboutWindow { Owner = this };
            dialog.ShowDialog();
        }

        private void AddProgram_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddProgramWindow(programs) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            string program = programs.FirstOrDefault(item => string.Equals(item, dialog.Program, StringComparison.OrdinalIgnoreCase));
            if (program == null)
            {
                program = dialog.Program;
                programs.Add(program);
                ProgramStatus.Visibility = ProgramChoices.Save(programs) ? Visibility.Collapsed : Visibility.Visible;
                ProgramStatus.Text = "The application was added for this session. Windows could not save it.";
            }

            ProgramPicker.SelectedItem = program;
        }

        private void ProgramPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            handler = ApplicationHandlers.Resolve(SelectedProgram);
            GitFilterPanel.Visibility = handler.SupportsRepositories ? Visibility.Visible : Visibility.Collapsed;
            PullButton.Visibility = PublishButton.Visibility = GitFilterPanel.Visibility;
            PullStatus.Visibility = PublishStatus.Visibility = CloseStatus.Visibility = Visibility.Collapsed;
            SearchLabel.Content = handler.SearchLabel;
            CheckAll.Content = handler.TitleColumn;
            ProcessList.ItemsSource = null;
            checklist.ReplaceEntries(Array.Empty<ProcessEntry>());
            ResetFilters();
            ApplySolutionFilter();
            if (IsVisible) RequestRefresh();
        }

        private void ResetFilters()
        {
            SolutionSearch.Clear();
            GitBoth.IsChecked = true;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RequestRefresh();
        }

        private async void RequestRefresh()
        {
            if (updatingProcessList)
            {
                refreshPending = true;
                return;
            }

            refreshPending = false;
            SetProcessListBusy(true);
            ApplicationHandler requestedHandler = handler;
            ProcessStatus.Text = requestedHandler.ReadingStatus;
            try
            {
                List<ProcessEntry> entries = await Task.Run(requestedHandler.ReadEntries);
                if (!ReferenceEquals(handler, requestedHandler)) return;
                checklist.ReplaceEntries(entries);
                foreach (ChecklistRow row in checklist.Rows)
                {
                    PropertyChangedEventManager.AddHandler(row, EntryCheckChanged, nameof(ChecklistRow.IsChecked));
                }
                ProcessList.ItemsSource = checklist.Rows;
                ProcessList.Items.GroupDescriptions.Clear();
                if (requestedHandler.GroupsByWindow)
                {
                    ProcessList.Items.GroupDescriptions.Add(new PropertyGroupDescription($"{nameof(ChecklistRow.Entry)}.{nameof(ProcessEntry.WindowGroup)}"));
                }
                ApplySolutionFilter();
                ShowProcessCount();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
            {
                if (ReferenceEquals(handler, requestedHandler))
                {
                    ProcessStatus.Text = $"Could not read {requestedHandler.Executable}. Try refreshing again.";
                }
            }
            finally
            {
                FinishProcessListUpdate();
            }
        }

        private void SetProcessListBusy(bool busy)
        {
            updatingProcessList = busy;
            RefreshButton.IsEnabled = !busy;
            CloseButton.IsEnabled = !busy;
        }

        private void FinishProcessListUpdate()
        {
            SetProcessListBusy(false);
            if (refreshPending && !exitRequested && !Dispatcher.HasShutdownStarted)
            {
                RequestRefresh();
            }
        }

        private void CheckAllChanged(object sender, RoutedEventArgs e)
        {
            if (synchronizingChecks || ProcessList.Items.IsEmpty)
            {
                return;
            }

            synchronizingChecks = true;
            try
            {
                ChecklistState.ToggleAll(VisibleRows);
            }
            finally
            {
                synchronizingChecks = false;
            }

            UpdateCheckAll();
        }

        private void EntryCheckChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateCheckAll();
        }

        private void UpdateCheckAll()
        {
            if (synchronizingChecks)
            {
                return;
            }

            int total = ProcessList.Items.Count;
            synchronizingChecks = true;
            try
            {
                CheckAll.IsEnabled = total > 0;
                CheckAll.IsChecked = ChecklistState.GetCheckState(VisibleRows, total);
            }
            finally
            {
                synchronizingChecks = false;
            }
        }

        private void FiltersChanged(object sender, RoutedEventArgs e)
        {
            ApplySolutionFilter();
            if (RefreshButton.IsEnabled)
            {
                ShowProcessCount();
            }
        }

        private GitRepositoryStatus? SelectedGitStatus
        {
            get
            {
                if (GitUpToDate.IsChecked == true)
                {
                    return GitRepositoryStatus.Clean;
                }
                if (GitWorkInProgress.IsChecked == true)
                {
                    return GitRepositoryStatus.Pending;
                }

                return GitNotARepo.IsChecked == true ? GitRepositoryStatus.NotRepository : (GitRepositoryStatus?)null;
            }
        }

        private void ApplySolutionFilter()
        {
            Predicate<ChecklistRow> filter = ChecklistState.CreateFilter(SolutionSearch.Text, SelectedGitStatus);
            ProcessList.Items.Filter = filter == null ? null : item => item is ChecklistRow row && filter(row);
            UpdateCheckAll();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ModifierKeys modifiers = e.KeyboardDevice.Modifiers;
            if (e.Key == Key.Escape && modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                DockToTaskbar();
            }
            else if (e.Key == ApplicationCycleShortcut.Key && modifiers == ApplicationCycleShortcut.Modifiers)
            {
                e.Handled = true;
                if (!e.IsRepeat)
                {
                    TryCycleApplications();
                }
            }
            else if (TryCycleGitFilter(e.Key, modifiers, e.IsRepeat))
            {
                e.Handled = true;
            }
        }

        private bool TryCycleApplications()
        {
            if (ProgramPicker.Items.Count < 2)
            {
                return false;
            }

            ProgramPicker.SelectedIndex = (ProgramPicker.SelectedIndex + 1) % ProgramPicker.Items.Count;
            SolutionSearch.Focus();
            return true;
        }

        private bool TryCycleGitFilter(Key key, ModifierKeys modifiers, bool isRepeat = false)
        {
            if (key != GitFilterShortcut.Key || modifiers != GitFilterShortcut.Modifiers || !GitFilterPanel.IsVisible)
            {
                return false;
            }

            if (!isRepeat)
            {
                RadioButton nextFilter = SelectedGitStatus switch
                {
                    GitRepositoryStatus.Clean => GitWorkInProgress,
                    GitRepositoryStatus.Pending => GitNotARepo,
                    GitRepositoryStatus.NotRepository => GitBoth,
                    _ => GitUpToDate
                };
                nextFilter.IsChecked = true;
            }

            return true;
        }

        private void ActivateOnlyResultOnEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.IsRepeat && ProcessList.Items.Count == 1 &&
                ProcessList.Items[0] is ChecklistRow entry)
            {
                e.Handled = true;
                FocusProcess(entry);
            }
        }

        private void ProcessList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChecklistRow entry = GetClickedEntry(e);
            if (entry != null)
            {
                ProcessList.SelectedItem = entry;
            }
            pendingActivation = e.ClickCount == 2 && entry == lastClickedEntry ? entry : null;
            lastClickedEntry = entry;
        }

        private void ProcessList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ChecklistRow entry = pendingActivation;
            pendingActivation = null;
            if (entry != null && entry == GetClickedEntry(e))
            {
                // Let the checkbox finish processing the second click before moving focus.
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => FocusProcess(entry)));
            }
        }

        private ChecklistRow GetClickedEntry(MouseButtonEventArgs e)
        {
            var row = ItemsControl.ContainerFromElement(ProcessList, e.OriginalSource as DependencyObject) as ListBoxItem;
            return row?.DataContext as ChecklistRow;
        }

        private async void FocusProcess(ChecklistRow row)
        {
            string error = await row.Entry.ActivateAsync();
            if (!ProcessList.Items.Contains(row)) return;

            if (error == null)
            {
                ShowProcessCount();
            }
            else
            {
                ProcessStatus.Text = error;
            }
        }

        private void ShowProcessCount()
        {
            int count = ProcessList.Items.Count;
            if (!string.IsNullOrWhiteSpace(SolutionSearch.Text) || SelectedGitStatus.HasValue)
            {
                string noun = handler.ResultNoun;
                ProcessStatus.Text = count == 0 ? $"No matching {noun}." : $"Showing {count} of {checklist.Rows.Count} {noun}";
                return;
            }

            ProcessStatus.Text = handler.DescribeResults(VisibleRows.Select(row => row.Entry), count);
        }
    }
}
