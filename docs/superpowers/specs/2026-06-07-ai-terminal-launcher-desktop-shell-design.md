# AI Terminal Launcher Desktop Shell Design

Date: 2026-06-07
Status: Reviewed design

## Summary

Evolve the current script-based AI Terminal Launcher into a more complete Windows desktop utility. The new version keeps the existing Explorer context-menu launcher, and adds a resident tray application with a settings window, per-tool global hotkeys, login auto-start, and a folder-picker fallback when Explorer context is unavailable.

The product must no longer be limited to three fixed tools. `Codex`, `Claude`, and `OpenCode` remain preloaded defaults, but the user can add more CLI tools later. Every tool uses the same model: command, arguments, enabled state, tray visibility, context-menu visibility, and its own optional hotkey.

## Goals

- Let the user open an AI CLI in any folder with one action.
- Support both mouse and keyboard workflows.
- Keep the existing Explorer context-menu flow.
- Add a tray-resident desktop shell that supports:
  - global hotkeys
  - settings UI
  - launch-at-login
  - folder-picker fallback
- Allow the user to add new CLI tools later without code changes.
- Keep installation and operation per-user where practical.
- Reuse the existing PowerShell launch layer instead of rewriting it.

## Non-goals

- No cloud sync.
- No telemetry.
- No plugin system for third-party extensions.
- No custom shell extension written in native C++ for MVP.
- No requirement to auto-install AI CLIs.
- No machine-wide admin-only install path for the first usable version.

## User Workflows

### Explorer right-click

1. User right-clicks a folder, or right-clicks blank space inside a folder.
2. Explorer shows one entry per enabled tool that is configured to appear in the context menu.
3. User selects a tool.
4. The existing launch layer opens the configured terminal in the target folder and runs the selected CLI.

### Global hotkey

1. User presses a tool-specific global hotkey.
2. The tray application resolves the target folder using this priority:
   - selected folder in the active Explorer window
   - current directory of the active Explorer window
   - folder-picker dialog if no usable Explorer context exists
3. The tray application invokes the existing launch layer for the selected tool and target folder.

### Settings-driven customization

1. User opens the settings window from the tray icon.
2. User can add, edit, enable, disable, or remove tools.
3. User can assign or change hotkeys, terminal preference, tray visibility, context-menu visibility, and auto-start.
4. Settings are saved to a user-level config file and take effect immediately where possible.

## Recommended Architecture

Use a hybrid architecture with two layers:

1. **Desktop shell application**
   - Technology: .NET 8 WinForms
   - Responsibilities: tray icon, settings window, hotkey registration, Explorer context resolution, folder picker fallback, auto-start management, logging, and invoking the launch layer

2. **Launch execution layer**
   - Technology: existing PowerShell scripts and module
   - Responsibilities: config loading compatibility, tool validation, path validation, terminal command generation, CLI launch, and Explorer context-menu install/uninstall

This is preferred over a pure PowerShell UI because the tray and hotkey features fit better in a Windows desktop host. It is preferred over a full rewrite because the current launcher logic already handles path validation, terminal selection, and registry integration cleanly.

## Component Design

### Desktop Shell Components

#### Tray Host

- Owns the tray icon and tray menu.
- Supports at minimum:
  - one launch action per enabled tray-visible tool
  - `Settings`
  - `Install Context Menu`
  - `Remove Context Menu`
  - `Launch at Login`
  - `Exit`
- Double-clicking the tray icon opens settings.

#### Hotkey Manager

- Registers one global hotkey per enabled tool that has a hotkey configured.
- Unregisters and re-registers hotkeys when settings change.
- Detects registration failures and reports them clearly.
- Must not silently ignore collisions or invalid combinations.

#### Explorer Context Resolver

- Resolves target paths using this rule set:
  - if the active Explorer window has a selected folder, use it
  - otherwise use the active Explorer window's current folder
  - otherwise show the folder-picker dialog
- Must reject non-folder targets.
- Must preserve paths with spaces and non-ASCII characters.

#### Launch Coordinator

- Receives a `toolId` and target folder.
- Resolves the configured tool from the shared config.
- Invokes the PowerShell launch layer with an explicit config path when possible.
- Centralizes launch calls so tray, hotkey, and future UI actions do not fork behavior.

#### Settings UI

- Provides a simple desktop settings window, not a dashboard.
- Supports:
  - listing all tools
  - add tool
  - edit tool
  - remove tool
  - enable/disable tool
  - set tool hotkey
  - toggle tray visibility
  - toggle context-menu visibility
  - edit command and arguments
  - choose terminal preference
  - toggle launch at login
- Saving should immediately update runtime state when safe to do so.

#### Config Store

- Uses one user-level config file as the source of truth.
- Settings UI edits this file.
- Manual edits to the same file are supported.
- The app must at minimum reload config on startup; runtime file watching is optional for the first version.

### Launch Layer Components

#### Shared module

- Keep and extend `src/AITerminalLauncher.psm1`.
- Move from fixed-tool assumptions to dynamic tool list handling.
- Preserve path validation and terminal command construction patterns.

#### Explorer integration scripts

- Keep `install.ps1` and `uninstall.ps1`.
- Change them from fixed entries for three hard-coded tools to dynamic entries based on the enabled tools configured for context-menu visibility.
- Continue to support `-DryRun`.

#### Launcher entrypoint

- Keep `launcher.ps1`.
- Add support for a user config path, for example through `-ConfigPath`, plus a compatibility fallback chain.

## Configuration Design

The primary config file should live at:

