# AI Terminal Launcher Desktop Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the current script-based launcher into a Windows tray utility with global hotkeys, settings UI, login auto-start, folder-picker fallback, and dynamic support for user-added AI CLIs while preserving Explorer context-menu launching.

**Architecture:** Keep the existing PowerShell scripts as the execution backend, but remove their fixed-tool assumptions and make them read a shared dynamic config. Add a .NET 8 WinForms desktop shell that owns tray behavior, hotkeys, Explorer context detection, settings editing, and auto-start. Keep non-UI logic in a small .NET core library so it can be tested without external test frameworks.

**Tech Stack:** .NET 8 (`net8.0` core library, `net8.0-windows` WinForms app), C# 12, Windows PowerShell 5.1-compatible scripts, JSON configuration, per-user Windows registry integration under `HKCU`.

---

## File Structure

- Create: `AITerminalLauncher.sln`
- Create: `src/AITerminalLauncher.Core/AITerminalLauncher.Core.csproj`
- Create: `src/AITerminalLauncher.Core/Config/AppConfig.cs`
- Create: `src/AITerminalLauncher.Core/Config/ToolConfig.cs`
- Create: `src/AITerminalLauncher.Core/Config/HotkeyConfig.cs`
- Create: `src/AITerminalLauncher.Core/Config/DefaultConfigFactory.cs`
- Create: `src/AITerminalLauncher.Core/Config/ConfigPathResolver.cs`
- Create: `src/AITerminalLauncher.Core/Config/ConfigStore.cs`
- Create: `src/AITerminalLauncher.Core/Validation/ConfigValidator.cs`
- Create: `src/AITerminalLauncher.Core/Launch/LaunchRequest.cs`
- Create: `src/AITerminalLauncher.Core/Launch/PowerShellLaunchRequestBuilder.cs`
- Create: `src/AITerminalLauncher.Core/Explorer/ExplorerWindowSnapshot.cs`
- Create: `src/AITerminalLauncher.Core/Explorer/SelectedItemSnapshot.cs`
- Create: `src/AITerminalLauncher.Core/Explorer/ExplorerTargetResolver.cs`
- Create: `src/AITerminalLauncher.Core/Hotkeys/HotkeyChord.cs`
- Create: `src/AITerminalLauncher.Core/Hotkeys/HotkeyConflictDetector.cs`
- Create: `src/AITerminalLauncher.Core/Tray/TrayMenuEntry.cs`
- Create: `src/AITerminalLauncher.Core/Tray/TrayMenuBuilder.cs`
- Create: `src/AITerminalLauncher.App/AITerminalLauncher.App.csproj`
- Create: `src/AITerminalLauncher.App/Program.cs`
- Create: `src/AITerminalLauncher.App/Tray/LauncherApplicationContext.cs`
- Create: `src/AITerminalLauncher.App/Hotkeys/GlobalHotkeyService.cs`
- Create: `src/AITerminalLauncher.App/Hotkeys/HotkeyMessageWindow.cs`
- Create: `src/AITerminalLauncher.App/Explorer/ShellExplorerWindowProvider.cs`
- Create: `src/AITerminalLauncher.App/Dialogs/FolderPickerService.cs`
- Create: `src/AITerminalLauncher.App/Services/LaunchService.cs`
- Create: `src/AITerminalLauncher.App/Services/ContextMenuScriptService.cs`
- Create: `src/AITerminalLauncher.App/Services/RunAtLoginService.cs`
- Create: `src/AITerminalLauncher.App/Forms/SettingsForm.cs`
- Create: `src/AITerminalLauncher.App/Forms/ToolEditorForm.cs`
- Create: `tests/AITerminalLauncher.Core.Tests/AITerminalLauncher.Core.Tests.csproj`
- Create: `tests/AITerminalLauncher.Core.Tests/Program.cs`
- Modify: `config.json`
- Modify: `src/AITerminalLauncher.psm1`
- Modify: `launcher.ps1`
- Modify: `install.ps1`
- Modify: `uninstall.ps1`
- Modify: `tests/run-tests.ps1`
- Modify: `README.md`
- Modify: `USAGE.md`

## Responsibilities

