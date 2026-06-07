namespace AITerminalLauncher.Core.Config;

public static class ConfigPathResolver
{
    private const string ApplicationDirectoryName = "AITerminalLauncher";
    private const string ConfigFileName = "config.json";

    public static string GetUserConfigPath()
    {
        var localAppDataPath = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (string.IsNullOrWhiteSpace(localAppDataPath))
        {
            localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        if (string.IsNullOrWhiteSpace(localAppDataPath))
        {
            throw new InvalidOperationException("无法解析 LocalAppData 路径。");
        }

        return Path.Combine(localAppDataPath, ApplicationDirectoryName, ConfigFileName);
    }
}
