using System.Text.Json;
using System.Diagnostics;
using AITerminalLauncher.App;
using AITerminalLauncher.App.Services;
using AITerminalLauncher.Core.Config;
using AITerminalLauncher.Core.Explorer;
using AITerminalLauncher.Core.Hotkeys;
using AITerminalLauncher.Core.Launch;
using AITerminalLauncher.Core.Tray;
using AITerminalLauncher.Core.Validation;

var expectedTools = new[]
{
    new ExpectedTool("codex", "Codex", "codex", "C"),
    new ExpectedTool("claude", "Claude", "claude", "L"),
    new ExpectedTool("opencode", "OpenCode", "opencode", "O"),
};

VerifyDefaultFactoryConfig(expectedTools);
VerifyBlankHotkeyDefaults();
VerifyCheckedInConfigJsonParity(expectedTools);
VerifyUserConfigPath();
VerifyConfigValidation();
VerifyConfigStoreSaveAndLoad(expectedTools);
VerifyLoadOrCreateUserConfig(expectedTools);
VerifyPowerShellLaunchRequestBuilder();
VerifyExplorerTargetResolver();
VerifyTrayMenuBuilderAndHotkeyConflicts();
VerifyCustomToolAddition();
VerifyLaunchServiceUsesFallbackTargetPath();
VerifyLaunchServiceStartsCustomTool();
VerifyStartupModeResolver();

Console.WriteLine("All .NET tests passed.");

static void VerifyDefaultFactoryConfig(IReadOnlyList<ExpectedTool> expectedTools)
{
    var config = DefaultConfigFactory.Create();

    VerifyExpectedConfig(config, expectedTools, "default config");
}

static void VerifyBlankHotkeyDefaults()
{
    var hotkey = new HotkeyConfig();

    AssertEx.Equal(string.Empty, hotkey.Key, "blank hotkey key");
    AssertEx.True(!hotkey.Enabled, "blank hotkey disabled");
}

static void VerifyCheckedInConfigJsonParity(IReadOnlyList<ExpectedTool> expectedTools)
{
    var configJsonPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "config.json"));
    AssertEx.True(File.Exists(configJsonPath), "config.json exists");

    var json = File.ReadAllText(configJsonPath);
    var config = AssertEx.NotNull(JsonSerializer.Deserialize<AppConfig>(json), "config.json deserializes");
    VerifyExpectedConfig(config, expectedTools, "config.json");
}

static void VerifyUserConfigPath()
{
    var originalLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
    var localAppDataRoot = Path.Combine(Path.GetTempPath(), "AITerminalLauncher.Core.Tests", Guid.NewGuid().ToString("N"));

    try
    {
        Environment.SetEnvironmentVariable("LOCALAPPDATA", localAppDataRoot);

        var path = ConfigPathResolver.GetUserConfigPath();
        var expectedPath = Path.Combine(localAppDataRoot, "AITerminalLauncher", "config.json");

        AssertEx.Equal(expectedPath, path, "user config path");
    }
    finally
    {
        Environment.SetEnvironmentVariable("LOCALAPPDATA", originalLocalAppData);
    }
}

