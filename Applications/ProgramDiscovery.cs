using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace RunningPrograms
{
    internal static class ProgramDiscovery
    {
        internal static List<RunningApplication> FindRunning(HashSet<string> existingPrograms)
        {
            var windowsByPid = DesktopWindows.Read().ToLookup(window => window.Id);
            var windowsByProgram = new Dictionary<string, List<DesktopWindow>>(StringComparer.OrdinalIgnoreCase);

            foreach (Process process in Process.GetProcesses())
            {
                using (process)
                {
                    if (!windowsByPid.Contains(process.Id)) continue;

                    string executable;
                    try
                    {
                        executable = process.ProcessName + ".exe";
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
                    {
                        continue;
                    }

                    if (existingPrograms.Contains(executable)) continue;
                    if (!windowsByProgram.TryGetValue(executable, out var windows))
                    {
                        windows = new List<DesktopWindow>();
                        windowsByProgram.Add(executable, windows);
                    }
                    windows.AddRange(windowsByPid[process.Id]);
                }
            }

            return windowsByProgram.Select(group => ApplicationHandlers.Resolve(group.Key).GetAddChoice(group.Value))
                .Where(app => app != null)
                .OrderBy(app => app.Executable, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

    }
}
