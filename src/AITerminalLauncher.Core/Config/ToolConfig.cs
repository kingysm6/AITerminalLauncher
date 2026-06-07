using System.Text.Json.Serialization;

namespace AITerminalLauncher.Core.Config;

public sealed class ToolConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public List<string> Args { get; set; } = new();

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("showInContextMenu")]
    public bool ShowInContextMenu { get; set; } = true;

    [JsonPropertyName("showInTrayMenu")]
    public bool ShowInTrayMenu { get; set; } = true;

    [JsonPropertyName("hotkey")]
    public HotkeyConfig Hotkey { get; set; } = new();

    public static ToolConfig CreateDefault(string id, string displayName, string command, string hotkeyKey)
    {
        return new ToolConfig
        {
            Id = id,
            DisplayName = displayName,
            Command = command,
            Hotkey = HotkeyConfig.CreateDefault(hotkeyKey),
        };
    }
}
