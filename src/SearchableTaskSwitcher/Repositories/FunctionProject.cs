using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace RunningPrograms
{
    internal sealed class FunctionProject
    {
        private static readonly Regex AzureResourceId = new Regex(
            @"^/subscriptions/(?<subscription>[a-f0-9-]+)/resourceGroups/[^/]+/providers/Microsoft\.Web/sites/(?<app>[a-z0-9-]+)(?:/slots/(?<slot>[a-z0-9-]+))?/?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private FunctionProject(string directory, string label)
        {
            DirectoryPath = directory;
            Label = label;
            Targets = ReadTargets(directory);
        }

        public string DirectoryPath { get; }
        public string Label { get; }
        public List<FunctionTarget> Targets { get; }

        public static List<FunctionProject> FindInRepository(string solutionDirectory)
        {
            string root = RunGit("rev-parse --show-toplevel", solutionDirectory).TrimEnd('\r', '\n');
            string paths = RunGit("ls-files --cached --others --exclude-standard -z -- \":(glob,icase)**/host.json\"", root);
            return paths.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(path => path.Replace('/', Path.DirectorySeparatorChar))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(path => File.Exists(Path.Combine(root, path)))
                .Select(path =>
                {
                    string relativeDirectory = Path.GetDirectoryName(path);
                    string directory = Path.GetFullPath(Path.Combine(root, relativeDirectory));
                    return new FunctionProject(directory, string.IsNullOrEmpty(relativeDirectory)
                        ? Path.GetFileName(directory)
                        : relativeDirectory);
                })
                .OrderBy(project => project.Label, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static string RunGit(string arguments, string directory)
        {
            LocalCommand result = LocalCommand.Run(new ProcessStartInfo("git.exe", arguments) { WorkingDirectory = directory });
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(result.FailureReason("Git"));
            }

            return result.Output;
        }

        private static List<FunctionTarget> ReadTargets(string directory)
        {
            var targets = new List<FunctionTarget>();
            string profiles = Path.Combine(directory, "Properties", "PublishProfiles");
            if (!Directory.Exists(profiles))
            {
                return targets;
            }

            foreach (string file in Directory.EnumerateFiles(profiles, "*.pubxml"))
            {
                XDocument profile;
                try
                {
                    profile = XDocument.Load(file);
                }
                catch (Exception ex) when (ex is XmlException || ex is IOException || ex is UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (XElement element in profile.Descendants().Where(element => element.Name.LocalName == "ResourceId"))
                {
                    Match match = AzureResourceId.Match(element.Value.Trim());
                    if (match.Success && Guid.TryParseExact(match.Groups["subscription"].Value, "D", out Guid subscription))
                    {
                        targets.Add(new FunctionTarget(match.Groups["app"].Value, match.Groups["slot"].Value,
                            subscription.ToString("D")));
                    }
                }
            }

            return targets.GroupBy(target => target.Label, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(target => target.Label, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public LocalCommand Publish(FunctionTarget target)
        {
            if (!File.Exists(Path.Combine(DirectoryPath, "host.json")))
            {
                throw new InvalidOperationException("The selected Azure Functions project is no longer available.");
            }

            // The Windows installer provides func.exe, and npm provides func.cmd.
            const string script = "[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false); " +
                "$tool = Get-Command -Name func.exe,func.cmd -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1; " +
                "if (!$tool) { [Console]::Error.WriteLine('Azure Functions Core Tools (func) was not found on PATH.'); exit 1 }; " +
                "$arguments = @('azure', 'functionapp', 'publish', $env:RUNNINGPROGRAMS_FUNCTION_APP); " +
                "if ($env:RUNNINGPROGRAMS_FUNCTION_SLOT) { $arguments += @('--slot', $env:RUNNINGPROGRAMS_FUNCTION_SLOT) }; " +
                "if ($env:RUNNINGPROGRAMS_FUNCTION_SUBSCRIPTION) { $arguments += @('--subscription', $env:RUNNINGPROGRAMS_FUNCTION_SUBSCRIPTION) }; " +
                "$LASTEXITCODE = 1; & $tool.Source @arguments; exit $LASTEXITCODE";

            string powershell = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
            var startInfo = new ProcessStartInfo(powershell, "-NoProfile -NonInteractive -Command \"" + script + "\"")
            {
                WorkingDirectory = DirectoryPath
            };
            startInfo.EnvironmentVariables["RUNNINGPROGRAMS_FUNCTION_APP"] = target.App;
            startInfo.EnvironmentVariables["RUNNINGPROGRAMS_FUNCTION_SLOT"] = target.Slot;
            startInfo.EnvironmentVariables["RUNNINGPROGRAMS_FUNCTION_SUBSCRIPTION"] = target.Subscription;
            return LocalCommand.Run(startInfo);
        }
    }

    internal sealed class FunctionTarget
    {
        public FunctionTarget(string app, string slot, string subscription)
        {
            App = app;
            Slot = slot;
            Subscription = subscription;
        }

        public string App { get; }
        public string Slot { get; }
        public string Subscription { get; }
        public string Destination => string.IsNullOrEmpty(Slot) ? App : $"{App} / {Slot}";
        public string Label => $"{Destination} ({Subscription})";
    }
}
