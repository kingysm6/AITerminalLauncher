using System.Text;
using AITerminalLauncher.Core.Config;

namespace AITerminalLauncher.App.Services;

public static class AppLogger
{
    public static void LogInfo(string message)
    {
        WriteEntry("INFO", message, exception: null);
    }

    public static void LogError(string message, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        WriteEntry("ERROR", message, exception);
    }

    public static string GetLogDirectoryPath()
    {
        var configPath = ConfigPathResolver.GetUserConfigPath();
        var configDirectory = Path.GetDirectoryName(configPath)
            ?? throw new InvalidOperationException($"无法根据配置路径“{configPath}”解析日志目录。");

        return Path.Combine(configDirectory, "logs");
    }

    private static void WriteEntry(string level, string message, Exception? exception)
    {
        try
        {
            var logDirectoryPath = GetLogDirectoryPath();
            Directory.CreateDirectory(logDirectoryPath);

            var logFilePath = Path.Combine(logDirectoryPath, $"{DateTime.UtcNow:yyyy-MM-dd}.log");
            var builder = new StringBuilder();
            builder.Append('[')
                .Append(DateTime.UtcNow.ToString("O"))
                .Append("] ")
                .Append(level)
                .Append(' ')
                .Append(message)
                .AppendLine();

            if (exception is not null)
            {
                builder.AppendLine(exception.ToString());
            }

            File.AppendAllText(logFilePath, builder.ToString(), Encoding.UTF8);
        }
        catch
        {
        }
    }
}