- `AITerminalLauncher.Core`: shared config, validation, tray-menu composition, hotkey collision checks, Explorer selection resolution rules, PowerShell launch request construction.
- `AITerminalLauncher.App`: WinForms tray host, global hotkey runtime, COM-based Explorer integration, folder picker, settings forms, auto-start, and script invocation.
- Existing PowerShell scripts: backend compatibility layer for terminal launch and Explorer context-menu registry management.
- `tests/AITerminalLauncher.Core.Tests`: dependency-free console test runner for the .NET core library.
- `tests/run-tests.ps1`: orchestrates PowerShell tests plus the .NET console test runner.

## Implementation Notes

- Use `net8.0` for the core library and console test project; use `net8.0-windows` with `UseWindowsForms=true` for the tray app.
- Do not add xUnit, MSTest, or any external NuGet-only test framework. Keep tests dependency-free.
- Keep `config.json` in the repo as the distributable default config. The tray app should copy or save an equivalent config into `%LocalAppData%\AITerminalLauncher\config.json`.
- Add `-ConfigPath` support to `launcher.ps1`, `install.ps1`, and `uninstall.ps1` so the desktop shell and scripts can share one user config.
- Treat the tool list as dynamic everywhere. `Codex`, `Claude`, and `OpenCode` are seeded defaults, not hard-coded special cases.
- Validate tool IDs so they are safe for registry key names and stable for hotkey and tray routing.
- Keep all path handling parameterized. Explorer-provided paths must continue to use `Test-Path -LiteralPath` and `Resolve-Path -LiteralPath`.
- This workspace is not a git repository, so commit steps are intentionally omitted during execution.

---

### Task 1: Scaffold the .NET solution and a dependency-free test harness

**Files:**
- Create: `AITerminalLauncher.sln`
- Create: `src/AITerminalLauncher.Core/AITerminalLauncher.Core.csproj`
- Create: `src/AITerminalLauncher.App/AITerminalLauncher.App.csproj`
- Create: `tests/AITerminalLauncher.Core.Tests/AITerminalLauncher.Core.Tests.csproj`
- Create: `tests/AITerminalLauncher.Core.Tests/Program.cs`
- Modify: `tests/run-tests.ps1`

- [ ] **Step 1: Add a failing .NET smoke test runner**

Create `tests/AITerminalLauncher.Core.Tests/Program.cs` with a minimal assertion harness that expects a not-yet-created config factory:

```csharp
using AITerminalLauncher.Core.Config;

static class AssertEx
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}

var config = DefaultConfigFactory.Create();
AssertEx.True(config.Tools.Count == 3, "default tool count");
Console.WriteLine("All .NET tests passed.");
```

- [ ] **Step 2: Create the solution and project shells**

Run:

```powershell
dotnet new sln -n AITerminalLauncher
dotnet new classlib -n AITerminalLauncher.Core -o .\src\AITerminalLauncher.Core --framework net8.0
dotnet new winforms -n AITerminalLauncher.App -o .\src\AITerminalLauncher.App --framework net8.0
dotnet new console -n AITerminalLauncher.Core.Tests -o .\tests\AITerminalLauncher.Core.Tests --framework net8.0
dotnet sln .\AITerminalLauncher.sln add .\src\AITerminalLauncher.Core\AITerminalLauncher.Core.csproj
dotnet sln .\AITerminalLauncher.sln add .\src\AITerminalLauncher.App\AITerminalLauncher.App.csproj
dotnet sln .\AITerminalLauncher.sln add .\tests\AITerminalLauncher.Core.Tests\AITerminalLauncher.Core.Tests.csproj
dotnet add .\src\AITerminalLauncher.App\AITerminalLauncher.App.csproj reference .\src\AITerminalLauncher.Core\AITerminalLauncher.Core.csproj
dotnet add .\tests\AITerminalLauncher.Core.Tests\AITerminalLauncher.Core.Tests.csproj reference .\src\AITerminalLauncher.Core\AITerminalLauncher.Core.csproj
```

Expected: project files exist, but the test project does not compile yet because `DefaultConfigFactory` is missing.

- [ ] **Step 3: Update the PowerShell test runner to invoke the .NET console tests**

At the end of `tests/run-tests.ps1`, add:

```powershell
$dotnetOutput = & dotnet run --project (Join-Path $repoRoot 'tests\AITerminalLauncher.Core.Tests\AITerminalLauncher.Core.Tests.csproj') 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Assertion failed: .NET test runner exits 0. Output: $dotnetOutput"
}
```