static void VerifyConfigValidation()
{
    var config = DefaultConfigFactory.Create();

    ConfigValidator.Validate(config);

    var duplicateIds = DefaultConfigFactory.Create();
    duplicateIds.Tools[1].Id = duplicateIds.Tools[0].Id;
    AssertEx.Throws(() => ConfigValidator.Validate(duplicateIds), "duplicate tool ids fail");

    var invalidToolId = DefaultConfigFactory.Create();
    invalidToolId.Tools[0].Id = "bad_id";
    AssertEx.Throws<InvalidOperationException>(() => ConfigValidator.Validate(invalidToolId), "工具 ID 'bad_id' 包含无效字符。", "invalid tool id fails");

    var blankDisplayName = DefaultConfigFactory.Create();
    blankDisplayName.Tools[0].DisplayName = " ";
    AssertEx.Throws(() => ConfigValidator.Validate(blankDisplayName), "blank display name fails");

    var blankCommand = DefaultConfigFactory.Create();
    blankCommand.Tools[0].Command = "";
    AssertEx.Throws(() => ConfigValidator.Validate(blankCommand), "blank command fails");

    var blankEnabledHotkeyKey = DefaultConfigFactory.Create();
    blankEnabledHotkeyKey.Tools[0].Hotkey.Key = "";
    AssertEx.Throws(() => ConfigValidator.Validate(blankEnabledHotkeyKey), "blank enabled hotkey key fails");

    var invalidEnabledHotkeyKey = DefaultConfigFactory.Create();
    invalidEnabledHotkeyKey.Tools[0].Hotkey.Key = "NotARealKey";
    AssertEx.Throws<InvalidOperationException>(() => ConfigValidator.Validate(invalidEnabledHotkeyKey), "工具“codex”启用的快捷键按键“NotARealKey”不受支持。", "invalid enabled hotkey key fails");

    var unsupportedModifier = DefaultConfigFactory.Create();
    unsupportedModifier.Tools[0].Hotkey.Modifiers[0] = "Hyper";
    AssertEx.Throws<InvalidOperationException>(() => ConfigValidator.Validate(unsupportedModifier), "工具“codex”启用的快捷键包含不受支持的修饰键。", "unsupported modifier fails");

    var disabledBlankHotkey = DefaultConfigFactory.Create();
    disabledBlankHotkey.Tools[0].Hotkey.Enabled = false;
    disabledBlankHotkey.Tools[0].Hotkey.Key = "";
    disabledBlankHotkey.Tools[0].Hotkey.Modifiers.Clear();
    ConfigValidator.Validate(disabledBlankHotkey);

    var singleKeyHotkey = DefaultConfigFactory.Create();
    singleKeyHotkey.Tools[0].Hotkey.Modifiers.Clear();
    ConfigValidator.Validate(singleKeyHotkey);

    var extendedHotkeyKeys = DefaultConfigFactory.Create();
    extendedHotkeyKeys.Tools[0].Hotkey.Key = "F12";
    extendedHotkeyKeys.Tools[1].Hotkey.Key = "NUMPAD5";
    extendedHotkeyKeys.Tools[2].Hotkey.Key = "PAGEUP";
    ConfigValidator.Validate(extendedHotkeyKeys);

    var nullTools = DefaultConfigFactory.Create();
    nullTools.Tools = null!;
    AssertEx.Throws<InvalidOperationException>(() => ConfigValidator.Validate(nullTools), "配置中的工具集合不能为空。", "null tools fail cleanly");

    var nullTerminal = DefaultConfigFactory.Create();
    nullTerminal.Terminal = null!;
    AssertEx.Throws<InvalidOperationException>(() => ConfigValidator.Validate(nullTerminal), "配置中的终端设置不能为空。", "null terminal fail cleanly");

    var nullStartup = DefaultConfigFactory.Create();
    nullStartup.Startup = null!;
    AssertEx.Throws<InvalidOperationException>(() => ConfigValidator.Validate(nullStartup), "配置中的启动设置不能为空。", "null startup fail cleanly");

    var nullFallbackBehavior = DefaultConfigFactory.Create();
    nullFallbackBehavior.FallbackBehavior = null!;
    AssertEx.Throws<InvalidOperationException>(() => ConfigValidator.Validate(nullFallbackBehavior), "配置中的回退行为设置不能为空。", "null fallback behavior fail cleanly");

    var nullHotkey = DefaultConfigFactory.Create();
    nullHotkey.Tools[0].Hotkey = null!;
    AssertEx.Throws<InvalidOperationException>(() => ConfigValidator.Validate(nullHotkey), "工具“codex”的快捷键配置不能为空。", "null hotkey fail cleanly");

    var nullArgs = DefaultConfigFactory.Create();
    nullArgs.Tools[0].Args = null!;
    AssertEx.Throws<InvalidOperationException>(() => ConfigValidator.Validate(nullArgs), "工具“codex”的参数集合不能为空。", "null args fail cleanly");

    var nullHotkeyModifiers = DefaultConfigFactory.Create();
    nullHotkeyModifiers.Tools[0].Hotkey.Modifiers = null!;
    AssertEx.Throws<InvalidOperationException>(() => ConfigValidator.Validate(nullHotkeyModifiers), "工具“codex”启用的快捷键修饰键不能为空。", "null hotkey modifiers fail cleanly");

    var duplicate = DefaultConfigFactory.Create();
    duplicate.Tools[1].Hotkey.Key = duplicate.Tools[0].Hotkey.Key;
    duplicate.Tools[1].Hotkey.Modifiers = duplicate.Tools[0].Hotkey.Modifiers.ToList();

    AssertEx.Throws(() => ConfigValidator.Validate(duplicate), "duplicate hotkeys fail");
}

