
#define MyAppName "Searchable Task Switcher"
#define MyAppPublisher "Mahmoud Bdeir"
#define MyAppExeName "SearchableTaskSwitcher.exe"

; Resolve app assets and build output from the repository root.
#define MyRepositoryRoot AddBackslash(SourcePath) + ".."
#define MyAppSourceDir MyRepositoryRoot + "\src\SearchableTaskSwitcher"
#define MyPublishDir MyRepositoryRoot + "\artifacts\publish"
#define MyOutputDir MyRepositoryRoot + "\artifacts\installer"
#define MyAppExePath MyPublishDir + "\" + MyAppExeName

; Run from the repository root before compiling:
; dotnet publish ".\src\SearchableTaskSwitcher\Searchable Task Switcher.csproj" -c Release -r win-x64 --self-contained true -o ".\artifacts\publish"
#if !FileExists(MyAppExePath)
  #error Application executable not found. Publish the project in src\SearchableTaskSwitcher to artifacts\publish before compiling this installer.
#endif

; Read version directly from the published executable
#define MyAppVersion GetVersionNumbersString(MyAppExePath)

[Setup]
; Application information
AppId={{B7E5D290-3A42-4F9B-AF2C-46B7E53D0128}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=Copyright (C) 2026 Mahmoud Bdeir

; Installation directory
DefaultDirName={localappdata}\Programs\SearchableTaskSwitcher
DefaultGroupName={#MyAppName}
UsePreviousAppDir=no

; Installer output
OutputDir={#MyOutputDir}
OutputBaseFilename=SearchableTaskSwitcher-Setup-{#MyAppVersion}
SetupIconFile={#MyAppSourceDir}\Assets\SearchableTaskSwitcher.ico

; Installation behavior
PrivilegesRequired=lowest
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes

; Windows application information
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

; Supported architecture
ArchitecturesAllowed=x64compatible

; Handle running applications during upgrades
CloseApplications=yes
RestartApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
; Include all published application files
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start menu shortcut
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Launch application after installation
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
