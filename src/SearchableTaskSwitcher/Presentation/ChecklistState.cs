using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace RunningPrograms
{
    internal sealed class ChecklistState
    {
        internal ObservableCollection<ChecklistRow> Rows { get; private set; } = new();

        internal void ReplaceEntries(IEnumerable<ProcessEntry> entries)
        {
            Rows = new ObservableCollection<ChecklistRow>(entries.Select(entry => new ChecklistRow(entry)));
        }

        internal static Predicate<ChecklistRow> CreateFilter(string search, GitRepositoryStatus? status)
        {
            string query = search.Trim();
            if (query.Length == 0 && !status.HasValue) return null;

            return row => (!status.HasValue || row.Entry.GitStatus == status.Value) &&
                (query.Length == 0 || row.Entry.SearchTitle?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        internal static bool? GetCheckState(IEnumerable<ChecklistRow> visibleRows, int count)
        {
            int checkedCount = visibleRows.Count(row => row.IsChecked);
            return checkedCount == 0 ? false : checkedCount == count ? true : (bool?)null;
        }

        internal static void ToggleAll(IEnumerable<ChecklistRow> visibleRows)
        {
            bool check = visibleRows.Any(row => !row.IsChecked);
            foreach (ChecklistRow row in visibleRows) row.IsChecked = check;
        }

        internal static List<ChecklistRow> GetTargets(IEnumerable<ChecklistRow> visibleRows, ChecklistRow highlighted)
        {
            var targets = visibleRows.Where(row => row.IsChecked).ToList();
            if (targets.Count == 0 && highlighted != null && visibleRows.Contains(highlighted))
            {
                targets.Add(highlighted);
            }

            return targets;
        }
    }

    internal sealed class ChecklistRow : INotifyPropertyChanged
    {
        private bool isChecked;

        internal ChecklistRow(ProcessEntry entry) => Entry = entry;

        public ProcessEntry Entry { get; }
        public string GitStatusText => Entry.GitStatus switch
        {
            GitRepositoryStatus.Clean => "No pending changes",
            GitRepositoryStatus.Pending => "Changes pending",
            GitRepositoryStatus.NotRepository => "Not a Git repo",
            GitRepositoryStatus.Unavailable => "Git status unavailable",
            _ => null
        };
        public string AccessibleName => (GitStatusText ?? Entry.WindowGroup) is string detail
            ? $"{Entry.Label}, {detail}" : Entry.Label;
        internal string CloseDescription => Entry.WindowGroup is string group
            ? $"{Entry.Label} ({group})" : Entry.Label;

        public bool IsChecked
        {
            get => isChecked;
            set
            {
                if (isChecked == value) return;
                isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
