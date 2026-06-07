# AI Terminal Launcher

AI Terminal Launcher is a Windows tray application for launching AI CLI tools from Explorer context, tray menu entries, or global hotkeys.

It is designed for tools such as Codex, Claude, OpenCode, and other command line assistants. The app detects the active Explorer folder, opens the configured terminal there, and runs the selected CLI command.

## Features

- Windows tray app with a settings window.
- Configurable CLI tools: display name, command, arguments, visibility, and hotkey.
- Global hotkeys, including single-key hotkeys and modifier combinations.
- Direct key capture for custom hotkey setup instead of a dropdown-only picker.
- Explorer folder detection:
  - selected folder wins;
  - selected file falls back to the current folder;
  - missing Explorer context falls back to folder picker.
- Short reuse window for consecutive hotkey launches, so a newly opened terminal does not break the next hotkey immediately.
- Optional tray menu entries.
- Optional Explorer context menu integration.
- Optional launch at login.
- Preferred terminal selection with PowerShell fallback.
- Self-contained single-file publish for Windows.

## Repository Layout

```text
.
├── src/
│   ├── AITerminalLauncher.App/      # WinForms tray application
│   ├── AITerminalLauncher.Core/     # Configuration, validation, launch, hotkey, tray logic
│   └── AITerminalLauncher.psm1      # PowerShell backend module
├── tests/
│   ├── AITerminalLauncher.Core.Tests/
│   └── run-tests.ps1
├── install.ps1                      # Install Explorer context menu
├── uninstall.ps1                    # Remove Explorer context menu
├── launcher.ps1                     # Tool launch script
├── publish.ps1                      # Self-contained publish script
└── config.json                      # Default config template
```

## Requirements

- Windows
- Windows PowerShell 5.1 or newer
- .NET 8 SDK for building from source
- Target CLI commands available in `PATH`, or configured with full executable paths
- Windows Terminal if `wt` is selected as preferred terminal

The published self-contained executable does not require installing the .NET runtime on the target machine.

## Build

```powershell
dotnet build .\AITerminalLauncher.sln
```

## Test

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```

The test script covers:

- PowerShell backend behavior
- config loading and validation
- launch command generation
- hotkey conflict detection
- Explorer target resolution
- context menu dry-run behavior
- UI source regression checks

## Publish

Publish a self-contained Windows x64 package:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\publish.ps1
```

Output:

```text
dist\AITerminalLauncher\
```

The published folder contains:

- `AITerminalLauncher.App.exe`
- `launcher.ps1`
- `install.ps1`
- `uninstall.ps1`
- `src\AITerminalLauncher.psm1`

Keep this folder layout intact when distributing the app.

To publish for another runtime:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\publish.ps1 -Runtime win-arm64
```

## Run

Open the settings window:

```powershell
.\dist\AITerminalLauncher\AITerminalLauncher.App.exe
```

Start silently in tray mode:

```powershell
.\dist\AITerminalLauncher\AITerminalLauncher.App.exe --tray
```

During development:

```powershell
dotnet run --project .\src\AITerminalLauncher.App\AITerminalLauncher.App.csproj
```

## Configuration

User config path:

```text
%LocalAppData%\AITerminalLauncher\config.json
```

The repository-level `config.json` is the default template.

## Hotkeys

Each enabled tool can have one global hotkey.

Supported keys include:

- `A-Z`
- `0-9`
- `F1-F24`
- `NUMPAD0-NUMPAD9`
- arrow keys
- `Home`, `End`
- `PageUp`, `PageDown`
- `Insert`, `Delete`
- `Space`, `Tab`, `Esc`, `Enter`, `Backspace`
- common symbol keys

When editing a tool, click the key input field and press the desired key or combination. For example, pressing `Ctrl+C` captures `C` and checks the `Ctrl` modifier.

## Explorer Context Menu

Install context menu entries:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json"
```

Preview registry changes:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json" -DryRun
```

Remove context menu entries:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\uninstall.ps1 -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json"
```

The tray menu also exposes install/remove context menu commands.

## Logs

Logs are written to:

```text
%LocalAppData%\AITerminalLauncher\logs\
```

Check this folder when startup, hotkey registration, or tool launch fails.

## Troubleshooting

### Hotkey registration fails

Another program may already own the key combination. Change the hotkey in settings and save again.

### Wrong folder is launched

The target folder priority is:

1. selected folder in the active Explorer window;
2. current folder in the active Explorer window;
3. briefly reused previous launch folder for consecutive hotkey launches;
4. folder picker fallback.

If Explorer is not the foreground window and the recent reuse window has expired, the app will not keep using an old folder forever.

### CLI command is not found

Add the command to `PATH`, or configure the tool with a full executable path.

### Windows Terminal is unavailable

Change the preferred terminal to `powershell`, or install Windows Terminal and keep using `wt`.
