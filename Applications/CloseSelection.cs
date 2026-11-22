using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RunningPrograms
{
    internal static class CloseSelection
    {
        internal static async Task<(ProcessEntry Entry, CloseOutcome Outcome)[]> RunAsync(IEnumerable<ProcessEntry> targets)
        {
            // Windows can close in parallel. Tabs that share one window close in sequence.
            var groups = await Task.WhenAll(targets.GroupBy(entry => (entry.Id, entry.WindowHandle))
                .Select(group => Task.Run(() => group.Select(entry => (Entry: entry, Outcome: entry.RequestClose())).ToArray())));
            return groups.SelectMany(group => group).ToArray();
        }
    }
}
