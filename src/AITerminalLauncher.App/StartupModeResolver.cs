namespace AITerminalLauncher.App;

public enum StartupMode
{
    ShowSettings,
    TrayOnly,
}

public static class StartupModeResolver
{
    public static StartupMode Resolve(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var argumentList = arguments.ToList();
        return argumentList.Any(static argument => string.Equals(argument, "--tray", StringComparison.OrdinalIgnoreCase))
            ? StartupMode.TrayOnly
            : StartupMode.ShowSettings;
    }
}