`%LocalAppData%\AITerminalLauncher\config.json`

The PowerShell layer should support this config resolution order:

1. explicit `-ConfigPath`
2. `AITL_CONFIG_PATH` environment variable
3. local script-directory `config.json` fallback for compatibility

### Configuration shape

```json
{
  "version": 1,
  "terminal": {
    "preferred": "wt",
    "fallback": "powershell"
  },
  "startup": {
    "launchAtLogin": false,
    "startMinimizedToTray": true
  },
  "fallbackBehavior": {
    "mode": "folderPicker"
  },
  "tools": [
    {
      "id": "codex",
      "displayName": "Codex",
      "command": "codex",
      "args": [],
      "enabled": true,
      "showInContextMenu": true,
      "showInTrayMenu": true,
      "hotkey": {
        "enabled": true,
        "modifiers": ["Control", "Alt"],
        "key": "C"
      }
    }
  ]
}
```

### Tool model rules

- `id` must be stable and unique.
- `displayName` is user-facing text.
- `command` is the executable name or full path.
- `args` is an ordered list of arguments.
- `enabled` disables the tool globally without deleting it.
- `showInContextMenu` controls Explorer menu creation.
- `showInTrayMenu` controls tray menu visibility.
- `hotkey.enabled` controls whether a global hotkey is registered.

The application should preload `Codex`, `Claude`, and `OpenCode` as default tools in a fresh config.

## Explorer Context-Menu Behavior

The product must preserve the current Explorer integration pattern:

- `HKCU:\Software\Classes\Directory\shell\AITerminalLauncher_<toolId>`
- `HKCU:\Software\Classes\Directory\Background\shell\AITerminalLauncher_<toolId>`

Each entry should continue to execute the launcher with process-scoped execution policy bypass and the Explorer-provided path placeholder. Registry generation must remain deterministic and testable through dry-run output.

The context menu is no longer fixed to three entries. It must be generated from the set of tools where:

- `enabled = true`
- `showInContextMenu = true`

If the user later adds a new CLI tool and enables context-menu visibility, that tool should receive its own context-menu entry on install or refresh.

## Hotkey Model

- Every tool may have zero or one global hotkey.
- Hotkeys are defined structurally, not as one free-form string, so they can be validated and re-registered reliably.
- Duplicate hotkeys across enabled tools must be rejected in settings validation.
- Registration failure at the Windows level must produce a visible warning.

## Auto-start Design

For the first usable version, auto-start should be implemented per-user via:

`HKCU:\Software\Microsoft\Windows\CurrentVersion\Run`

The tray app should start minimized with a tray-only presence when launched at login.

## Error Handling and Logging

### User-facing errors

- Missing Explorer context: open folder picker.
- Invalid selected target: show readable error.
- Missing CLI executable: show which tool failed and suggest checking the configured command or `PATH`.
- Config parse failure: show readable config error and path.
- Hotkey registration failure: identify the affected tool and hotkey.
- Terminal launch failure: identify the target folder and tool.

### Diagnostics

- Write detailed logs under:
  - `%LocalAppData%\AITerminalLauncher\logs\`
- Keep user-facing messages short and actionable.
- Do not swallow exceptions silently.

## Packaging Strategy

### Phase 1: Functional desktop product

Deliver:

- tray application
- settings window
- global hotkeys
- Explorer context resolution
- folder-picker fallback
- launch-at-login
- existing context-menu support retained and integrated
- shared config between desktop shell and PowerShell layer

### Phase 2: Installation polish

Add later:

- application icon polish
- self-contained or single-file publishing decision
- installer
- start menu shortcuts
- uninstall entry

This split keeps the critical path focused on core behavior rather than packaging ceremony.

## Testing Strategy

### Automated

- Extend the existing PowerShell test runner to cover dynamic tool lists and config-path resolution.
- Add desktop-shell tests for:
  - config parsing and validation
  - hotkey conflict validation
  - tool add/edit/remove behavior
  - tray menu generation from config
  - launch request construction

### Manual

1. Launch the tray app.
2. Trigger each default tool by hotkey.
3. Verify selected-folder priority in Explorer.
4. Verify current-folder fallback in Explorer.
5. Verify folder-picker fallback when no Explorer context exists.
6. Add a new custom CLI tool and confirm it can:
   - appear in settings
   - receive a hotkey
   - launch correctly
   - appear in tray menu
   - appear in Explorer context menu after install/refresh
7. Verify paths with spaces.
8. Verify paths with Chinese characters.
9. Verify auto-start behavior after sign-in.
10. Verify context-menu uninstall removes only owned keys.

## Acceptance Criteria

- The app runs as a tray-resident Windows utility.
- The user can configure launch-at-login.
- The user can open settings and manage tools without editing source code.
- The user can also manually edit the config file.
- Global hotkeys launch the configured tool against:
  - selected folder first
  - current Explorer folder second
  - folder-picker fallback otherwise
- Explorer context-menu launch still works.
- The product is not limited to three hard-coded tools.
- A user-added CLI can have its own hotkey, tray entry, and context-menu entry.
- The launch layer still supports paths with spaces and Chinese characters.
- Failures are readable and logged.

## Implementation Notes

- Prefer the hybrid architecture over a rewrite.
- Do not introduce a plugin system for this scope.
- Keep all launcher path handling parameterized and validated with `-LiteralPath` patterns.
- Treat the desktop shell as the primary user-facing app, and the PowerShell layer as the execution backend.
- This workspace is currently not a git repository, so any workflow step that requires committing the spec is intentionally deferred.