static void VerifyConfigStoreSaveAndLoad(IReadOnlyList<ExpectedTool> expectedTools)
{
    var store = new ConfigStore();
    var directoryPath = Path.Combine(Path.GetTempPath(), "AITerminalLauncher.Core.Tests", Guid.NewGuid().ToString("N"));
    var configPath = Path.Combine(directoryPath, "config.json");
    var expected = DefaultConfigFactory.Create();
    expected.Tools[0].Args.Add("--help");

    store.SaveToPath(configPath, expected);

    AssertEx.True(File.Exists(configPath), "config store saves file");

    var loaded = store.LoadFromPath(configPath);
    AssertEx.Equal(expected.Version, loaded.Version, "saved config version");
    AssertEx.Equal(expected.Terminal.Preferred, loaded.Terminal.Preferred, "saved config terminal preferred");
    AssertEx.Equal(expected.Terminal.Fallback, loaded.Terminal.Fallback, "saved config terminal fallback");
    AssertEx.Equal(expected.Tools.Count, loaded.Tools.Count, "saved config tool count");
    AssertEx.SequenceEqual(expectedTools.Select(t => t.Id), loaded.Tools.Select(t => t.Id), "saved config tool ids");
    AssertEx.SequenceEqual(["--help"], loaded.Tools[0].Args, "saved config preserves args");
}

static void VerifyLoadOrCreateUserConfig(IReadOnlyList<ExpectedTool> expectedTools)
{
    var originalLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
    var rootPath = Path.Combine(Path.GetTempPath(), "AITerminalLauncher.Core.Tests", Guid.NewGuid().ToString("N"));

    try
    {
        Environment.SetEnvironmentVariable("LOCALAPPDATA", rootPath);

        var store = new ConfigStore();
        var created = store.LoadOrCreateUserConfig();
        var configPath = ConfigPathResolver.GetUserConfigPath();

        AssertEx.True(File.Exists(configPath), "load or create writes user config");
        VerifyExpectedConfig(created, expectedTools, "created user config");

        created.Terminal.Preferred = "pwsh";
        store.SaveToPath(configPath, created);

        var reloaded = store.LoadOrCreateUserConfig();
        AssertEx.Equal("pwsh", reloaded.Terminal.Preferred, "reloaded user config preserves saved value");
        AssertEx.Equal("powershell", reloaded.Terminal.Fallback, "reloaded user config keeps fallback");
        AssertEx.SequenceEqual(expectedTools.Select(t => t.Id), reloaded.Tools.Select(t => t.Id), "reloaded user config keeps tool ids");
    }
    finally
    {
        Environment.SetEnvironmentVariable("LOCALAPPDATA", originalLocalAppData);

        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }
}

static void VerifyPowerShellLaunchRequestBuilder()
{
    var request = PowerShellLaunchRequestBuilder.BuildLauncherRequest(
        launcherScriptPath: @"F:\repo\launcher.ps1",
        configPath: @"C:\Users\me\AppData\Local\AITerminalLauncher\config.json",
        toolId: "codex",
        targetPath: @"C:\Work\Project");

    AssertEx.Equal("powershell.exe", request.FileName, "launch request host");
    AssertEx.True(request.Arguments.Contains("-NoProfile", StringComparison.Ordinal), "launch request includes -NoProfile");
    AssertEx.True(request.Arguments.Contains("-ExecutionPolicy Bypass", StringComparison.Ordinal), "launch request includes execution policy bypass");
    AssertEx.True(request.Arguments.Contains("-File \"F:\\repo\\launcher.ps1\"", StringComparison.Ordinal), "launch request includes launcher path");
    AssertEx.True(request.Arguments.Contains("-Tool \"codex\"", StringComparison.Ordinal), "launch request includes tool id");
    AssertEx.True(request.Arguments.Contains("-Path \"C:\\Work\\Project\"", StringComparison.Ordinal), "launch request includes target path");
    AssertEx.True(request.Arguments.Contains("-ConfigPath \"C:\\Users\\me\\AppData\\Local\\AITerminalLauncher\\config.json\"", StringComparison.Ordinal), "launch request includes config path");
}

