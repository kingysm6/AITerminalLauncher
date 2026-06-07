# AI Terminal Launcher Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a lightweight Windows Explorer context-menu launcher that opens Codex, Claude, or OpenCode in any selected/current folder.

**Architecture:** Use focused PowerShell scripts plus JSON configuration. Keep reusable behavior in one module, keep entrypoint scripts thin, and verify registry commands through dry-run output before writing to the real registry.

**Tech Stack:** Windows PowerShell 5.1-compatible scripts, JSON config, per-user Windows registry entries, optional Windows Terminal (`wt.exe`) with PowerShell fallback.

---

## File Structure

- Create: `src/AITerminalLauncher.psm1` — shared functions for config loading, path validation, command resolution, terminal argument construction, and registry entry construction.
- Create: `config.json` — editable defaults for terminal and AI CLI commands.
- Create: `launcher.ps1` — validates `-Tool` and `-Path`, then starts the chosen CLI in the target folder.
- Create: `install.ps1` — writes per-user Explorer context-menu entries; supports `-DryRun`.
- Create: `uninstall.ps1` — removes only registry entries owned by this utility; supports `-DryRun`.
- Create: `tests/run-tests.ps1` — dependency-free script-level tests for config, path validation, quoting, command generation, and registry dry-run data.
- Create: `README.md` — install, uninstall, config, and troubleshooting instructions.

## Implementation Notes

- Use PowerShell parameters `-Tool` and `-Path`; do not use Unix-style `--tool`.
- Use `Test-Path -LiteralPath` and `Resolve-Path -LiteralPath` for Explorer-provided paths.
- Prefer per-user registry keys under `HKCU:\Software\Classes`.
- Registry key names must be deterministic: `AITerminalLauncher_codex`, `AITerminalLauncher_claude`, `AITerminalLauncher_opencode`.
- `install.ps1 -DryRun` and `uninstall.ps1 -DryRun` must print planned registry operations without modifying the system.
- Tests must use a local PowerShell test runner; do not require Pester or external modules.
- Do not use `powershell.exe -WorkingDirectory`; use `Start-Process -WorkingDirectory` or a Windows PowerShell 5.1-compatible equivalent.
- Do not use `as any`, `@ts-ignore`, or any equivalent type/error suppression pattern. This project is PowerShell, so the relevant equivalent is: no swallowed errors and no empty `catch {}` blocks.
- Do not commit unless the user explicitly asks.

---

### Task 1: Create configuration and shared module skeleton

**Files:**
- Create: `config.json`
- Create: `src/AITerminalLauncher.psm1`
- Create: `tests/run-tests.ps1`

- [ ] **Step 1: Write config fixture tests**

Add a dependency-free test runner that imports `src/AITerminalLauncher.psm1`, defines `Assert-Equal` and `Assert-Throws`, loads `config.json`, and verifies these values exist:

```powershell
Assert-Equal 'wt' $config.terminal 'terminal defaults to wt'
Assert-Equal 'powershell' $config.fallbackTerminal 'fallback terminal defaults to powershell'
Assert-Equal 'codex' $config.tools.codex.command 'codex command exists'
Assert-Equal 'claude' $config.tools.claude.command 'claude command exists'
Assert-Equal 'opencode' $config.tools.opencode.command 'opencode command exists'
```

