using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace RunningPrograms.Handlers.Chrome
{
    internal sealed class ChromeTab
    {
        private static readonly PropertyCondition TabItem = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem);
        private static readonly PropertyCondition Button = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
        private readonly AutomationElement element;
        private readonly int[] runtimeId;

        private ChromeTab(AutomationElement element)
        {
            this.element = element;
            runtimeId = element.GetRuntimeId();
            string name = element.Current.Name;
            Title = string.IsNullOrWhiteSpace(name) ? "Untitled tab" : name;
        }

        internal string Title { get; }

        internal static IReadOnlyList<ChromeTab> Read(IntPtr window)
        {
            try
            {
                var root = AutomationElement.FromHandle(window);
                var walker = TreeWalker.ControlViewWalker;
                var chromeControls = new Queue<AutomationElement>();
                chromeControls.Enqueue(root);

                while (chromeControls.TryDequeue(out AutomationElement control))
                {
                    ControlType type = control.Current.ControlType;
                    // Web pages can contain tab controls of their own. Only inspect the browser UI.
                    if (type == ControlType.Document) continue;
                    if (type == ControlType.Tab)
                    {
                        AutomationElementCollection tabs = control.FindAll(TreeScope.Descendants, TabItem);
                        if (tabs.Count > 0) return tabs.Cast<AutomationElement>().Select(tab => new ChromeTab(tab)).ToList();
                    }

                    for (var child = walker.GetFirstChild(control); child != null; child = walker.GetNextSibling(child))
                    {
                        chromeControls.Enqueue(child);
                    }
                }
            }
            catch (Exception ex) when (IsAutomationFailure(ex))
            {
                // A closing or inaccessible window can still be listed by its native title.
            }

            return Array.Empty<ChromeTab>();
        }

        internal bool Select()
        {
            try
            {
                if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object selection))
                {
                    ((SelectionItemPattern)selection).Select();
                    return true;
                }
                if (element.TryGetCurrentPattern(InvokePattern.Pattern, out object invoke))
                {
                    ((InvokePattern)invoke).Invoke();
                    return true;
                }
            }
            catch (Exception ex) when (IsAutomationFailure(ex)) { }

            return false;
        }

        internal bool RequestClose()
        {
            try
            {
                foreach (AutomationElement button in element.FindAll(TreeScope.Children, Button))
                {
                    var info = button.Current;
                    bool closeButton = info.ClassName == "TabCloseButton" || info.Name is "Close" or "Close tab";
                    if (!closeButton || !button.TryGetCurrentPattern(InvokePattern.Pattern, out object invoke)) continue;
                    ((InvokePattern)invoke).Invoke();
                    return true;
                }
            }
            catch (Exception ex) when (IsAutomationFailure(ex)) { }

            return false;
        }

        internal bool IsAvailable()
        {
            try
            {
                return element.Current.ProcessId != 0;
            }
            catch (Exception ex) when (ex is ElementNotAvailableException || ex is COMException)
            {
                return false;
            }
        }

        internal bool WasRemovedFrom(IntPtr window)
        {
            IReadOnlyList<ChromeTab> tabs = Read(window);
            return tabs.Count > 0 && tabs.All(tab => !runtimeId.SequenceEqual(tab.runtimeId));
        }

        private static bool IsAutomationFailure(Exception error) => error is ElementNotAvailableException or
            ElementNotEnabledException or InvalidOperationException or COMException or UnauthorizedAccessException or ArgumentException;
    }
}
