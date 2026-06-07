using AITerminalLauncher.Core.Config;

namespace AITerminalLauncher.Core.Hotkeys;

public static class HotkeyConflictDetector
{
    public static List<DuplicateHotkey> FindDuplicates(IEnumerable<ToolConfig> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        return tools
            .Where(static tool => tool.Enabled)
            .Select(static tool => new
            {
                ToolId = tool.Id,
                Chord = HotkeyChord.FromConfig(tool.Hotkey),
            })
            .Where(static item => item.Chord is not null)
            .GroupBy(static item => item.Chord!.Signature, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .Select(static group => new DuplicateHotkey(
                group.First().Chord!,
                group.Select(static item => item.ToolId)
                    .OrderBy(static toolId => toolId, StringComparer.Ordinal)
                    .ToArray()))
            .ToList();
    }

    public sealed record DuplicateHotkey(HotkeyChord Chord, IReadOnlyList<string> ToolIds);
}
