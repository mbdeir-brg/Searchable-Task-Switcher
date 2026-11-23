using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.CSharp.RuntimeBinder;

namespace RunningPrograms.Handlers.Devenv
{
    internal static class VisualStudioSolutions
    {
        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx context);

        public static Dictionary<int, string> ReadPaths(IEnumerable<int> processIds)
        {
            var pending = new HashSet<int>(processIds);
            var paths = new Dictionary<int, string>();

            try
            {
                Marshal.ThrowExceptionForHR(GetRunningObjectTable(0, out IRunningObjectTable table));
                Marshal.ThrowExceptionForHR(CreateBindCtx(0, out IBindCtx context));
                table.EnumRunning(out IEnumMoniker enumerator);

                var monikers = new IMoniker[1];
                while (pending.Count > 0 && enumerator.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    try
                    {
                        IMoniker moniker = monikers[0];
                        moniker.GetDisplayName(context, null, out string registration);
                        // Visual Studio registers each DTE instance as !VisualStudio.DTE.<version>:<pid>.
                        if (!registration.StartsWith("!VisualStudio.DTE.", StringComparison.OrdinalIgnoreCase) ||
                            !int.TryParse(registration.Substring(registration.LastIndexOf(':') + 1), out int pid) ||
                            !pending.Remove(pid))
                        {
                            continue;
                        }

                        if (table.GetObject(moniker, out object instance) != 0)
                        {
                            continue;
                        }

                        dynamic automation = instance;
                        dynamic solution = automation.Solution;
                        if (solution != null && (bool)solution.IsOpen)
                        {
                            string path = (string)solution.FullName;
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                paths.Add(pid, path);
                            }
                        }
                    }
                    catch (Exception ex) when (ex is COMException or UnauthorizedAccessException or
                                               RuntimeBinderException or MissingMemberException)
                    {
                        // Dynamic COM binding can report a busy IDE as MissingMemberException during member lookup.
                        // Leave this instance without a solution path and continue reading the other instances.
                    }
                }
            }
            catch (Exception ex) when (ex is COMException || ex is UnauthorizedAccessException)
            {
                // The process checklist remains available when the running object table cannot be read.
            }

            return paths;
        }
    }
}
