using System.Diagnostics;
using AITerminalLauncher.Core.Config;

namespace AITerminalLauncher.App.Services;

public sealed class ContextMenuScriptService
{
    private readonly string _installScriptPath;
    private readonly string _uninstallScriptPath;

    public ContextMenuScriptService()
    {
        _installScriptPath = ScriptPathResolver.ResolveRequiredScriptPath("install.ps1");
        _uninstallScriptPath = ScriptPathResolver.ResolveRequiredScriptPath("uninstall.ps1");
    }

    public string Install()
    {
        return InvokeScript(_installScriptPath);
    }

    public string Remove()
    {
        return InvokeScript(_uninstallScriptPath);
    }

    private static string InvokeScript(string scriptPath)
    {
        var configPath = ConfigPathResolver.GetUserConfigPath();
        var arguments = string.Join(" ",
        [
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", Quote(scriptPath),
            "-ConfigPath", Quote(configPath),
        ]);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("powershell.exe", arguments)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            },
        };

        _ = process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var combinedOutput = string.Join(
            Environment.NewLine,
            new[] { standardOutput.Trim(), standardError.Trim() }
                .Where(static text => !string.IsNullOrWhiteSpace(text)));

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"脚本“{Path.GetFileName(scriptPath)}”执行失败，退出代码 {process.ExitCode}。{Environment.NewLine}{combinedOutput}".Trim());
        }

        return string.IsNullOrWhiteSpace(combinedOutput)
            ? $"脚本“{Path.GetFileName(scriptPath)}”执行成功。"
            : combinedOutput;
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }
}
