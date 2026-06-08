namespace AITerminalLauncher.Core.Explorer;

public static class ExplorerHotkeyLaunchPolicy
{
    public static bool ShouldLaunch(ExplorerWindowSnapshot? foregroundExplorerSnapshot)
    {
        return foregroundExplorerSnapshot is not null;
    }
}
