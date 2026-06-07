namespace AITerminalLauncher.Core.Config;

public static class DefaultConfigFactory
{
    public static AppConfig Create()
    {
        return new AppConfig
        {
            Tools =
            [
                ToolConfig.CreateDefault("codex", "Codex", "codex", "C"),
                ToolConfig.CreateDefault("claude", "Claude", "claude", "L"),
                ToolConfig.CreateDefault("opencode", "OpenCode", "opencode", "O"),
            ],
        };
    }
}
