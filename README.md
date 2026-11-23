# Searchable Task Switcher

A searchable Windows task switcher built with C#, WPF, and .NET 10. Find a Visual Studio window by its solution name, a Chrome window by one of its tab titles, or another application window by its title.

## Installation

Open the latest release and expand **Assets** to find the Windows download. [Download Searchable Task Switcher](https://github.com/mbdeir-brg/Searchable-Task-Switcher/releases)

- **Windows ZIP**. Download the Searchable Task Switcher ZIP, right-click it, and choose **Extract All**. Open the extracted folder and double-click `SearchableTaskSwitcher.exe`.
- **Windows installer**, if listed. Download and run the `SearchableTaskSwitcher-Setup` file, follow the prompts, then open **Searchable Task Switcher** from the Start menu.

## Use the switcher

- The application picker starts with Visual Studio (`devenv.exe`). Use **Add** to choose from running applications with multiple windows or instances.
- Select Chrome to see individual tabs grouped by window, including tabs that are in the background. If the switcher can't read a window's tabs, it shows the window title.
- Type in the search box to filter the list. Double-click an entry to activate it, or press **Enter** when the search leaves one result.
- **Close** acts on checked visible entries, or on the highlighted entry when nothing visible is checked, after confirmation.
- The switcher hides in the taskbar notification area when you switch away. Reopen it with the global shortcut or the tray icon. Use **Exit** from the tray menu to quit.
- Check **Start with Windows** to launch the switcher in the notification area when you sign in. Uncheck it to stop launching at sign-in.
- If the switcher is already running, use the global shortcut or tray icon to reopen it. Launching the executable again leaves the running instance as it is.

## Default keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| **Ctrl+Tab** | Open the switcher. Press it again while the switcher is active to cycle through configured applications. |
| **Ctrl+Space** | Cycle through configured applications while the switcher is active. |
| **Ctrl+Shift+Tab** | Cycle through the Git status filters when they are visible. |
| **Esc** | Hide the switcher in the taskbar notification area. |

Use **Shortcut** to change the global shortcut that opens the switcher. The status bar reflects your settings.

## Visual Studio and Git

Git status is based on pending changes in the local working tree. To use Git status and **Pull**, install Git for Windows and make sure `git.exe` is on `PATH`. Pull runs `git pull --ff-only` for the selected solution's repository.

A Visual Studio instance can appear as **Solution name unavailable** while it is busy, inaccessible, or has no open solution. Refresh the list after opening the solution or after Visual Studio becomes responsive.

## Azure Functions publishing

Install Azure Functions Core Tools and make `func` available on `PATH`. Sign in locally with Azure CLI or Azure PowerShell using an account that can deploy to the existing Function App. Select a Visual Studio solution and click **Publish**, then choose the Functions project and review the target app, subscription, and slot before deploying. [Microsoft Core Tools publishing guide](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local#publish-to-azure)

## Build from source

Install the .NET 10 SDK on a supported Windows version. You can build from PowerShell or open `Searchable Task Switcher.slnx` in Visual Studio 2026 with the **.NET desktop development** workload. [Microsoft .NET requirements for Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows)

```powershell
dotnet build '.\Searchable Task Switcher.slnx' -c Release
& '.\src\SearchableTaskSwitcher\bin\Release\net10.0-windows\SearchableTaskSwitcher.exe'
```

## Project layout

- `src\SearchableTaskSwitcher` contains the WPF app, settings, and Git and Azure action flows.
- `src\Libraries\WindowSwitcher.Core` contains shared process and window handling.
- `src\Libraries\WindowSwitcher.Handlers.Devenv` and `src\Libraries\WindowSwitcher.Handlers.Chrome` contain application-specific behavior.
- `scripts` contains the PowerShell publish script, and `installer` contains the Inno Setup script.
- `artifacts` contains generated publish and installer output and is ignored by Git.

## Publish and build the Windows installer

From the repository root, run the publish script in PowerShell:

```powershell
& '.\scripts\publish.ps1'
```

The script creates a self-contained Windows x64 build in `artifacts\publish`. To run that build directly, launch `SearchableTaskSwitcher.exe` from that folder and keep the published files together.

To create the installer, open `installer\installer.iss` in Inno Setup and compile it. If `ISCC.exe` is on `PATH`, you can compile it from the repository root:

```powershell
ISCC.exe '.\installer\installer.iss'
```

Inno Setup packages the files in `artifacts\publish` and reads the version from the published executable. Upload the resulting `SearchableTaskSwitcher-Setup-<version>.exe` from `artifacts\installer` as a GitHub Release asset.
