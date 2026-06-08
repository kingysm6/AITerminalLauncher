using System.Drawing;
using System.Windows.Forms;
using AITerminalLauncher.App.Explorer;
using AITerminalLauncher.App.Forms;
using AITerminalLauncher.App.Hotkeys;
using AITerminalLauncher.App.Services;
using AITerminalLauncher.Core.Config;
using AITerminalLauncher.Core.Explorer;
using AITerminalLauncher.Core.Tray;

namespace AITerminalLauncher.App.Tray;

public sealed class LauncherApplicationContext : ApplicationContext
{
    private static readonly TimeSpan RecentLaunchTargetReuseWindow = TimeSpan.FromSeconds(8);

    private readonly LaunchService _launchService;
    private readonly ShellExplorerWindowProvider _explorerWindowProvider;
    private readonly GlobalHotkeyService _globalHotkeyService;
    private readonly ConfigStore _configStore;
    private readonly ContextMenuScriptService _contextMenuScriptService;
    private readonly RunAtLoginService _runAtLoginService;
    private readonly Icon _trayIcon;
    private readonly System.Windows.Forms.Timer _foregroundPollTimer;
    private readonly SingleInstanceMessageWindow _singleInstanceMessageWindow;
    private AppConfig _config;
    private ContextMenuStrip _contextMenu;
    private readonly NotifyIcon _notifyIcon;
    private string? _lastLaunchTargetPath;
    private DateTimeOffset _lastLaunchTargetCapturedAt;
    private bool _hotkeysRegistered;
    private bool _hotkeyRegistrationPaused;

    public LauncherApplicationContext(bool openSettingsOnStartup = false)
    {
        _configStore = new ConfigStore();
        _launchService = new LaunchService();
        _explorerWindowProvider = new ShellExplorerWindowProvider();
        _globalHotkeyService = new GlobalHotkeyService();
        _contextMenuScriptService = new ContextMenuScriptService();
        _runAtLoginService = new RunAtLoginService();
        _singleInstanceMessageWindow = new SingleInstanceMessageWindow();
        _config = _launchService.LoadConfig();
        _contextMenu = BuildContextMenu();
        _trayIcon = TrayIconProvider.GetTrayIcon();
        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _contextMenu,
            Icon = _trayIcon,
            Text = "AI Terminal Launcher",
            Visible = true,
        };

        _notifyIcon.DoubleClick += (_, _) => OpenSettings();
        _singleInstanceMessageWindow.ShowSettingsRequested += (_, _) => OpenSettings();

        _globalHotkeyService.HotkeyPressed += OnHotkeyPressed;
        _foregroundPollTimer = new System.Windows.Forms.Timer
        {
            Interval = 250,
        };
        _foregroundPollTimer.Tick += (_, _) => RefreshHotkeyRegistrationForForeground(showWarning: false);
        RefreshHotkeyRegistrationForForeground(showWarning: true);
        _foregroundPollTimer.Start();

