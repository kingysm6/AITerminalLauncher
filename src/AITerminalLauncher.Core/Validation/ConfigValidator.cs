using AITerminalLauncher.Core.Config;

namespace AITerminalLauncher.Core.Validation;

public static class ConfigValidator
{
    private static readonly HashSet<string> AllowedHotkeyKeys =
    [
        .. Enumerable.Range('A', 26).Select(static key => ((char)key).ToString()),
        .. Enumerable.Range(0, 10).Select(static key => key.ToString()),
        .. Enumerable.Range(1, 24).Select(static key => $"F{key}"),
        .. Enumerable.Range(0, 10).Select(static key => $"NUMPAD{key}"),
        "SPACE",
        "TAB",
        "ESC",
        "ENTER",
        "BACKSPACE",
        "DELETE",
        "INSERT",
        "HOME",
        "END",
        "PAGEUP",
        "PAGEDOWN",
        "UP",
        "DOWN",
        "LEFT",
        "RIGHT",
        "-",
        "=",
        ",",
        ".",
        "/",
        ";",
        "'",
        "[",
        "]",
        "\\",
        "`",
    ];

    private static readonly HashSet<string> AllowedHotkeyModifiers =
    [
        "Alt",
        "Control",
        "Shift",
        "Windows",
    ];

    public static void Validate(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.Terminal is null)
        {
            throw new InvalidOperationException("配置中的终端设置不能为空。");
        }

        if (config.Startup is null)
        {
            throw new InvalidOperationException("配置中的启动设置不能为空。");
        }

        if (config.FallbackBehavior is null)
        {
            throw new InvalidOperationException("配置中的回退行为设置不能为空。");
        }

        if (config.Tools is null)
        {
            throw new InvalidOperationException("配置中的工具集合不能为空。");
        }

        var toolIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hotkeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tool in config.Tools)
        {
            ValidateTool(tool, toolIds, hotkeys);
        }
    }

    private static void ValidateTool(ToolConfig tool, HashSet<string> toolIds, HashSet<string> hotkeys)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (string.IsNullOrWhiteSpace(tool.Id))
        {
            throw new InvalidOperationException("工具 ID 不能为空。");
        }

        if (!tool.Id.All(static character => char.IsLower(character) || char.IsDigit(character) || character == '-'))
        {
            throw new InvalidOperationException($"工具 ID '{tool.Id}' 包含无效字符。");
        }

        if (!toolIds.Add(tool.Id))
        {
            throw new InvalidOperationException($"工具 ID '{tool.Id}' 重复。");
        }

        if (string.IsNullOrWhiteSpace(tool.DisplayName))
        {
            throw new InvalidOperationException($"工具“{tool.Id}”的显示名称不能为空。");
        }

        if (string.IsNullOrWhiteSpace(tool.Command))
        {
            throw new InvalidOperationException($"工具“{tool.Id}”的命令不能为空。");
        }

        if (tool.Args is null)
        {
            throw new InvalidOperationException($"工具“{tool.Id}”的参数集合不能为空。");
        }

        ValidateHotkey(tool, hotkeys);
    }

    private static void ValidateHotkey(ToolConfig tool, HashSet<string> hotkeys)
    {
        if (tool.Hotkey is null)
        {
            throw new InvalidOperationException($"工具“{tool.Id}”的快捷键配置不能为空。");
        }

        if (!tool.Hotkey.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(tool.Hotkey.Key))
        {
            throw new InvalidOperationException($"工具“{tool.Id}”启用快捷键后必须设置按键。");
        }

        var normalizedKey = tool.Hotkey.Key.Trim().ToUpperInvariant();
        if (!AllowedHotkeyKeys.Contains(normalizedKey))
        {
            throw new InvalidOperationException($"工具“{tool.Id}”启用的快捷键按键“{tool.Hotkey.Key}”不受支持。");
        }

        if (tool.Hotkey.Modifiers is null)
        {
            throw new InvalidOperationException($"工具“{tool.Id}”启用的快捷键修饰键不能为空。");
        }

        var normalizedModifiers = tool.Hotkey.Modifiers
            .Select(static modifier => modifier.Trim())
            .ToList();

        if (normalizedModifiers.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"工具“{tool.Id}”启用的快捷键包含空修饰键。");
        }

        if (normalizedModifiers.Any(modifier => !AllowedHotkeyModifiers.Contains(modifier)))
        {
            throw new InvalidOperationException($"工具“{tool.Id}”启用的快捷键包含不受支持的修饰键。");
        }

        var duplicateModifiers = normalizedModifiers
            .GroupBy(static modifier => modifier, StringComparer.OrdinalIgnoreCase)
            .Any(static group => group.Count() > 1);

        if (duplicateModifiers)
        {
            throw new InvalidOperationException($"工具“{tool.Id}”启用的快捷键包含重复修饰键。");
        }

        var signature = string.Join("+", normalizedModifiers
            .OrderBy(static modifier => modifier, StringComparer.OrdinalIgnoreCase))
            + "|" + normalizedKey;

        if (!hotkeys.Add(signature))
        {
            throw new InvalidOperationException($"启用的快捷键“{signature}”重复。");
        }
    }
}
