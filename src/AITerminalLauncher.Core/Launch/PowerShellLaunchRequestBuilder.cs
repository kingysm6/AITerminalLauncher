namespace AITerminalLauncher.Core.Launch;

public static class PowerShellLaunchRequestBuilder
{
    public static LaunchRequest BuildLauncherRequest(
        string launcherScriptPath,
        string configPath,
        string toolId,
        string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(launcherScriptPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        var arguments = string.Join(" ",
        [
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", Quote(launcherScriptPath),
            "-Tool", Quote(toolId),
            "-Path", Quote(targetPath),
            "-ConfigPath", Quote(configPath),
        ]);

        return new LaunchRequest("powershell.exe", arguments);
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }
}