- [ ] **Step 2: Run tests and verify failure**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```

Expected: FAIL because files/functions do not exist yet.

- [ ] **Step 3: Create `config.json`**

Use:

```json
{
  "terminal": "wt",
  "fallbackTerminal": "powershell",
  "tools": {
    "codex": { "command": "codex", "args": [] },
    "claude": { "command": "claude", "args": [] },
    "opencode": { "command": "opencode", "args": [] }
  }
}
```

- [ ] **Step 4: Add `Get-AITLConfig`**

Implement a PowerShell function that accepts `-ConfigPath`, reads JSON with `Get-Content -Raw`, parses with `ConvertFrom-Json`, and throws readable errors for missing or malformed config.

- [ ] **Step 5: Re-run tests**

Expected: config-loading tests PASS.

---

### Task 2: Implement path and tool validation

**Files:**
- Modify: `src/AITerminalLauncher.psm1`
- Modify: `tests/run-tests.ps1`

- [ ] **Step 1: Add failing validation tests**

Cover:

```powershell
Resolve-AITLTargetPath -Path '.'
Resolve-AITLTargetPath -Path 'Z:\definitely-missing-folder'
Get-AITLToolConfig -Config $config -Tool 'codex'
Get-AITLToolConfig -Config $config -Tool 'missing'
```

Expected behavior: valid paths resolve to a full path; invalid paths throw; valid tool returns command; unknown tool throws and lists valid tools.

- [ ] **Step 2: Run tests and verify failure**

Expected: FAIL because validation functions do not exist.

- [ ] **Step 3: Implement validation functions**

Add:

```powershell
function Resolve-AITLTargetPath { param([string]$Path) ... }
function Get-AITLToolConfig { param($Config, [string]$Tool) ... }
```

Use `Test-Path -LiteralPath` and `Resolve-Path -LiteralPath`. Do not swallow exceptions.

- [ ] **Step 4: Re-run tests**

Expected: validation tests PASS.

---

### Task 3: Build terminal command generation

**Files:**
- Modify: `src/AITerminalLauncher.psm1`
- Modify: `tests/run-tests.ps1`

- [ ] **Step 1: Add failing command-generation tests**

Verify generated launch data for:

- Windows Terminal mode: executable `wt.exe`, arguments include `-d`, target directory, `powershell.exe`, `-NoExit`, `-Command`, and the configured tool command.
- PowerShell fallback mode: executable `powershell.exe`, launch data includes a separate `WorkingDirectory` value, arguments include `-NoExit` and the configured tool command, and no argument named `-WorkingDirectory` is passed to `powershell.exe`.
- A path with spaces.
- A path with Chinese characters.

- [ ] **Step 2: Run tests and verify failure**

Expected: FAIL because command-generation function does not exist.

- [ ] **Step 3: Implement `New-AITLLaunchCommand`**

Return a structured object instead of one concatenated command string:

```powershell
[pscustomobject]@{
  FilePath = 'wt.exe'
  ArgumentList = @('-d', $TargetPath, 'powershell.exe', '-NoExit', '-Command', $ToolCommand)
  WorkingDirectory = $null
}
```

This makes quoting testable and avoids path injection from Explorer-provided paths.

- [ ] **Step 4: Re-run tests**

Expected: command-generation tests PASS.

---

### Task 4: Implement launcher entrypoint

**Files:**
- Create: `launcher.ps1`
- Modify: `tests/run-tests.ps1`

- [ ] **Step 1: Add launcher dry-run tests**

Design `launcher.ps1` to support `-DryRun`. Tests should call:

```powershell
powershell.exe -NoProfile -File .\launcher.ps1 -Tool codex -Path . -DryRun
```

Expected: prints structured launch data and exits `0` without starting a real terminal.

- [ ] **Step 2: Run test and verify failure**

Expected: FAIL because `launcher.ps1` does not exist.

- [ ] **Step 3: Implement `launcher.ps1`**

Responsibilities:

- Import `src/AITerminalLauncher.psm1`.
- Load `config.json` next to the script.
- Resolve target path.
- Resolve tool config.
- Build launch command.
- If `-DryRun`, print launch data and exit.
- Otherwise call `Start-Process -FilePath ... -ArgumentList ...`, including `-WorkingDirectory` only as a `Start-Process` parameter when the generated launch data sets it.

- [ ] **Step 4: Re-run tests and perform manual dry-run**

Expected: tests PASS and dry-run prints the selected executable and arguments.

---

### Task 5: Implement registry install/uninstall dry-run first

**Files:**
- Create: `install.ps1`
- Create: `uninstall.ps1`
- Modify: `src/AITerminalLauncher.psm1`
- Modify: `tests/run-tests.ps1`

- [ ] **Step 1: Add registry operation tests**

Test pure registry operation construction for each tool and each location:

- `HKCU:\Software\Classes\Directory\shell\AITerminalLauncher_codex`
- `HKCU:\Software\Classes\Directory\Background\shell\AITerminalLauncher_codex`
- equivalent keys for Claude and OpenCode.

Verify command values invoke:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "<launcher.ps1>" -Tool "<tool>" -Path "%V"
```

