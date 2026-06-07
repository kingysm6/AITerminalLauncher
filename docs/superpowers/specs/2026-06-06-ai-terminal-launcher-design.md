# AI Terminal Launcher Design

Date: 2026-06-06
Status: Reviewed design

## Summary

Build a lightweight Windows launcher that lets the user open Codex, Claude, or OpenCode from any folder with one click. The MVP uses Windows Explorer context-menu entries because that is the fastest, most reliable way to support “any folder” without requiring a resident background process.

## Goals

- Add Explorer context-menu actions for folders and folder backgrounds.
- Launch Codex, Claude, or OpenCode in the selected/current directory.
- Support paths containing spaces and non-ASCII characters such as Chinese folder names.
- Keep the tool easy to install, uninstall, inspect, and modify.
- Avoid a heavy desktop UI for the MVP.

## Non-goals for MVP

- No always-running tray app.
- No Electron or Tauri desktop shell.
- No custom visual picker UI.
- No automatic installation of Codex, Claude, OpenCode, Windows Terminal, or PowerShell.
- No cloud service or telemetry.

## Recommended MVP

Use a script-based Windows utility with three layers:

1. **Launcher script**: receives a target directory and a tool name, validates inputs, then opens the configured terminal and runs the chosen CLI.
2. **Configuration file**: stores command names, terminal preference, and optional arguments.
3. **Install/uninstall scripts**: add or remove Windows Explorer context-menu registry entries.

This approach is preferred because it is transparent, quick to build, easy to debug, and does not introduce a runtime dependency beyond PowerShell and the target CLI tools.

## User Experience

Explorer should expose these entries:

- `Open with Codex`
- `Open with Claude`
- `Open with OpenCode`

Entries should work in two common situations:

- Right-clicking a folder itself.
- Right-clicking blank space inside a folder.

When invoked, the selected CLI opens in a new terminal window whose working directory is the selected/current folder.

## Architecture

```text
Explorer context menu
        |
        v
Registry command entry
        |
        v
launcher.ps1 -Tool <codex|claude|opencode> -Path <folder>
        |
        v
config.json command lookup
        |
        v
Windows Terminal or PowerShell launches target CLI in folder
```

## Components

### `config.json`

Stores editable defaults:

```json
{
  "terminal": "wt",
  "fallbackTerminal": "powershell",
  "tools": {
    "codex": {
      "command": "codex",
      "args": []
    },
    "claude": {
      "command": "claude",
      "args": []
    },
    "opencode": {
      "command": "opencode",
      "args": []
    }
  }
}
```

### `launcher.ps1`

Responsibilities:

- Accept `-Tool` and `-Path` parameters using normal PowerShell parameter syntax.
- Resolve and validate the target path.
- Load `config.json`.
- Validate that the tool exists in configuration.
- Start the configured terminal in the target directory. For Windows Terminal, use its directory option rather than relying on process inheritance.
- Run the configured CLI command.
- Emit clear errors for missing config, malformed config, unknown tool, invalid path, or missing command.

### `install.ps1`

Responsibilities:

- Locate the launcher directory.
- Add registry entries under the current user where possible.
- Register actions for both folder objects and folder backgrounds with deterministic key names owned by this utility.
- Quote paths safely so install locations with spaces or Chinese characters work.
- Avoid requiring administrator privileges for normal installation.

### `uninstall.ps1`

Responsibilities:

- Remove all registry keys created by `install.ps1`.
- Leave user configuration files intact unless explicitly asked otherwise.

## Registry Integration

The installer should target per-user Explorer context-menu locations where practical, such as:

- `HKCU:\Software\Classes\Directory\shell\AITerminalLauncher_<tool>`
- `HKCU:\Software\Classes\Directory\Background\shell\AITerminalLauncher_<tool>`

Each menu entry should invoke PowerShell with execution policy scoped to the process:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "<launcher.ps1>" -Tool "codex" -Path "%V"
```

The implementation must verify and document the placeholder used for each registry location:

- Folder object entries should pass the clicked folder path.
- Folder background entries should pass the current folder path.
- If `%V` does not behave correctly for either location on the target Windows version, the installer must use the verified alternative placeholder and record that choice in comments near the registry-writing code.

The launcher must not concatenate untrusted path strings into a single shell command when it can pass arguments as separate process arguments. When invoking Windows Terminal, the intended shape is:

```powershell
wt.exe -d "<target-directory>" powershell.exe -NoExit -Command "<tool command and args>"
```

When falling back to PowerShell directly, the intended shape is:

```powershell
Start-Process -FilePath "powershell.exe" -WorkingDirectory "<target-directory>" -ArgumentList @("-NoExit", "-Command", "<tool command and args>")
```

Implementation may adjust the exact invocation if testing shows a more reliable PowerShell 5.1-compatible form, but it must preserve these properties: new terminal window, correct working directory, safe quoting, and visible error output. Do not rely on `powershell.exe -WorkingDirectory` because Windows PowerShell 5.1 compatibility is required.

## Error Handling

- Invalid directory: show a readable PowerShell error and exit non-zero.
- Unknown tool: list valid tool names from `config.json`.
- Missing CLI executable: show the command that failed and suggest checking PATH.
- Missing Windows Terminal: fall back to PowerShell if configured, or show a clear message.
- Registry write failure: explain whether permissions or policy likely caused it.

The launcher should exit with non-zero status for validation/configuration failures. For interactive launches, failures should remain visible long enough for the user to read them.

## Security and Safety

- Do not download or execute remote code.
- Do not store API keys or credentials.
- Do not modify machine-wide registry hives unless explicitly configured later.
- Keep all generated commands quoted and parameterized to avoid path injection bugs.
- Treat configured tool commands as trusted local configuration, but treat Explorer-provided paths as untrusted input that must be validated with `Test-Path -LiteralPath` / `Resolve-Path -LiteralPath`.
- Prefer per-user installation to avoid admin permissions.

## Testing Strategy

Manual MVP verification:

1. Install context-menu entries.
2. Right-click a normal ASCII folder and launch each tool.
3. Right-click a folder whose path contains spaces.
4. Right-click a folder whose path contains Chinese characters.
5. Right-click blank space inside a folder and launch each tool.
6. Temporarily configure a fake/missing command and confirm the error is readable.
7. Run uninstall and verify registry entries disappear.

Automated script-level checks:

- Validate `config.json` parsing.
- Validate unknown tool handling.
- Validate invalid path handling.
- Validate generated command quoting for paths with spaces and non-ASCII characters.
- Validate install/uninstall registry key generation without writing to real registry by factoring registry key/value construction into testable functions or dry-run output.
- Use a repository-local test runner so the MVP does not require Pester or any external test framework.

## Future Extensions

- A unified `AI Terminal Here` submenu.
- Global hotkey support.
- Tray app with recent folders.
- CLI auto-detection and setup wizard.
- Tauri-based picker UI if a richer visual workflow becomes valuable.

## Acceptance Criteria

- A user can install the launcher without administrator privileges.
- Explorer shows entries for Codex, Claude, and OpenCode when right-clicking a folder and when right-clicking blank space inside a folder.
- Each entry opens the selected CLI in the intended folder in a new terminal window.
- Paths with spaces and Chinese characters work for both folder-object and folder-background invocations.
- The user can uninstall cleanly.
- Configuration is editable without changing source code.
- Missing CLI/config/path failures produce readable output and non-zero exits.
