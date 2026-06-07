using Microsoft.Win32;

namespace AITerminalLauncher.App.Services;

public sealed class RunAtLoginService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AITerminalLauncher";

    private readonly string _applicationPath;

    public RunAtLoginService(string? applicationPath = null)
    {
        _applicationPath = applicationPath ?? Application.ExecutablePath;
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(ValueName) as string;
        return string.Equals(value, BuildCommand(), StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException($"无法打开注册表项“{RunKeyPath}”。");

        if (enabled)
        {
            key.SetValue(ValueName, BuildCommand(), RegistryValueKind.String);
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private string BuildCommand()
    {
        return $"\"{_applicationPath}\" --tray";
    }
}
