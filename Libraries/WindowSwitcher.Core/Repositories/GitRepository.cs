using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace RunningPrograms
{
    public enum GitRepositoryStatus { Unavailable, Clean, Pending, NotRepository }

    public static class GitRepository
    {
        public static GitRepositoryStatus ReadStatus(string directory)
        {
            if (!Path.IsPathRooted(directory) || !Directory.Exists(directory)) return GitRepositoryStatus.Unavailable;

            try
            {
                var startInfo = new ProcessStartInfo("git.exe", "--no-optional-locks status --porcelain=v1 --untracked-files=normal")
                {
                    WorkingDirectory = directory
                };
                startInfo.EnvironmentVariables["LC_ALL"] = "C";
                LocalCommand result = LocalCommand.Run(startInfo);
                if (result.ExitCode == 0)
                {
                    return result.Output.Length == 0 ? GitRepositoryStatus.Clean : GitRepositoryStatus.Pending;
                }

                if (result.Error.StartsWith("fatal: not a git repository", StringComparison.Ordinal))
                {
                    return GitRepositoryStatus.NotRepository;
                }
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException ||
                                       ex is IOException || ex is UnauthorizedAccessException)
            {
                // An unavailable Git status does not prevent listing the instance.
            }

            return GitRepositoryStatus.Unavailable;
        }

        public static string Pull(string directory, string solutionName)
        {
            var startInfo = new ProcessStartInfo("git.exe", "pull --ff-only") { WorkingDirectory = directory };
            startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";

            LocalCommand result = LocalCommand.Run(startInfo);
            if (result.ExitCode == 0)
            {
                return result.Output.Trim() == "Already up to date."
                    ? $"{solutionName} is already up to date."
                    : $"Pull complete for {solutionName}.";
            }

            return $"Pull failed for {solutionName}. {result.FailureReason("Git")}";
        }
    }
}
