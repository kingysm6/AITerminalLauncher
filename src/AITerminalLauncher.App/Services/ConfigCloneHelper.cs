using System.Text.Json;
using AITerminalLauncher.Core.Config;

namespace AITerminalLauncher.App.Services;

internal static class ConfigCloneHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public static AppConfig Clone(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var json = JsonSerializer.Serialize(config, SerializerOptions);
        return JsonSerializer.Deserialize<AppConfig>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Failed to clone app config.");
    }

    public static ToolConfig Clone(ToolConfig tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        var json = JsonSerializer.Serialize(tool, SerializerOptions);
        return JsonSerializer.Deserialize<ToolConfig>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Failed to clone tool config.");
    }
}