        if (openSettingsOnStartup)
        {
            Application.Idle += OpenSettingsOnStartup;
        }
    }

    protected override void ExitThreadCore()
    {
        _foregroundPollTimer.Stop();
        _foregroundPollTimer.Dispose();
        _singleInstanceMessageWindow.Dispose();
        _globalHotkeyService.HotkeyPressed -= OnHotkeyPressed;
        _globalHotkeyService.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _trayIcon.Dispose();
        base.ExitThreadCore();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        var launchEntries = TrayMenuBuilder.BuildLaunchEntries(_config.Tools);

        if (launchEntries.Count == 0)
        {
            menu.Items.Add(new ToolStripMenuItem("没有已启用的托盘工具")
            {
                Enabled = false,
            });
        }
        else
        {
            foreach (var entry in launchEntries)
            {
                var item = new ToolStripMenuItem($"启动 {entry.DisplayName}");
                item.Click += (_, _) => LaunchTool(entry);
                menu.Items.Add(item);
            }
        }

        menu.Items.Add(new ToolStripSeparator());

        var settingsItem = new ToolStripMenuItem("设置");
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        var installContextMenuItem = new ToolStripMenuItem("安装右键菜单");
        installContextMenuItem.Click += (_, _) => RunContextMenuAction(isInstall: true);
        menu.Items.Add(installContextMenuItem);

        var removeContextMenuItem = new ToolStripMenuItem("移除右键菜单");
        removeContextMenuItem.Click += (_, _) => RunContextMenuAction(isInstall: false);
        menu.Items.Add(removeContextMenuItem);

        var launchAtLoginItem = new ToolStripMenuItem("开机启动")
        {
            Checked = _runAtLoginService.IsEnabled(),
            CheckOnClick = false,
        };
        launchAtLoginItem.Click += (_, _) => ToggleLaunchAtLogin();
        menu.Items.Add(launchAtLoginItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitThread();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void LaunchTool(TrayMenuEntry entry)
    {
        LaunchTool(entry.ToolId, entry.DisplayName, "从托盘启动工具");
    }

    private void OpenSettingsOnStartup(object? sender, EventArgs e)
    {
        Application.Idle -= OpenSettingsOnStartup;
        OpenSettings();
    }

    private void OnHotkeyPressed(object? sender, string toolId)
    {
        var entry = TrayMenuBuilder.BuildLaunchEntries(_config.Tools)
            .FirstOrDefault(entry => string.Equals(entry.ToolId, toolId, StringComparison.OrdinalIgnoreCase));

        LaunchToolFromHotkey(toolId, entry?.DisplayName ?? toolId);
    }

    private void LaunchToolFromHotkey(string toolId, string displayName)
    {
        try
        {
            var snapshot = _explorerWindowProvider.GetActiveSnapshot();
            if (!ExplorerHotkeyLaunchPolicy.ShouldLaunch(snapshot))
            {
                AppLogger.LogInfo($"忽略快捷键启动工具“{toolId}”：当前前台窗口不是资源管理器。");
                return;
            }

            LaunchTool(toolId, displayName, "通过快捷键启动工具", snapshot);
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"通过快捷键启动工具“{displayName}”失败。", ex);
            MessageBox.Show(
                ex.Message,
                $"启动 {displayName}",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void LaunchTool(
        string toolId,
        string displayName,
        string source,
        ExplorerWindowSnapshot? snapshot = null)
    {
        try
        {
            snapshot ??= _explorerWindowProvider.GetActiveSnapshot();
            var fallbackTargetPath = GetRecentLaunchTargetPath();
            var targetPath = _launchService.ResolveTargetPath(snapshot, fallbackTargetPath);
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return;
            }

            if (snapshot is null && string.Equals(targetPath, fallbackTargetPath, StringComparison.OrdinalIgnoreCase))
            {
                AppLogger.LogInfo($"当前前台窗口不是资源管理器，沿用上次目录“{targetPath}”启动工具“{toolId}”。");
            }

            _launchService.LaunchTool(toolId, targetPath);
            _lastLaunchTargetPath = targetPath;
            _lastLaunchTargetCapturedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"{source}“{displayName}”失败。", ex);
            MessageBox.Show(
                ex.Message,
                $"启动 {displayName}",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private string? GetRecentLaunchTargetPath()
    {
        if (string.IsNullOrWhiteSpace(_lastLaunchTargetPath))
        {
            return null;
        }

        if (DateTimeOffset.UtcNow - _lastLaunchTargetCapturedAt > RecentLaunchTargetReuseWindow)
        {
            return null;
        }

        return _lastLaunchTargetPath;
    }

    private void OpenSettings()
    {
        _hotkeyRegistrationPaused = true;
        _globalHotkeyService.UnregisterAll();
        _hotkeysRegistered = false;
        var restoreHotkeys = true;

        try
        {
            using var form = new SettingsForm(_config);
            if (form.ShowDialog() != DialogResult.OK || form.SavedConfig is null)
            {
                return;
            }

            try
            {
                _configStore.SaveToPath(ConfigPathResolver.GetUserConfigPath(), form.SavedConfig);
                _runAtLoginService.SetEnabled(form.SavedConfig.Startup.LaunchAtLogin);
                _hotkeyRegistrationPaused = false;
                RefreshRuntimeState();
                restoreHotkeys = false;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("从托盘保存设置失败。", ex);
                MessageBox.Show(
                    ex.Message,
                    "保存设置",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        finally
        {
            if (restoreHotkeys)
            {
                _hotkeyRegistrationPaused = false;
                RefreshHotkeyRegistrationForForeground(showWarning: true);
            }
        }
    }

    private void ToggleLaunchAtLogin()
    {
        try
        {
            var enabled = !_runAtLoginService.IsEnabled();
            _runAtLoginService.SetEnabled(enabled);
            _config.Startup.LaunchAtLogin = enabled;
            _configStore.SaveToPath(ConfigPathResolver.GetUserConfigPath(), _config);
            RefreshRuntimeState();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("切换开机启动失败。", ex);
            MessageBox.Show(
                ex.Message,
                "开机启动",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void RunContextMenuAction(bool isInstall)
    {
        try
        {
            var message = isInstall
                ? _contextMenuScriptService.Install()
                : _contextMenuScriptService.Remove();

            MessageBox.Show(
                message,
                isInstall ? "安装右键菜单" : "移除右键菜单",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppLogger.LogError(isInstall ? "安装右键菜单失败。" : "移除右键菜单失败。", ex);
            MessageBox.Show(
                ex.Message,
                isInstall ? "安装右键菜单" : "移除右键菜单",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void RefreshRuntimeState()
    {
        _config = _launchService.LoadConfig();

        if (_hotkeysRegistered)
        {
            _globalHotkeyService.UnregisterAll();
            _hotkeysRegistered = false;
        }

        RefreshHotkeyRegistrationForForeground(showWarning: true);

        var previousMenu = _contextMenu;
        _contextMenu = BuildContextMenu();
        _notifyIcon.ContextMenuStrip = _contextMenu;
        previousMenu.Dispose();
    }

    private void RefreshHotkeyRegistrationForForeground(bool showWarning)
    {
        if (_hotkeyRegistrationPaused)
        {
            return;
        }

        var foregroundExplorerSnapshot = _explorerWindowProvider.GetActiveSnapshot();
        if (!ExplorerHotkeyLaunchPolicy.ShouldLaunch(foregroundExplorerSnapshot))
        {
            if (_hotkeysRegistered)
            {
                _globalHotkeyService.UnregisterAll();
                _hotkeysRegistered = false;
            }

            return;
        }

        if (_hotkeysRegistered)
        {
            return;
        }

        TryRegisterHotkeys(showWarning);
    }

    private void TryRegisterHotkeys(bool showWarning)
    {
        try
        {
            _globalHotkeyService.RegisterToolHotkeys(_config.Tools);
            _hotkeysRegistered = true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("快捷键注册警告。", ex);
            if (!showWarning)
            {
                return;
            }

            MessageBox.Show(
                ex.Message,
                "快捷键注册",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
