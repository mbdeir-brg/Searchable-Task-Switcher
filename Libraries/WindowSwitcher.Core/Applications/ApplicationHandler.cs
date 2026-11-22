using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RunningPrograms
{
    public abstract class ApplicationHandler
    {
        protected ApplicationHandler(string executable)
        {
            Executable = executable;
        }

        public string Executable { get; }
        public virtual bool SupportsRepositories => false;
        public virtual bool GroupsByWindow => false;
        public virtual string SearchLabel => "_Search by window title";
        public virtual string TitleColumn => "Window title";
        public virtual string ReadingStatus => $"Reading {Executable} windows…";
        public virtual string ResultNoun => "windows";
        public virtual string CloseTitle => "Close windows";
        protected virtual string CloseItem => "window";

        public abstract List<ProcessEntry> ReadEntries();

        public virtual string DescribeResults(IEnumerable<ProcessEntry> entries, int count) =>
            $"{count} {Executable} {(count == 1 ? "window" : "windows")}";

        public virtual string CloseQuestion(IReadOnlyList<ProcessEntry> entries) =>
            entries.Count == 1 ? $"Close this {CloseItem}?" : $"Close {entries.Count} {CloseItem}s?";

        public virtual RunningApplication GetAddChoice(IReadOnlyList<DesktopWindow> windows) =>
            windows.Count >= 2 ? new RunningApplication(Executable, $"{windows.Count} windows") : null;

        protected HashSet<int> ReadProcessIds()
        {
            var ids = new HashSet<int>();
            foreach (Process process in Process.GetProcessesByName(Executable[..^4]))
            {
                using (process) ids.Add(process.Id);
            }

            return ids;
        }
    }

    public class DesktopWindowHandler : ApplicationHandler
    {
        public DesktopWindowHandler(string executable) : base(executable) { }

        public override List<ProcessEntry> ReadEntries()
        {
            var ids = ReadProcessIds();
            if (ids.Count == 0) return new List<ProcessEntry>();

            return ReadWindows(DesktopWindows.Read(ids));
        }

        protected virtual List<ProcessEntry> ReadWindows(IReadOnlyList<DesktopWindow> windows) => windows
            .Select(window => new ProcessEntry(Executable, window.Id, window.Handle, window.Title))
            .OrderBy(entry => entry.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Id)
            .ToList();
    }
}