- [ ] **Step 4: Run tests and verify the .NET side fails first**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```

Expected: FAIL with a compile error that `AITerminalLauncher.Core.Config.DefaultConfigFactory` does not exist yet.

---

### Task 2: Define the shared dynamic config model and default config

**Files:**
- Create: `src/AITerminalLauncher.Core/Config/AppConfig.cs`
- Create: `src/AITerminalLauncher.Core/Config/ToolConfig.cs`
- Create: `src/AITerminalLauncher.Core/Config/HotkeyConfig.cs`
- Create: `src/AITerminalLauncher.Core/Config/DefaultConfigFactory.cs`
- Modify: `config.json`
- Modify: `tests/AITerminalLauncher.Core.Tests/Program.cs`

- [ ] **Step 1: Write failing config-shape tests**

Extend `tests/AITerminalLauncher.Core.Tests/Program.cs` to assert the dynamic tool-list shape:

```csharp
var config = DefaultConfigFactory.Create();
AssertEx.True(config.Version == 1, "config version");
AssertEx.True(config.Tools.Count == 3, "default tool count");
AssertEx.True(config.Tools.Any(t => t.Id == "codex"), "codex default exists");
AssertEx.True(config.Tools.Any(t => t.Id == "claude"), "claude default exists");
AssertEx.True(config.Tools.Any(t => t.Id == "opencode"), "opencode default exists");
AssertEx.True(config.Tools.All(t => t.ShowInContextMenu), "defaults show in context menu");
```

- [ ] **Step 2: Run the .NET tests to verify failure**

Run:

```powershell
dotnet run --project .\tests\AITerminalLauncher.Core.Tests\AITerminalLauncher.Core.Tests.csproj
```

Expected: FAIL because the config types do not exist yet.

- [ ] **Step 3: Implement the minimal config model and default factory**

Add focused types such as:

```csharp
public sealed class AppConfig
{
    public int Version { get; set; } = 1;
    public TerminalConfig Terminal { get; set; } = new();
    public StartupConfig Startup { get; set; } = new();
    public FallbackBehaviorConfig FallbackBehavior { get; set; } = new();
    public List<ToolConfig> Tools { get; set; } = new();
}
```

And a default factory:

```csharp
public static class DefaultConfigFactory
{
    public static AppConfig Create()
    {
        return new AppConfig
        {
            Tools =
            [
                ToolConfig.CreateDefault("codex", "Codex", "codex", "C"),
                ToolConfig.CreateDefault("claude", "Claude", "claude", "L"),
                ToolConfig.CreateDefault("opencode", "OpenCode", "opencode", "O"),
            ]
        };
    }
}
```

- [ ] **Step 4: Replace the repo `config.json` with the new schema**

Use the dynamic structure from the spec:

```json
{
  "version": 1,
  "terminal": { "preferred": "wt", "fallback": "powershell" },
  "startup": { "launchAtLogin": false, "startMinimizedToTray": true },
  "fallbackBehavior": { "mode": "folderPicker" },
  "tools": [
    {
      "id": "codex",
      "displayName": "Codex",
      "command": "codex",
      "args": [],
      "enabled": true,
      "showInContextMenu": true,
      "showInTrayMenu": true,
      "hotkey": { "enabled": true, "modifiers": ["Control", "Alt"], "key": "C" }
    }
  ]
}
```

Populate all three defaults in the real file.

- [ ] **Step 5: Re-run the .NET tests**

Expected: the default-config tests PASS.

---

### Task 3: Add config loading, saving, path resolution, and validation in the core library

**Files:**
- Create: `src/AITerminalLauncher.Core/Config/ConfigPathResolver.cs`
- Create: `src/AITerminalLauncher.Core/Config/ConfigStore.cs`
- Create: `src/AITerminalLauncher.Core/Validation/ConfigValidator.cs`
- Modify: `tests/AITerminalLauncher.Core.Tests/Program.cs`

- [ ] **Step 1: Write failing tests for config persistence and validation**

Add tests covering:

```csharp
var path = ConfigPathResolver.GetUserConfigPath();
AssertEx.True(path.EndsWith(@"AITerminalLauncher\config.json"), "user config path");

var config = DefaultConfigFactory.Create();
ConfigValidator.Validate(config);

