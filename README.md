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
|-- src/
|   |-- AITerminalLauncher.App/      # WinForms tray application
|   |-- AITerminalLauncher.Core/     # Configuration, validation, launch, hotkey, tray logic
|   `-- AITerminalLauncher.psm1      # PowerShell backend module
|-- tests/
|   |-- AITerminalLauncher.Core.Tests/
|   `-- run-tests.ps1
|-- install.ps1                      # Install Explorer context menu
|-- uninstall.ps1                    # Remove Explorer context menu
|-- launcher.ps1                     # Tool launch script
|-- publish.ps1                      # Self-contained publish script
`-- config.json                      # Default config template
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

---

# AI Terminal Launcher 中文说明

AI Terminal Launcher 是一个 Windows 托盘程序，用来从资源管理器目录、托盘菜单或全局快捷键快速启动 AI 命令行工具。

它适合把 Codex、Claude、OpenCode 等 CLI 工具配置成固定入口。程序会识别当前资源管理器目录，在该目录打开终端，并运行对应命令。

## 主要功能

- Windows 托盘常驻，双击托盘图标打开设置窗口。
- 可配置多个 CLI 工具，包括显示名称、命令、参数、快捷键和显示位置。
- 支持全局快捷键，包括单键快捷键和组合键。
- 自定义快捷键按键支持直接捕获用户输入，不再只能从下拉框选择。
- 支持资源管理器目录识别：
  - 选中文件夹时优先使用选中的文件夹；
  - 选中文件时使用当前资源管理器目录；
  - 没有资源管理器上下文时弹出文件夹选择器。
- 连续按多个快捷键时，会短时间复用上次目录，避免新终端抢焦点后下一个快捷键失效。
- 支持托盘菜单入口。
- 支持安装和移除资源管理器右键菜单。
- 支持开机启动。
- 支持选择首选终端，并在不可用时回退到 PowerShell。
- 支持发布为 Windows 自包含单文件程序。

## 目录结构

```text
.
|-- src/
|   |-- AITerminalLauncher.App/      # WinForms 托盘程序和设置界面
|   |-- AITerminalLauncher.Core/     # 配置、验证、启动、快捷键、托盘逻辑
|   `-- AITerminalLauncher.psm1      # PowerShell 后端模块
|-- tests/
|   |-- AITerminalLauncher.Core.Tests/
|   `-- run-tests.ps1
|-- install.ps1                      # 安装资源管理器右键菜单
|-- uninstall.ps1                    # 移除资源管理器右键菜单
|-- launcher.ps1                     # 工具启动脚本
|-- publish.ps1                      # 自包含发布脚本
`-- config.json                      # 默认配置模板
```

## 运行要求

- Windows
- Windows PowerShell 5.1 或更高版本
- 从源码构建需要 .NET 8 SDK
- 目标 CLI 命令需要在 `PATH` 中，或者在设置里填写完整可执行文件路径
- 如果首选终端选择 `wt`，需要安装 Windows Terminal

发布后的自包含 exe 不要求目标机器预装 .NET 运行时。

## 构建

```powershell
dotnet build .\AITerminalLauncher.sln
```

## 测试

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```

## 发布

发布 Windows x64 自包含包：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\publish.ps1
```

发布目录：

```text
dist\AITerminalLauncher\
```

分发时需要保留整个目录结构，包括：

- `AITerminalLauncher.App.exe`
- `launcher.ps1`
- `install.ps1`
- `uninstall.ps1`
- `src\AITerminalLauncher.psm1`

## 使用方式

打开设置窗口：

```powershell
.\dist\AITerminalLauncher\AITerminalLauncher.App.exe
```

只启动托盘：

```powershell
.\dist\AITerminalLauncher\AITerminalLauncher.App.exe --tray
```

开发时运行：

```powershell
dotnet run --project .\src\AITerminalLauncher.App\AITerminalLauncher.App.csproj
```

## 配置文件

用户配置文件位置：

```text
%LocalAppData%\AITerminalLauncher\config.json
```

仓库根目录的 `config.json` 是默认配置模板。

## 快捷键

每个启用的工具都可以设置一个全局快捷键。

支持的按键包括：

- `A-Z`
- `0-9`
- `F1-F24`
- `NUMPAD0-NUMPAD9`
- 方向键
- `Home`、`End`
- `PageUp`、`PageDown`
- `Insert`、`Delete`
- `Space`、`Tab`、`Esc`、`Enter`、`Backspace`
- 常用符号键

编辑工具时，点击“按键输入”控件，然后直接按目标键或组合键。例如按下 `Ctrl+C`，会捕获 `C` 并自动勾选 `Ctrl`。

## 资源管理器右键菜单

安装右键菜单：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json"
```

预览注册表改动：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json" -DryRun
```

移除右键菜单：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\uninstall.ps1 -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json"
```

托盘菜单里也提供安装和移除右键菜单的入口。

## 日志

日志目录：

```text
%LocalAppData%\AITerminalLauncher\logs\
```

如果启动失败、快捷键注册失败或工具启动失败，可以先查看这里。

## 常见问题

### 快捷键注册失败

可能是快捷键已经被其他程序占用。修改快捷键后保存设置即可。

### 启动目录不对

程序选择目录的优先级如下：

1. 当前资源管理器窗口中选中的文件夹；
2. 当前资源管理器窗口所在目录；
3. 连续快捷键启动时短时间复用上一次目录；
4. 弹出文件夹选择器。

如果资源管理器不在前台，并且短时间复用窗口已经过期，程序不会一直使用旧目录。

### CLI 命令找不到

把命令加入 `PATH`，或者在设置里填写完整可执行文件路径。

### Windows Terminal 不可用

把首选终端改成 `powershell`，或者安装 Windows Terminal 后继续使用 `wt`。
