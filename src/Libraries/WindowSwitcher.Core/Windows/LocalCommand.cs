using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RunningPrograms
{
    public sealed class LocalCommand
    {
        private LocalCommand(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }

        public int ExitCode { get; }
        public string Output { get; }
        public string Error { get; }

        public string FailureReason(string tool)
        {
            var errorLines = Error.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var outputLines = Output.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return errorLines.Concat(outputLines).FirstOrDefault(line =>
                       line.StartsWith("fatal:", StringComparison.OrdinalIgnoreCase) ||
                       line.StartsWith("error:", StringComparison.OrdinalIgnoreCase))
                   ?? errorLines.FirstOrDefault() ?? outputLines.LastOrDefault()
                   ?? $"{tool} exited with code {ExitCode}.";
        }

        public static LocalCommand Run(ProcessStartInfo startInfo)
        {
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;

            using (Process process = Process.Start(startInfo))
            {
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                return new LocalCommand(process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
            }
        }
    }
}
