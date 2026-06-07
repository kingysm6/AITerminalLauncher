# AI Terminal Launcher 使用说明

## 首次启动

如有需要，先构建解决方案：

```powershell
dotnet build .\AITerminalLauncher.sln
```

启动托盘程序：

```powershell
dotnet run --project .\src\AITerminalLauncher.App\AITerminalLauncher.App.csproj
```

启动后：

- 程序会常驻系统托盘
- 双击托盘图标可打开设置
- 右键托盘图标可打开托盘菜单

## 托盘菜单

托盘菜单包含：

- 每个已启用且允许显示在托盘里的工具，都会有一项“启动 <工具名>”
- `设置`
- `安装右键菜单`
- `移除右键菜单`
- `开机启动`
- `退出`

如果当前没有可显示在托盘中的工具，会显示“没有已启用的托盘工具”。

## 设置窗口

你可以通过以下方式打开设置：

- 双击托盘图标
- 在托盘菜单中点击“设置”
- 使用 `--settings` 启动程序

设置窗口可以：

- 查看所有已配置工具
- 添加新工具
- 编辑现有工具
- 删除工具
- 切换启用状态
- 切换托盘显示状态
- 切换右键菜单显示状态
- 修改首选终端和备用终端
- 控制开机启动

## 添加新的 CLI 工具

1. 打开“设置”。
2. 点击“添加工具”。
3. 填写以下字段：
   - `ID`
   - `显示名称`
   - `命令`
   - `参数`
   - 显示位置
   - 快捷键设置
4. 在工具编辑窗口点击“保存”。
5. 在设置窗口点击“保存”。

注意：

- `ID` 只能包含小写字母、数字或 `-`
- 不允许重复的工具 ID
- 不允许重复的已启用快捷键

## 设置快捷键

在工具编辑窗口中：

1. 勾选“启用快捷键”
2. 选择一个按键
3. 选择一个或多个修饰键
4. 保存工具
5. 保存设置

保存后，托盘程序会立即重新注册快捷键。

快捷键启动目录的优先级：

1. 当前激活资源管理器中被选中的文件夹
2. 当前激活资源管理器所在目录
3. 文件夹选择器

## 安装资源管理器右键菜单

通过托盘菜单：

- 点击“安装右键菜单”

或通过 PowerShell：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json"
```

如果想先看将要写入的注册表操作，可以加 `-DryRun`。

只有同时满足以下条件的工具才会生成右键菜单项：

- `enabled = true`
- `showInContextMenu = true`

如果你在设置里改了这些选项，改完后请重新安装或刷新右键菜单。

## 移除资源管理器右键菜单

通过托盘菜单：

- 点击“移除右键菜单”

或通过 PowerShell：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\uninstall.ps1 -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json"
```

## 开机启动

可以通过以下位置切换：

- 托盘菜单里的“开机启动”
- 设置窗口中的“开机启动”

程序会在这里写入当前用户的自启动项：

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

注册的命令会以 `--tray` 参数启动桌面程序。

## 不真正打开工具时测试启动后端

可以使用 dry-run：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\launcher.ps1 -Tool codex -Path . -ConfigPath "$env:LOCALAPPDATA\AITerminalLauncher\config.json" -DryRun
```

它会输出结构化 JSON，包括：

- `FilePath`
- `ArgumentList`
- `WorkingDirectory`

## 故障排查

### 快捷键注册警告

- 大概率是快捷键已被其他程序占用。
- 打开设置，换一个快捷键后重新保存。
- 查看 `%LocalAppData%\AITerminalLauncher\logs\` 下的日志。

### CLI 没有启动

- 确认设置里的命令是否正确。
- 必要时使用完整可执行文件路径。
- 可以先用 `launcher.ps1 -DryRun` 验证启动后端。

### 资源管理器右键菜单缺失

- 再执行一次“安装右键菜单”。
- 如果菜单没有立刻刷新，可以重启 Explorer。

### 明明想使用资源管理器上下文，却弹出了文件夹选择器

- 确保目标资源管理器窗口处于激活状态。
- 选中的文件夹优先于选中的文件。
- 如果当前窗口不是可用的资源管理器文件夹窗口，回退到文件夹选择器是预期行为。

### Windows Terminal 不可用

- 在设置中把首选终端改成 `powershell`。

### 设置保存失败

- 检查是否有重复工具 ID 或重复快捷键。
- 查看错误弹窗和程序日志。

## 发布单文件版本

如果想把程序分发到没有安装 .NET 运行时的电脑，可以打包成自包含单文件：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\publish.ps1
```

完成后，`dist\AITerminalLauncher\` 下会得到一个 `AITerminalLauncher.App.exe`，连同 `launcher.ps1`、`install.ps1`、`uninstall.ps1` 和 `src\AITerminalLauncher.psm1`。把整个 `AITerminalLauncher` 文件夹复制到目标电脑，双击 `AITerminalLauncher.App.exe` 即可启动托盘程序。

注意：必须保留文件夹内的脚本和 `src` 子目录，不要只复制 exe，否则启动工具时会找不到 PowerShell 后端。

## 完整验证

运行自动化验证：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```
