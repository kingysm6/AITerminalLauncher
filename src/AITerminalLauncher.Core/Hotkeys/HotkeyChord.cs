using AITerminalLauncher.Core.Config;

namespace AITerminalLauncher.Core.Hotkeys;

public sealed record HotkeyChord(string Key, IReadOnlyList<string> Modifiers)
{
    public string Signature => string.Join("+", Modifiers) + "|" + Key;

    public static HotkeyChord? FromConfig(HotkeyConfig? hotkey)
    {
        if (hotkey is null || !hotkey.Enabled || string.IsNullOrWhiteSpace(hotkey.Key) || hotkey.Modifiers is null)
        {
            return null;
        }

        var modifiers = hotkey.Modifiers
            .Select(static modifier => modifier.Trim())
            .Where(static modifier => !string.IsNullOrWhiteSpace(modifier))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static modifier => modifier, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new HotkeyChord(hotkey.Key.Trim().ToUpperInvariant(), modifiers);
    }
}
