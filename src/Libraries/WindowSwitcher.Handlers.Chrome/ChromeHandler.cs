using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RunningPrograms.Handlers.Chrome
{
    public sealed class ChromeHandler : DesktopWindowHandler
    {
        public const string ExecutableName = "chrome.exe";

        public ChromeHandler() : base(ExecutableName) { }

        public override bool GroupsByWindow => true;
        public override string SearchLabel => "_Search Chrome tabs";
        public override string TitleColumn => "Tab or window title";
        public override string ReadingStatus => "Reading Chrome tabs…";
        public override string ResultNoun => "Chrome results";
        public override string CloseTitle => "Close Chrome selection";

        protected override List<ProcessEntry> ReadWindows(IReadOnlyList<DesktopWindow> windows)
        {
            var entries = new List<ProcessEntry>();
            int windowIndex = 0;
            const string suffix = " - Google Chrome";
            foreach (var window in windows.OrderBy(item => item.Handle.ToInt64()))
            {
                string group = $"Chrome window {++windowIndex}";
                IReadOnlyList<ChromeTab> tabs = ChromeTab.Read(window.Handle);
                if (tabs.Count == 0)
                {
                    string title = window.Title.Length > suffix.Length && window.Title.EndsWith(suffix, StringComparison.Ordinal)
                        ? window.Title[..^suffix.Length] : window.Title;
                    entries.Add(new ProcessEntry(Executable, window.Id, window.Handle, title, group));
                    continue;
                }

                entries.AddRange(tabs.OrderBy(tab => tab.Title, StringComparer.CurrentCultureIgnoreCase)
                    .Select(tab => new ChromeTabEntry(window, group, tab)));
            }

            return entries;
        }

        public override RunningApplication GetAddChoice(IReadOnlyList<DesktopWindow> windows)
        {
            if (windows.Count != 1) return base.GetAddChoice(windows);

            int tabs = ChromeTab.Read(windows[0].Handle).Count;
            return tabs >= 2 ? new RunningApplication(Executable, $"{tabs} tabs") : null;
        }

        public override string DescribeResults(IEnumerable<ProcessEntry> entries, int count)
        {
            int tabs = entries.Count(entry => entry is ChromeTabEntry);
            int windows = count - tabs;
            string tabLabel = tabs == 1 ? "tab" : "tabs";
            string windowLabel = windows == 1 ? "window" : "windows";
            return tabs == 0 ? $"{windows} Chrome {windowLabel}"
                : windows == 0 ? $"{tabs} Chrome {tabLabel}" : $"{tabs} {tabLabel}, {windows} {windowLabel} by title";
        }

        public override string CloseQuestion(IReadOnlyList<ProcessEntry> entries)
        {
            int tabs = entries.Count(entry => entry is ChromeTabEntry);
            if (tabs == 0) return base.CloseQuestion(entries);
            if (tabs < entries.Count) return $"Close {entries.Count} Chrome tabs and windows?";
            return tabs == 1 ? "Close this Chrome tab?" : $"Close {tabs} Chrome tabs?";
        }
    }

    internal sealed class ChromeTabEntry : ProcessEntry
    {
        private readonly ChromeTab tab;

        internal ChromeTabEntry(DesktopWindow window, string group, ChromeTab tab)
            : base(ChromeHandler.ExecutableName, window.Id, window.Handle, tab.Title, group)
        {
            this.tab = tab;
        }

        protected override async Task<string> ActivateItemAsync() => await Task.Run(tab.Select)
            ? null : $"Chrome could not select {Label}. Refresh the list.";

        protected override CloseOutcome CloseTarget(Process process)
        {
            bool Exists() => DesktopWindows.IsOpen(WindowHandle, Id) && tab.IsAvailable();
            if (!Exists()) return CloseOutcome.Exited;
            if (!tab.RequestClose()) return CloseOutcome.Unavailable;
            return WaitForClose(() => SpinWait.SpinUntil(() => !Exists(), 800) || tab.WasRemovedFrom(WindowHandle));
        }
    }
}