var duplicate = DefaultConfigFactory.Create();
duplicate.Tools[1].Hotkey!.Key = duplicate.Tools[0].Hotkey!.Key;
duplicate.Tools[1].Hotkey!.Modifiers = duplicate.Tools[0].Hotkey!.Modifiers.ToList();
AssertEx.Throws(() => ConfigValidator.Validate(duplicate), "duplicate hotkeys fail");
```

- [ ] **Step 2: Run the .NET tests and verify failure**

Expected: FAIL because these services do not exist yet.

- [ ] **Step 3: Implement config path resolution and JSON save/load**

Create a focused store that can:

```csharp
public sealed class ConfigStore
{
    public AppConfig LoadFromPath(string path) { ... }
    public void SaveToPath(string path, AppConfig config) { ... }
    public AppConfig LoadOrCreateUserConfig() { ... }
}
```

Rules:
- use `%LocalAppData%\AITerminalLauncher\config.json`
- create the parent directory if missing
- if the user config does not exist, write a fresh default config

- [ ] **Step 4: Implement config validation**

Validate at least:
- unique non-empty tool IDs
- non-empty display name
- non-empty command
- hotkeys only when both key and modifiers are valid
- no duplicate enabled hotkeys

- [ ] **Step 5: Re-run the .NET tests**

Expected: config path, load/save, and validation tests PASS.

---

### Task 4: Upgrade the PowerShell backend to dynamic tools and explicit config paths

**Files:**
- Modify: `src/AITerminalLauncher.psm1`
- Modify: `launcher.ps1`
- Modify: `install.ps1`
- Modify: `uninstall.ps1`
- Modify: `tests/run-tests.ps1`

- [ ] **Step 1: Write failing PowerShell tests for the new config contract**

Replace fixed-tool assertions with dynamic ones such as:

```powershell
$config = Get-AITLConfig -ConfigPath $configPath
Assert-Equal 1 $config.version 'config version'
Assert-Equal 'wt' $config.terminal.preferred 'preferred terminal defaults to wt'
Assert-Equal 3 $config.tools.Count 'three default tools exist'

$codexTool = Get-AITLToolConfig -Config $config -ToolId 'codex'
Assert-Equal 'codex' $codexTool.command 'codex tool resolves from list'
Assert-Throws { Get-AITLToolConfig -Config $config -ToolId 'missing' } 'unknown tool id throws'
```

Also add config-path resolution checks:

```powershell
$resolved = Resolve-AITLConfigPath -ExplicitPath $configPath
Assert-Equal $configPath $resolved 'explicit config path wins'
```

- [ ] **Step 2: Run `tests/run-tests.ps1` and verify failure**

Expected: FAIL because the module still assumes the old object-shaped `tools` section and scripts do not accept `-ConfigPath`.

- [ ] **Step 3: Implement config-path resolution and dynamic tool lookup in `AITerminalLauncher.psm1`**

Add functions like:

```powershell
function Resolve-AITLConfigPath {
    param([string] $ExplicitPath)
    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) { return $ExplicitPath }
    if (-not [string]::IsNullOrWhiteSpace($env:AITL_CONFIG_PATH)) { return $env:AITL_CONFIG_PATH }
    return (Join-Path $PSScriptRoot '..\config.json')
}

