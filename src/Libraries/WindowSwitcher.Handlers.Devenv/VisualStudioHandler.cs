using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RunningPrograms.Handlers.Devenv
{
    public sealed class VisualStudioHandler : ApplicationHandler
    {
        public const string ExecutableName = "devenv.exe";

        public VisualStudioHandler() : base(ExecutableName) { }

        public override bool SupportsRepositories => true;
        public override string SearchLabel => "_Search by solution name";
        public override string TitleColumn => "Solution name";
        public override string ReadingStatus => "Reading Visual Studio instances…";
        public override string ResultNoun => "instances";
        public override string CloseTitle => "Close Visual Studio";
        protected override string CloseItem => "Visual Studio instance";

        public override string DescribeResults(IEnumerable<ProcessEntry> entries, int count) =>
            $"{count} Visual Studio {(count == 1 ? "instance" : "instances")}";

        public override List<ProcessEntry> ReadEntries()
        {
            var ids = ReadProcessIds();
            if (ids.Count == 0) return new List<ProcessEntry>();

            var paths = VisualStudioSolutions.ReadPaths(ids);
            var statuses = new Dictionary<string, GitRepositoryStatus>(StringComparer.OrdinalIgnoreCase);
            var entries = new List<ProcessEntry>(ids.Count);
            foreach (int id in ids)
            {
                paths.TryGetValue(id, out string path);
                string directory = Path.GetDirectoryName(path);
                GitRepositoryStatus status = GitRepositoryStatus.Unavailable;
                if (!string.IsNullOrEmpty(directory) && !statuses.TryGetValue(directory, out status))
                {
                    status = GitRepository.ReadStatus(directory);
                    statuses.Add(directory, status);
                }

                entries.Add(new VisualStudioEntry(id, path, status));
            }

            return entries.OrderBy(entry => entry.Label, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(entry => entry.Id).ToList();
        }
    }

    internal sealed class VisualStudioEntry : ProcessEntry, IRepositoryEntry
    {
        internal VisualStudioEntry(int id, string solutionPath, GitRepositoryStatus status)
            : base(VisualStudioHandler.ExecutableName, id, IntPtr.Zero, Path.GetFileNameWithoutExtension(solutionPath) ?? "Solution name unavailable")
        {
            SolutionPath = solutionPath;
            SolutionName = Path.GetFileNameWithoutExtension(solutionPath);
            GitStatus = status;
        }

        public string SolutionPath { get; }
        public string SolutionName { get; }
        public override string SearchTitle => SolutionName;
        public override GitRepositoryStatus? GitStatus { get; }

        protected override IntPtr ResolveWindow(Process process) => process.MainWindowHandle;

        protected override CloseOutcome CloseTarget(Process process) => process.CloseMainWindow()
            ? WaitForClose(() => process.WaitForExit(2000))
            : process.HasExited ? CloseOutcome.Exited : CloseOutcome.Unavailable;

        public string GetConfirmedSolutionDirectory()
        {
            var currentPaths = VisualStudioSolutions.ReadPaths(new[] { Id });
            if (!currentPaths.TryGetValue(Id, out string currentPath) ||
                !string.Equals(SolutionPath, currentPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The open solution could not be confirmed. Refresh the list and try again.");
            }

            string directory = Path.GetDirectoryName(currentPath);
            if (string.IsNullOrEmpty(directory) || !Path.IsPathRooted(directory) || !Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException("The solution folder is unavailable.");
            }

            return directory;
        }
    }
}