static void VerifyExplorerTargetResolver()
{
    var selectedFolderSnapshot = new ExplorerWindowSnapshot(
        CurrentFolder: @"C:\Repo",
        SelectedItems:
        [
            new SelectedItemSnapshot(@"C:\Repo\SubFolder", IsFolder: true),
        ]);

    AssertEx.Equal(@"C:\Repo\SubFolder", ExplorerTargetResolver.Resolve(selectedFolderSnapshot), "selected folder wins");

    var selectedFileSnapshot = new ExplorerWindowSnapshot(
        CurrentFolder: @"C:\Repo",
        SelectedItems:
        [
            new SelectedItemSnapshot(@"C:\Repo\readme.md", IsFolder: false),
        ]);

    AssertEx.Equal(@"C:\Repo", ExplorerTargetResolver.Resolve(selectedFileSnapshot), "current folder wins when selection is a file");

    var currentFolderOnlySnapshot = new ExplorerWindowSnapshot(
        CurrentFolder: @"C:\Repo",
        SelectedItems: []);

    AssertEx.Equal(@"C:\Repo", ExplorerTargetResolver.Resolve(currentFolderOnlySnapshot), "current folder is fallback when no folder is selected");
    AssertEx.Equal<string?>(null, ExplorerTargetResolver.Resolve(null), "missing explorer context returns null");
}

static void VerifyTrayMenuBuilderAndHotkeyConflicts()
{
    var config = DefaultConfigFactory.Create();
    var trayEntries = TrayMenuBuilder.BuildLaunchEntries(config.Tools);

    AssertEx.Equal(3, trayEntries.Count, "default tray entry count");
    AssertEx.SequenceEqual(["codex", "claude", "opencode"], trayEntries.Select(static entry => entry.ToolId), "default tray entry tool ids");

    config.Tools[2].ShowInTrayMenu = false;
    trayEntries = TrayMenuBuilder.BuildLaunchEntries(config.Tools);
    AssertEx.Equal(2, trayEntries.Count, "tray hides tools not marked for tray visibility");

    var conflicts = HotkeyConflictDetector.FindDuplicates(DefaultConfigFactory.Create().Tools);
    AssertEx.Equal(0, conflicts.Count, "default hotkeys do not conflict");

    var duplicateConfig = DefaultConfigFactory.Create();
    duplicateConfig.Tools[1].Hotkey.Key = duplicateConfig.Tools[0].Hotkey.Key;
    duplicateConfig.Tools[1].Hotkey.Modifiers = duplicateConfig.Tools[0].Hotkey.Modifiers.ToList();
    conflicts = HotkeyConflictDetector.FindDuplicates(duplicateConfig.Tools);

    AssertEx.Equal(1, conflicts.Count, "duplicate hotkeys produce one conflict group");
    AssertEx.SequenceEqual(["claude", "codex"], conflicts[0].ToolIds.OrderBy(static toolId => toolId, StringComparer.Ordinal), "duplicate hotkey reports affected tools");

    var singleKeyConfig = DefaultConfigFactory.Create();
    singleKeyConfig.Tools[0].Hotkey.Modifiers.Clear();
    singleKeyConfig.Tools[1].Hotkey.Key = singleKeyConfig.Tools[0].Hotkey.Key;
    singleKeyConfig.Tools[1].Hotkey.Modifiers.Clear();
    conflicts = HotkeyConflictDetector.FindDuplicates(singleKeyConfig.Tools);

    AssertEx.Equal(1, conflicts.Count, "duplicate single-key hotkeys produce one conflict group");
    AssertEx.Equal("|C", conflicts[0].Chord.Signature, "single-key hotkey signature keeps an empty modifier prefix");
    AssertEx.SequenceEqual(["claude", "codex"], conflicts[0].ToolIds.OrderBy(static toolId => toolId, StringComparer.Ordinal), "duplicate single-key hotkey reports affected tools");
}

static void VerifyCustomToolAddition()
{
    var config = DefaultConfigFactory.Create();
    config.Tools.Add(ToolConfig.CreateDefault("gemini", "Gemini", "gemini", "G"));

    ConfigValidator.Validate(config);

    AssertEx.True(config.Tools.Any(static tool => tool.Id == "gemini"), "custom tool can be added");
    AssertEx.Equal(4, config.Tools.Count, "custom tool increases tool count");
}