function Get-AITLToolConfig {
    param($Config, [string] $ToolId)
    $tool = @($Config.tools) | Where-Object { $_.id -eq $ToolId -and $_.enabled }
    ...
}
```

Keep readable errors for missing config and unknown tool IDs.

- [ ] **Step 4: Thread `-ConfigPath` through all scripts**

Update:
- `launcher.ps1` to accept `-ConfigPath`
- `install.ps1` to accept `-ConfigPath`
- `uninstall.ps1` to accept `-ConfigPath`

And ensure each script passes the chosen config path down to module functions.

- [ ] **Step 5: Make context-menu install/uninstall dynamic**

Replace the hard-coded three-tool list with:

```powershell
$tools = @($config.tools) | Where-Object { $_.enabled -and $_.showInContextMenu }
foreach ($tool in $tools) {
    $label = "Open with $($tool.displayName)"
    $operations += New-AITLInstallRegistryOperation -LauncherPath $launcherPath -ToolId $tool.id -Label $label -ConfigPath $resolvedConfigPath
}
```

Do the matching dynamic uninstall path using the same resolved config.

- [ ] **Step 6: Re-run `tests/run-tests.ps1`**

Expected: PowerShell tests PASS again with the dynamic config shape.

---

### Task 5: Preserve safe launch command generation under the new schema

**Files:**
- Modify: `src/AITerminalLauncher.psm1`
- Modify: `launcher.ps1`
- Modify: `tests/run-tests.ps1`

- [ ] **Step 1: Add failing launch tests for the new terminal config shape**

Update the existing command-generation assertions to use:

```powershell
$wtCommand = New-AITLLaunchCommand -Terminal $config.terminal.preferred -FallbackTerminal $config.terminal.fallback -TargetPath $repoRoot -ToolConfig $codexTool
Assert-Equal 'wt.exe' $wtCommand.FilePath 'Windows Terminal executable is wt.exe'
Assert-Contains $wtCommand.ArgumentList $repoRoot 'target path is preserved'
Assert-Contains $wtCommand.ArgumentList 'codex' 'tool command is present'
```

Also verify `launcher.ps1 -DryRun -ConfigPath ...` prints structured launch data.

- [ ] **Step 2: Run the PowerShell tests to verify failure**

Expected: FAIL until the launch path reads the new terminal shape everywhere.

- [ ] **Step 3: Update the launch pipeline without changing its safety properties**

Preserve the existing structured launch-object pattern:

```powershell
[pscustomobject]@{
    FilePath = 'wt.exe'
    ArgumentList = @('-d', $TargetPath, 'powershell.exe', '-NoExit', '-Command', $toolCommand)
    WorkingDirectory = $null
}
```

Keep:
- `Resolve-Path -LiteralPath`
- `Start-Process -WorkingDirectory`
- no `powershell.exe -WorkingDirectory`
- no concatenated untrusted path strings

- [ ] **Step 4: Re-run `tests/run-tests.ps1`**

Expected: dry-run and launch-command tests PASS under the new schema.

---

### Task 6: Add a core launch-request builder for the desktop shell

**Files:**
- Create: `src/AITerminalLauncher.Core/Launch/LaunchRequest.cs`
- Create: `src/AITerminalLauncher.Core/Launch/PowerShellLaunchRequestBuilder.cs`
- Modify: `tests/AITerminalLauncher.Core.Tests/Program.cs`

- [ ] **Step 1: Write failing tests for desktop-to-script launch bridging**

Add tests such as:

```csharp
var request = PowerShellLaunchRequestBuilder.BuildLauncherRequest(
    launcherScriptPath: @"F:\repo\launcher.ps1",
    configPath: @"C:\Users\me\AppData\Local\AITerminalLauncher\config.json",
    toolId: "codex",
    targetPath: @"C:\Work\Project");

AssertEx.True(request.FileName == "powershell.exe", "powershell host");
AssertEx.True(request.Arguments.Contains("-Tool \"codex\""), "tool argument");
AssertEx.True(request.Arguments.Contains("-ConfigPath"), "explicit config path");
```

- [ ] **Step 2: Run the .NET tests and verify failure**

Expected: FAIL because the launch-request builder does not exist.

- [ ] **Step 3: Implement the builder**

Use a simple immutable result:

```csharp
public sealed record LaunchRequest(string FileName, string Arguments);
```

And generate PowerShell execution like:

```csharp
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "<launcher.ps1>" -Tool "<toolId>" -Path "<targetPath>" -ConfigPath "<configPath>"
```

Quote only trusted generated segments and keep path arguments explicit.

- [ ] **Step 4: Re-run the .NET tests**

Expected: launch-request tests PASS.

---

### Task 7: Model Explorer target resolution independently of COM

**Files:**
- Create: `src/AITerminalLauncher.Core/Explorer/ExplorerWindowSnapshot.cs`
- Create: `src/AITerminalLauncher.Core/Explorer/SelectedItemSnapshot.cs`
- Create: `src/AITerminalLauncher.Core/Explorer/ExplorerTargetResolver.cs`
- Modify: `tests/AITerminalLauncher.Core.Tests/Program.cs`

- [ ] **Step 1: Write failing tests for the selection rules**

Cover the exact rule order the user approved:

```csharp
var snapshot = new ExplorerWindowSnapshot(
    currentFolder: @"C:\Repo",
    selectedItems:
    [
        new SelectedItemSnapshot(@"C:\Repo\SubFolder", isFolder: true)
    ]);

