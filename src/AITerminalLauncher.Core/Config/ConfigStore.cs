using System.Text.Json;
using AITerminalLauncher.Core.Validation;

namespace AITerminalLauncher.Core.Config;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public AppConfig LoadFromPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AppConfig>(json, SerializerOptions)
            ?? throw new InvalidOperationException($"无法反序列化配置文件“{path}”。");

        ConfigValidator.Validate(config);
        return config;
    }

    public void SaveToPath(string path, AppConfig config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(config);

        ConfigValidator.Validate(config);

        var directoryPath = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException($"配置路径“{path}”不包含父目录。");
        }

        Directory.CreateDirectory(directoryPath);

        var json = JsonSerializer.Serialize(config, SerializerOptions);
        File.WriteAllText(path, json);
    }

    public AppConfig LoadOrCreateUserConfig()
    {
        var path = ConfigPathResolver.GetUserConfigPath();
        if (File.Exists(path))
        {
            return LoadFromPath(path);
        }

        var config = DefaultConfigFactory.Create();
        SaveToPath(path, config);
        return config;
    }
}