static void VerifyLaunchServiceUsesFallbackTargetPath()
{
    var fallbackPath = Path.Combine(Path.GetTempPath(), "AITerminalLauncher.App.Tests", Guid.NewGuid().ToString("N"));

    try
    {
        Directory.CreateDirectory(fallbackPath);
        var launchService = new LaunchService();

        AssertEx.Equal(fallbackPath, launchService.ResolveTargetPath(explorerSnapshot: null, fallbackPath), "launch service reuses the previous launch target when explorer context is unavailable");
    }
    finally
    {
        if (Directory.Exists(fallbackPath))
        {
            Directory.Delete(fallbackPath, recursive: true);
        }
    }
}

static void VerifyLaunchServiceStartsCustomTool()
{
    var originalLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
    var rootPath = Path.Combine(Path.GetTempPath(), "AITerminalLauncher.App.Tests", Guid.NewGuid().ToString("N"));
    var targetPath = Path.Combine(rootPath, "target path");
    var smokeFilePath = Path.Combine(targetPath, "aitl-smoke.txt");
    var processSnapshot = CaptureProcessIds("powershell");

    try
    {
        Directory.CreateDirectory(targetPath);
        Environment.SetEnvironmentVariable("LOCALAPPDATA", rootPath);

        var config = new AppConfig
        {
            Version = 1,
            Terminal = new TerminalConfig
            {
                Preferred = "powershell",
                Fallback = "powershell",
            },
            Startup = new StartupConfig
            {
                LaunchAtLogin = false,
                StartMinimizedToTray = true,
            },
            FallbackBehavior = new FallbackBehaviorConfig
            {
                Mode = "folderPicker",
            },
            Tools =
            [
                new ToolConfig
                {
                    Id = "smoke-test",
                    DisplayName = "SmokeTest",
                    Command = "cmd.exe",
                    Args =
                    [
                        "/c",
                        "echo launched>aitl-smoke.txt",
                    ],
                    Enabled = true,
                    ShowInContextMenu = false,
                    ShowInTrayMenu = true,
                    Hotkey = new HotkeyConfig(),
                },
            ],
        };

        var store = new ConfigStore();
        var userConfigPath = ConfigPathResolver.GetUserConfigPath();
        store.SaveToPath(userConfigPath, config);

        var reloadedConfig = store.LoadOrCreateUserConfig();
        AssertEx.True(reloadedConfig.Tools.Any(static tool => tool.Id == "smoke-test"), "user config persists custom tool");

        var launchService = new LaunchService();
        var loadedByService = launchService.LoadConfig();
        AssertEx.True(loadedByService.Tools.Any(static tool => tool.Id == "smoke-test"), "launch service loads custom tool from user config");

        launchService.LaunchTool("smoke-test", targetPath);

        AssertEx.True(WaitForCondition(() => File.Exists(smokeFilePath), TimeSpan.FromSeconds(15)), "launch service starts custom tool and creates smoke file");
        TerminateNewProcesses("powershell", processSnapshot);
        var smokeContent = File.ReadAllText(smokeFilePath).Trim();
        AssertEx.Equal("launched", smokeContent, "custom tool writes the expected smoke marker");
    }
    finally
    {
        TerminateNewProcesses("powershell", processSnapshot);
        Environment.SetEnvironmentVariable("LOCALAPPDATA", originalLocalAppData);

        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }
}

static void VerifyStartupModeResolver()
{
    AssertEx.Equal(StartupMode.ShowSettings, StartupModeResolver.Resolve([]), "double-click/no-argument startup opens settings");
    AssertEx.Equal(StartupMode.ShowSettings, StartupModeResolver.Resolve(["--settings"]), "--settings opens settings");
    AssertEx.Equal(StartupMode.TrayOnly, StartupModeResolver.Resolve(["--tray"]), "--tray starts silently in tray");
    AssertEx.Equal(StartupMode.TrayOnly, StartupModeResolver.Resolve(["--settings", "--tray"]), "--tray wins over settings when both are present");
}

static HashSet<int> CaptureProcessIds(string processName)
{
    var ids = new HashSet<int>();
    foreach (var process in Process.GetProcessesByName(processName))
    {
        using (process)
        {
            ids.Add(process.Id);
        }
    }

    return ids;
}