var target = ExplorerTargetResolver.Resolve(snapshot);
AssertEx.True(target == @"C:\Repo\SubFolder", "selected folder wins");
```

Also cover:
- selected file + current folder present => current folder wins
- no selected folder + current folder present => current folder wins
- no Explorer context => resolver returns `null`

- [ ] **Step 2: Run the .NET tests and verify failure**

Expected: FAIL because the resolver types do not exist.

- [ ] **Step 3: Implement the pure resolution logic**

Keep it COM-free:

```csharp
public static string? Resolve(ExplorerWindowSnapshot? snapshot)
{
    var selectedFolder = snapshot?.SelectedItems.FirstOrDefault(item => item.IsFolder);
    if (selectedFolder is not null) return selectedFolder.Path;
    return snapshot?.CurrentFolder;
}
```

- [ ] **Step 4: Re-run the .NET tests**

Expected: selection-priority tests PASS.

---

### Task 8: Add hotkey and tray-menu composition logic to the core library

**Files:**
- Create: `src/AITerminalLauncher.Core/Hotkeys/HotkeyChord.cs`
- Create: `src/AITerminalLauncher.Core/Hotkeys/HotkeyConflictDetector.cs`
- Create: `src/AITerminalLauncher.Core/Tray/TrayMenuEntry.cs`
- Create: `src/AITerminalLauncher.Core/Tray/TrayMenuBuilder.cs`
- Modify: `tests/AITerminalLauncher.Core.Tests/Program.cs`

- [ ] **Step 1: Write failing tests for hotkey conflicts and tray visibility**

Add tests like:

```csharp
var config = DefaultConfigFactory.Create();
var trayEntries = TrayMenuBuilder.BuildLaunchEntries(config.Tools);
AssertEx.True(trayEntries.Count == 3, "default tray entries");

config.Tools[2].ShowInTrayMenu = false;
trayEntries = TrayMenuBuilder.BuildLaunchEntries(config.Tools);
AssertEx.True(trayEntries.Count == 2, "tray hides disabled items");

