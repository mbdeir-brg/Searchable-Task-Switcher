using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RunningPrograms
{
    public class ProcessEntry
    {
        private readonly string processName;

        public ProcessEntry(string executable, int id, IntPtr windowHandle, string label, string windowGroup = null)
        {
            processName = executable[..^4];
            Id = id;
            WindowHandle = windowHandle;
            Label = label;
            WindowGroup = windowGroup;
        }

        public int Id { get; }
        public IntPtr WindowHandle { get; }
        public string WindowGroup { get; }
        public string Label { get; }
        public virtual string SearchTitle => Label;
        public virtual GitRepositoryStatus? GitStatus => null;

        public async Task<string> ActivateAsync()
        {
            try
            {
                using Process process = Process.GetProcessById(Id);
                if (!string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
                {
                    return "That process has changed. Refresh the list.";
                }

                IntPtr window = ResolveWindow(process);
                if (!DesktopWindows.IsOpen(window, Id)) return $"The window for {Label} was unavailable. Refresh the list.";

                bool focused = DesktopWindows.Focus(window);
                string error = await ActivateItemAsync();
                return error ?? (focused ? null : $"Windows could not focus {Label}.");
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is Win32Exception)
            {
                return $"Could not open {Label}. Refresh the list to check the process.";
            }
        }

        public CloseOutcome RequestClose()
        {
            try
            {
                using Process process = Process.GetProcessById(Id);
                return string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase)
                    ? CloseTarget(process) : CloseOutcome.Unavailable;
            }
            catch (ArgumentException)
            {
                return CloseOutcome.Exited;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
            {
                return CloseOutcome.Unavailable;
            }
        }

        protected virtual IntPtr ResolveWindow(Process process) => WindowHandle;
        protected virtual Task<string> ActivateItemAsync() => Task.FromResult<string>(null);

        protected virtual CloseOutcome CloseTarget(Process process)
        {
            if (!DesktopWindows.IsOpen(WindowHandle, Id)) return CloseOutcome.Exited;
            if (!DesktopWindows.RequestClose(WindowHandle, Id)) return CloseOutcome.Unavailable;
            return WaitForClose(() => SpinWait.SpinUntil(() => !DesktopWindows.IsOpen(WindowHandle, Id), 2000));
        }

        protected static CloseOutcome WaitForClose(Func<bool> wait)
        {
            try
            {
                return wait() ? CloseOutcome.Exited : CloseOutcome.Waiting;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
            {
                return CloseOutcome.Waiting;
            }
        }
    }

    public enum CloseOutcome { Exited, Waiting, Unavailable }

    public interface IRepositoryEntry
    {
        string SolutionPath { get; }
        string SolutionName { get; }
        string GetConfirmedSolutionDirectory();
    }
}
