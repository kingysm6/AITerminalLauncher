using System.Text.Json.Serialization;

namespace AITerminalLauncher.Core.Config;

public sealed class HotkeyConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = ["Control", "Alt"];

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    public static HotkeyConfig CreateDefault(string key)
    {
        return new HotkeyConfig
        {
            Enabled = true,
            Key = key,
        };
    }
}