var conflict = HotkeyConflictDetector.FindDuplicates(config.Tools);
AssertEx.True(conflict.Count == 0, "defaults do not conflict");
```

- [ ] **Step 2: Run the .NET tests and verify failure**

Expected: FAIL because hotkey and tray builders do not exist.

- [ ] **Step 3: Implement the builders**

Keep them data-only:

```csharp
public sealed record TrayMenuEntry(string ToolId, string DisplayName);
```

And build them from:
- `enabled = true`
- `showInTrayMenu = true`

Implement a duplicate detector over enabled hotkeys so runtime registration can fail early with a clear message.

- [ ] **Step 4: Re-run the .NET tests**

Expected: tray and hotkey tests PASS.

---

### Task 9: Build the WinForms tray host and the PowerShell-backed launch service

**Files:**
- Create: `src/AITerminalLauncher.App/Program.cs`
- Create: `src/AITerminalLauncher.App/Tray/LauncherApplicationContext.cs`
- Create: `src/AITerminalLauncher.App/Services/LaunchService.cs`
- Create: `src/AITerminalLauncher.App/Dialogs/FolderPickerService.cs`
- Modify: `src/AITerminalLauncher.App/AITerminalLauncher.App.csproj`

- [ ] **Step 1: Add a build-only failing milestone**

Run:

```powershell
dotnet build .\src\AITerminalLauncher.App\AITerminalLauncher.App.csproj
```

Expected: FAIL because the app project still contains only template code and no tray-specific types.

- [ ] **Step 2: Replace the template app startup with a tray `ApplicationContext`**

Set `Program.cs` to:

```csharp
ApplicationConfiguration.Initialize();
Application.Run(new LauncherApplicationContext());
```

And in `LauncherApplicationContext` create:
- `NotifyIcon`
- `ContextMenuStrip`
- launch entries from `TrayMenuBuilder`
- `Settings`
- `Install Context Menu`
- `Remove Context Menu`
- `Launch at Login`
- `Exit`

- [ ] **Step 3: Implement a launch service that shells out to `launcher.ps1`**

Create a focused adapter that:
- resolves the desktop app's user config path
- requests a target directory
- builds the PowerShell invocation with `PowerShellLaunchRequestBuilder`
- launches it with `ProcessStartInfo`

- [ ] **Step 4: Add folder-picker fallback**

Implement a simple folder picker service:

```csharp
using var dialog = new FolderBrowserDialog();
if (dialog.ShowDialog() == DialogResult.OK) { return dialog.SelectedPath; }
return null;
```

- [ ] **Step 5: Rebuild the app**

Expected: the WinForms app builds and opens a tray icon with static or partially wired menu items.

---

### Task 10: Implement COM-backed Explorer context lookup and runtime global hotkeys

**Files:**
- Create: `src/AITerminalLauncher.App/Explorer/ShellExplorerWindowProvider.cs`
- Create: `src/AITerminalLauncher.App/Hotkeys/GlobalHotkeyService.cs`
- Create: `src/AITerminalLauncher.App/Hotkeys/HotkeyMessageWindow.cs`
- Modify: `src/AITerminalLauncher.App/Tray/LauncherApplicationContext.cs`

- [ ] **Step 1: Add a manual verification checkpoint before writing code**

Document the expected behavior in a temporary checklist:
- if a folder is selected in the active Explorer window, use it
- else use the current folder
- else open the folder picker

This is the behavior the runtime implementation must match exactly.

- [ ] **Step 2: Implement the Explorer COM adapter**

Use `Shell.Application` to enumerate Explorer windows, identify the foreground Explorer window, and project it into `ExplorerWindowSnapshot`.

Keep COM details isolated in the app layer; do not move COM references into the core library.

- [ ] **Step 3: Implement global hotkey registration**

Create a hidden message window plus a service around `RegisterHotKey` / `UnregisterHotKey`.

Runtime flow:
- build a chord for each enabled tool hotkey
- reject duplicates before registration
- register all valid hotkeys on startup
- unregister and re-register after settings save

- [ ] **Step 4: Wire hotkey callbacks into the launch flow**

When a hotkey fires:
- resolve Explorer context from the COM adapter
- feed the snapshot into `ExplorerTargetResolver`
- fall back to `FolderPickerService` if resolver returns `null`
- launch the matching tool

- [ ] **Step 5: Build and manually smoke-test**

Run:

```powershell
dotnet run --project .\src\AITerminalLauncher.App\AITerminalLauncher.App.csproj
```

Expected: tray app runs; pressing a configured hotkey attempts a launch or opens the folder picker when no Explorer context exists.

---

### Task 11: Build the settings window and dynamic tool editor

**Files:**
- Create: `src/AITerminalLauncher.App/Forms/SettingsForm.cs`
- Create: `src/AITerminalLauncher.App/Forms/ToolEditorForm.cs`
- Modify: `src/AITerminalLauncher.App/Tray/LauncherApplicationContext.cs`
- Modify: `src/AITerminalLauncher.App/Services/LaunchService.cs`

- [ ] **Step 1: Add a data-level failing test for tool add/edit/remove**

Extend the console test runner with a simple mutation scenario:

```csharp
var config = DefaultConfigFactory.Create();
config.Tools.Add(ToolConfig.CreateDefault("gemini", "Gemini", "gemini", "G"));
ConfigValidator.Validate(config);
AssertEx.True(config.Tools.Any(t => t.Id == "gemini"), "custom tool added");
```

Expected: this should already fail if the validator or defaults block valid custom tools incorrectly.

- [ ] **Step 2: Implement `SettingsForm` around the shared config model**

Support:
- list tools
- add tool
- edit tool
- remove tool
- toggle enabled
- toggle tray visibility
- toggle context-menu visibility
- toggle launch at login
- edit terminal preference

- [ ] **Step 3: Implement `ToolEditorForm` for one tool at a time**

Fields:
- ID
- display name
- command
- arguments
- enabled
- show in tray
- show in context menu
- hotkey enabled
- hotkey modifiers
- hotkey key

Validate before save using the shared `ConfigValidator`.

- [ ] **Step 4: Save settings and refresh runtime state immediately**

After a successful save:
- write the user config file
- refresh tray entries
- re-register hotkeys
- update the launch-at-login check state

- [ ] **Step 5: Re-run console tests and manually verify the form**

Expected: data-level tests PASS; the tray app settings window can add a new tool and persist it.

---

### Task 12: Add launch-at-login and context-menu management from the tray app

**Files:**
- Create: `src/AITerminalLauncher.App/Services/RunAtLoginService.cs`
- Create: `src/AITerminalLauncher.App/Services/ContextMenuScriptService.cs`
- Modify: `src/AITerminalLauncher.App/Tray/LauncherApplicationContext.cs`

- [ ] **Step 1: Add focused core or integration checks for command generation**

At minimum, assert that the app services build calls to:
- `install.ps1 -ConfigPath <user-config>`
- `uninstall.ps1 -ConfigPath <user-config>`

And that run-at-login points to the tray app executable with tray/minimized startup arguments if used.

- [ ] **Step 2: Implement `RunAtLoginService`**

Write per-user registry values under:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

The value should point to the desktop app executable and any `--tray` style startup argument the app supports.

- [ ] **Step 3: Implement `ContextMenuScriptService`**

This service should invoke:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "<install.ps1>" -ConfigPath "<userConfigPath>"
```