static void TerminateNewProcesses(string processName, IReadOnlySet<int> existingProcessIds)
{
    foreach (var process in Process.GetProcessesByName(processName))
    {
        using (process)
        {
            if (existingProcessIds.Contains(process.Id))
            {
                continue;
            }

            try
            {
                if (process.HasExited)
                {
                    continue;
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
            catch
            {
            }
        }
    }
}

static bool WaitForCondition(Func<bool> predicate, TimeSpan timeout)
{
    var stopwatch = Stopwatch.StartNew();
    while (stopwatch.Elapsed < timeout)
    {
        if (predicate())
        {
            return true;
        }

        Thread.Sleep(100);
    }

    return predicate();
}

static void VerifyExpectedConfig(AppConfig config, IReadOnlyList<ExpectedTool> expectedTools, string subject)
{
    AssertEx.Equal(1, config.Version, $"{subject} version");
    AssertEx.Equal("wt", config.Terminal.Preferred, $"{subject} terminal preferred");
    AssertEx.Equal("powershell", config.Terminal.Fallback, $"{subject} terminal fallback");
    AssertEx.Equal(false, config.Startup.LaunchAtLogin, $"{subject} startup launchAtLogin");
    AssertEx.Equal(true, config.Startup.StartMinimizedToTray, $"{subject} startup startMinimizedToTray");
    AssertEx.Equal("folderPicker", config.FallbackBehavior.Mode, $"{subject} fallbackBehavior mode");
    AssertEx.Equal(expectedTools.Count, config.Tools.Count, $"{subject} tool count");
    AssertEx.SequenceEqual(expectedTools.Select(t => t.Id), config.Tools.Select(t => t.Id), $"{subject} tool ids");

    for (var i = 0; i < expectedTools.Count; i++)
    {
        VerifyExpectedTool(config.Tools[i], expectedTools[i], $"{subject} tool[{i}]");
    }
}

static void VerifyExpectedTool(ToolConfig tool, ExpectedTool expected, string subject)
{
    AssertEx.Equal(expected.Id, tool.Id, $"{subject} id");
    AssertEx.Equal(expected.DisplayName, tool.DisplayName, $"{subject} displayName");
    AssertEx.Equal(expected.Command, tool.Command, $"{subject} command");
    AssertEx.Empty(tool.Args, $"{subject} args");
    AssertEx.Equal(true, tool.Enabled, $"{subject} enabled");
    AssertEx.Equal(true, tool.ShowInContextMenu, $"{subject} showInContextMenu");
    AssertEx.Equal(true, tool.ShowInTrayMenu, $"{subject} showInTrayMenu");
    AssertEx.Equal(true, tool.Hotkey.Enabled, $"{subject} hotkey enabled");
    AssertEx.SequenceEqual(["Control", "Alt"], tool.Hotkey.Modifiers, $"{subject} hotkey modifiers");
    AssertEx.Equal(expected.HotkeyKey, tool.Hotkey.Key, $"{subject} hotkey key");
}

static class AssertEx
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"{message}: expected '{expected}', got '{actual}'");
        }
    }

    public static T NotNull<T>(T? value, string message) where T : class
    {
        if (value is null)
        {
            throw new Exception(message);
        }

        return value;
    }

    public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new Exception($"{message}: expected '{string.Join(", ", expected)}', got '{string.Join(", ", actual)}'");
        }
    }

    public static void Empty<T>(IEnumerable<T> values, string message)
    {
        if (values.Any())
        {
            throw new Exception($"{message}: expected empty, got '{string.Join(", ", values)}'");
        }
    }

    public static void Throws(Action action, string message)
    {
        try
        {
            action();
        }
        catch
        {
            return;
        }

        throw new Exception($"{message}: expected exception");
    }

    public static void Throws<TException>(Action action, string expectedMessage, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            if (ex is not TException)
            {
                throw new Exception($"{message}: expected exception type '{typeof(TException).Name}', got '{ex.GetType().Name}'");
            }

            if (!ex.Message.Contains(expectedMessage, StringComparison.Ordinal))
            {
                throw new Exception($"{message}: expected message containing '{expectedMessage}', got '{ex.Message}'");
            }

            return;
        }

        throw new Exception($"{message}: expected exception");
    }
}

sealed record ExpectedTool(string Id, string DisplayName, string Command, string HotkeyKey);
