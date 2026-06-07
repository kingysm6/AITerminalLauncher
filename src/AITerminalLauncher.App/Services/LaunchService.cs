using System.Diagnostics;
using System.Windows.Forms;
using AITerminalLauncher.App.Dialogs;
using AITerminalLauncher.Core.Config;
using AITerminalLauncher.Core.Explorer;
using AITerminalLauncher.Core.Launch;

namespace AITerminalLauncher.App.Services;

public sealed class LaunchService
{
    private readonly ConfigStore _configStore;
    private readonly FolderPickerService _folderPickerService;
    private readonly string _launcherScriptPath;

    public LaunchService(ConfigStore? configStore = null, FolderPickerService? folderPickerService = null)
    {
        _configStore = configStore ?? new ConfigStore();
        _folderPickerService = folderPickerService ?? new FolderPickerService();
        _launcherScriptPath = ScriptPathResolver.ResolveRequiredScriptPath("launcher.ps1");
    }

    public AppConfig LoadConfig()
    {
        return _configStore.LoadOrCreateUserConfig();
    }

    public bool LaunchTool(string toolId, IWin32Window? owner = null)
    {
        return LaunchTool(toolId, explorerSnapshot: null, owner);
    }

    public bool LaunchTool(string toolId, ExplorerWindowSnapshot? explorerSnapshot, IWin32Window? owner = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolId);

        var targetPath = ResolveTargetPath(explorerSnapshot, fallbackTargetPath: null, owner);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return false;
        }

        LaunchTool(toolId, targetPath);
        return true;
    }

    public string? ResolveTargetPath(ExplorerWindowSnapshot? explorerSnapshot, string? fallbackTargetPath = null, IWin32Window? owner = null)
    {
        var explorerTarget = ExplorerTargetResolver.Resolve(explorerSnapshot);
        if (!string.IsNullOrWhiteSpace(explorerTarget))
        {
            return explorerTarget;
        }

        if (!string.IsNullOrWhiteSpace(fallbackTargetPath) && Directory.Exists(fallbackTargetPath))
        {
            return fallbackTargetPath;
        }

        return _folderPickerService.PickFolder(owner);
    }

    public void LaunchTool(string toolId, string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        _ = _configStore.LoadOrCreateUserConfig();

        var configPath = ConfigPathResolver.GetUserConfigPath();
        var request = PowerShellLaunchRequestBuilder.BuildLauncherRequest(
            launcherScriptPath: _launcherScriptPath,
            configPath: configPath,
            toolId: toolId,
            targetPath: targetPath);

        var startInfo = new ProcessStartInfo(request.FileName, request.Arguments)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(_launcherScriptPath),
        };

        AppLogger.LogInfo($"正在为目标“{targetPath}”启动工具“{toolId}”。");

        _ = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"启动工具“{toolId}”失败。");
    }
}
