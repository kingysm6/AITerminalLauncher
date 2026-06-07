# AI Terminal Launcher

AI Terminal Launcher 是一个 Windows 托盘工具，用来从资源管理器、托盘菜单或全局快捷键快速启动 AI CLI 工具。

它适合把 Codex、Claude、OpenCode 等命令行工具固定成可配置的启动入口，并自动把当前资源管理器目录作为终端工作目录。

## 功能

- 托盘常驻，双击打开设置窗口。
- 支持自定义 CLI 工具：名称、命令、参数、显示位置。
- 支持全局快捷键，可以直接捕获用户按下的键。
- 支持单键快捷键，也支持 Ctrl / Alt / Shift / Win 组合键。
- 支持启动 Codex、Claude、OpenCode 等任意 PATH 中的命令。
- 支持资源管理器右键菜单安装和移除。
- 支持开机启动。
- 支持 Windows Terminal (`wt`) 和 PowerShell 回退。
- 当连续按多个快捷键时，会短时间复用上一次识别到的目录，避免新终端抢焦点后后续快捷键失效。

## 项目结构

```text
.
├── src/
│   ├── AITerminalLauncher.App/      # WinForms 托盘程序和设置界面
│   ├── AITerminalLauncher.Core/     # 配置、快捷键、启动请求等核心逻辑
│   └── AITerminalLauncher.psm1      # PowerShell 模块
├── tests/
│   ├── AITerminalLauncher.Core.Tests/
│   └── run-tests.ps1
├── install.ps1                      # 安装资源管理器右键菜单
├── uninstall.ps1                    # 移除资源管理器右键菜单
├── launcher.ps1                     # CLI 启动脚本
├── publish.ps1                      # 单文件发布脚本
└── config.json                      # 默认配置模板
```

## 运行要求

- Windows
- Windows PowerShell 5.1 或更高版本
- 从源码运行需要 .NET 8 SDK
- 使用发布后的单文件版本不需要目标机器预装 .NET
- 目标 CLI 命令需要加入 `PATH`，或者在设置中填写完整可执行文件路径

## 构建

```powershell
dotnet build .\AITerminalLauncher.sln
```

## 测试

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```

测试覆盖：

- 配置读写和验证
- 快捷键冲突检测
- 启动命令生成
- 资源管理器目标路径解析
- 右键菜单脚本 dry-run
- UI 源码回归检查

## 发布

生成 Windows x64 自包含单文件程序：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\publish.ps1
```

发布产物位于：

```text
dist\AITerminalLauncher\
```

主要文件：

- `AITerminalLauncher.App.exe`
- `launcher.ps1`
- `install.ps1`
- `uninstall.ps1`
- `src\AITerminalLauncher.psm1`

这些文件需要保持同一目录布局一起分发。

## 使用

启动设置窗口：

```powershell
.\dist\AITerminalLauncher\AITerminalLauncher.App.exe
```

只启动托盘：

```powershell
.\dist\AITerminalLauncher\AITerminalLauncher.App.exe --tray
```

常用入口：

- 双击托盘图标打开设置。
- 右键托盘图标打开菜单。
- 在设置中添加、编辑、启用或停用工具。
- 在设置中为每个工具配置快捷键。

## 配置文件

用户配置保存位置：

```text
%LocalAppData%\AITerminalLauncher\config.json
```

仓库根目录的 `config.json` 是默认配置模板。

## 快捷键

快捷键编辑方式：

1. 打开设置。
2. 选择工具并点击编辑。
3. 启用快捷键。
4. 点击“按键输入”控件。
5. 直接按下目标键或组合键。

支持的按键包括：

- `A-Z`
- `0-9`
- `F1-F24`
- `NUMPAD0-NUMPAD9`
- 方向键
- `Home` / `End`
- `PageUp` / `PageDown`
- `Insert` / `Delete`
- `Space` / `Tab` / `Esc` / `Enter` / `Backspace`
- 常用符号键

## 资源管理器右键菜单

安装右键菜单：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json"
```

预览将写入的注册表操作：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json" -DryRun
```

移除右键菜单：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\uninstall.ps1 -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json"
```

托盘菜单中也提供“安装右键菜单”和“移除右键菜单”。

## 日志

日志目录：

```text
%LocalAppData%\AITerminalLauncher\logs\
```

如果遇到启动失败、快捷键注册失败、工具启动失败，优先查看这里。

## 常见问题

### 快捷键不能注册

可能被其他程序占用。修改快捷键后保存设置，或者查看日志确认具体失败项。

### 按快捷键时目录不对

程序优先读取当前前台资源管理器窗口：

1. 如果选中了文件夹，使用选中文件夹。
2. 如果选中了文件，使用当前资源管理器目录。
3. 如果当前前台不是资源管理器，短时间内会复用上一次目录，方便连续按多个快捷键。
4. 如果没有可用目录，会弹出文件夹选择器。

### CLI 命令找不到

检查命令是否在 `PATH` 中，或者在设置里填写完整可执行文件路径。

### Windows Terminal 不可用

在设置里把首选终端改成 `powershell`，或者安装 Windows Terminal 后继续使用 `wt`。