- [ ] **Step 2: Run tests and verify failure**

Expected: FAIL because registry operation functions/scripts do not exist.

- [ ] **Step 3: Implement registry operation constructors**

Add functions that return objects describing key creation/deletion and values. Keep construction separate from execution.

- [ ] **Step 4: Implement `install.ps1 -DryRun` and `uninstall.ps1 -DryRun`**

Dry-run should print operations only. Non-dry-run should use `New-Item`, `New-ItemProperty`, and `Remove-Item` with `-LiteralPath` where applicable.

- [ ] **Step 5: Re-run tests**

Expected: registry dry-run tests PASS.

---

### Task 6: Manual Windows verification

**Files:**
- No new files expected.

- [ ] **Step 1: Run launcher dry-runs**

Run:

```powershell
powershell.exe -NoProfile -File .\launcher.ps1 -Tool codex -Path . -DryRun
powershell.exe -NoProfile -File .\launcher.ps1 -Tool claude -Path . -DryRun
powershell.exe -NoProfile -File .\launcher.ps1 -Tool opencode -Path . -DryRun
```

Expected: all exit `0` and show correct launch data.

- [ ] **Step 2: Test special paths**

Create temporary folders with spaces and Chinese characters, then dry-run against them.

Expected: target path resolves correctly and is preserved as one argument.

- [ ] **Step 3: Install registry entries**

Run:

```powershell
powershell.exe -NoProfile -File .\install.ps1
```

Expected: per-user registry entries are created without admin privileges.

- [ ] **Step 4: Verify Explorer behavior**

Right-click a folder and folder background. Confirm Codex, Claude, and OpenCode entries appear and launch into the intended folder.

- [ ] **Step 5: Uninstall registry entries**

Run:

```powershell
powershell.exe -NoProfile -File .\uninstall.ps1
```

Expected: menu entries disappear after Explorer refresh/restart if needed.

---

### Task 7: Documentation and final verification

**Files:**
- Create: `README.md`

- [ ] **Step 1: Write README**

Include:

- What the tool does.
- Prerequisites.
- Install command.
- Uninstall command.
- Config editing examples.
- Dry-run commands.
- Troubleshooting for missing CLI, missing `wt.exe`, and Explorer menu refresh.

- [ ] **Step 2: Run final automated checks**

Run the local test runner:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```

Expected: all tests PASS without requiring Pester or external modules.

- [ ] **Step 3: Run PowerShell syntax checks**

Run:

```powershell
powershell.exe -NoProfile -Command "$files = 'launcher.ps1','install.ps1','uninstall.ps1','src/AITerminalLauncher.psm1'; foreach ($f in $files) { $null = [System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $f), [ref]$null, [ref]$errors); if ($errors) { $errors; exit 1 } }; 'Syntax OK'"
```

Expected: `Syntax OK`.

- [ ] **Step 4: Final review**

Confirm implementation matches `docs/superpowers/specs/2026-06-06-ai-terminal-launcher-design.md` acceptance criteria.

---

## Handoff Notes

- Implement tasks in order; later tasks depend on earlier testable functions.
- Keep registry writes behind dry-run-verifiable constructors.
- Do not install globally or write `HKLM` keys.
- Do not leave installed registry entries behind after manual verification unless the user asks to keep them.
- This workspace is currently not a git repository, so commit steps are intentionally omitted.
