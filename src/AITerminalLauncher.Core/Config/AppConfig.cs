using System.Text.Json.Serialization;

namespace AITerminalLauncher.Core.Config;

public sealed class AppConfig
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("terminal")]
    public TerminalConfig Terminal { get; set; } = new();

    [JsonPropertyName("startup")]
    public StartupConfig Startup { get; set; } = new();

    [JsonPropertyName("fallbackBehavior")]
    public FallbackBehaviorConfig FallbackBehavior { get; set; } = new();

    [JsonPropertyName("tools")]
    public List<ToolConfig> Tools { get; set; } = new();
}

public sealed class TerminalConfig
{
    [JsonPropertyName("preferred")]
    public string Preferred { get; set; } = "wt";

    [JsonPropertyName("fallback")]
    public string Fallback { get; set; } = "powershell";
}

public sealed class StartupConfig
{
    [JsonPropertyName("launchAtLogin")]
    public bool LaunchAtLogin { get; set; }

    [JsonPropertyName("startMinimizedToTray")]
    public bool StartMinimizedToTray { get; set; } = true;
}

public sealed class FallbackBehaviorConfig
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "folderPicker";
}
