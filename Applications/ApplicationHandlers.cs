using System;
using System.Collections.Generic;
using System.Linq;
using RunningPrograms.Handlers.Chrome;
using RunningPrograms.Handlers.Devenv;

namespace RunningPrograms
{
    internal static class ApplicationHandlers
    {
        private static readonly Dictionary<string, ApplicationHandler> specialized = new ApplicationHandler[]
        {
            new VisualStudioHandler(),
            new ChromeHandler()
        }.ToDictionary(handler => handler.Executable, StringComparer.OrdinalIgnoreCase);

        internal static ApplicationHandler Resolve(string executable) => specialized.TryGetValue(executable, out var handler)
            ? handler : new DesktopWindowHandler(executable);
    }
}
