using AITerminalLauncher.Core.Config;

namespace AITerminalLauncher.Core.Tray;

public static class TrayMenuBuilder
{
    public static List<TrayMenuEntry> BuildLaunchEntries(IEnumerable<ToolConfig> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        return tools
            .Where(static tool => tool.Enabled && tool.ShowInTrayMenu)
            .Select(static tool => new TrayMenuEntry(tool.Id, tool.DisplayName))
            .ToList();
    }
}