or the uninstall equivalent, and surface failures clearly.

- [ ] **Step 4: Wire the tray menu actions**

Make these menu items live:
- `Install Context Menu`
- `Remove Context Menu`
- `Launch at Login`

Update checked states and show a readable message on success or failure.

- [ ] **Step 5: Manually verify registry-backed behaviors**

Expected:
- toggling launch at login writes/removes the `Run` value
- install/remove context-menu actions call the PowerShell scripts successfully

---

### Task 13: Update documentation for the desktop-shell product shape

**Files:**
- Modify: `README.md`
- Modify: `USAGE.md`

- [ ] **Step 1: Rewrite `README.md` around the new product**

Include:
- tray-resident desktop app overview
- global hotkeys
- settings UI
- user-added CLI tools
- launch-at-login
- context-menu install/remove
- fallback folder picker
- config file location

- [ ] **Step 2: Rewrite `USAGE.md` to match the desktop workflow**

Document:
- first launch
- opening settings
- adding a new CLI
- assigning a hotkey
- using the tray menu
- rebuilding Explorer context-menu entries after config changes

- [ ] **Step 3: Add troubleshooting**

Cover:
- hotkey conflicts
- CLI command not found
- Explorer context not detected
- folder picker appears unexpectedly
- Windows Terminal missing
- context menu not refreshed

- [ ] **Step 4: Review docs against the accepted spec**

Expected: docs match the actual runtime behavior and the spec's acceptance criteria.

---

### Task 14: Final verification

**Files:**
- Modify as needed from earlier tasks only.

- [ ] **Step 1: Run the full automated test suite**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```

Expected: PowerShell backend tests PASS and the .NET console tests PASS.

- [ ] **Step 2: Build the full solution**

Run:

```powershell
dotnet build .\AITerminalLauncher.sln
```

Expected: all projects build successfully.

- [ ] **Step 3: Run syntax and parser checks for the scripts**

Run:

```powershell
powershell.exe -NoProfile -Command "$errors = $null; $files = 'launcher.ps1','install.ps1','uninstall.ps1','src\\AITerminalLauncher.psm1'; foreach ($f in $files) { $null = [System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $f), [ref]$null, [ref]$errors); if ($errors) { $errors; exit 1 } }; 'Syntax OK'"
```

Expected: `Syntax OK`.

- [ ] **Step 4: Perform manual desktop verification**

Verify:
1. Tray icon appears on launch.
2. Opening settings works.
3. Default tools are present.
4. Hotkeys launch the right tool.
5. Selected-folder priority works.
6. Current-folder fallback works.
7. Folder-picker fallback works.
8. A newly added custom CLI gets its own hotkey and tray entry.
9. Installing context menus creates entries for the enabled context-menu tools.
10. Uninstall removes only owned entries.

- [ ] **Step 5: Compare final behavior against the desktop-shell spec**

Confirm implementation matches:
- [2026-06-07-ai-terminal-launcher-desktop-shell-design.md](/F:/自用小程序/docs/superpowers/specs/2026-06-07-ai-terminal-launcher-desktop-shell-design.md:1)

Expected: all acceptance criteria are satisfied.

---

## Handoff Notes

- Execute tasks in order; later UI tasks depend on the earlier config and backend work.
- Follow `@superpowers:test-driven-development` while implementing each task, even when the test harness is manual.
- Use `@superpowers:verification-before-completion` before claiming the app works.
- Keep backend compatibility intact while moving from fixed tools to a dynamic list.
- Do not add packaging polish or an installer until the functional tray app is stable.
- Because this workspace is not a git repository, skip commit steps unless the repository is initialized later.
