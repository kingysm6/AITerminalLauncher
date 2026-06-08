using System.Runtime.InteropServices;
using System.Windows.Forms;
using AITerminalLauncher.App.Services;
using AITerminalLauncher.Core.Config;
using AITerminalLauncher.Core.Hotkeys;

namespace AITerminalLauncher.App.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private readonly HotkeyMessageWindow _messageWindow;
    private readonly Dictionary<int, string> _toolIdsByRegistrationId = new();
    private bool _disposed;

    public event EventHandler<string>? HotkeyPressed;

    public GlobalHotkeyService()
    {
        _messageWindow = new HotkeyMessageWindow();
        _messageWindow.HotkeyPressed += OnHotkeyPressed;
    }

    public void RegisterToolHotkeys(IEnumerable<ToolConfig> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        ThrowIfDisposed();

        var toolList = tools.ToList();
        var duplicates = HotkeyConflictDetector.FindDuplicates(toolList);
        if (duplicates.Count > 0)
        {
            var duplicate = duplicates[0];
            throw new InvalidOperationException(
                $"Duplicate hotkey '{duplicate.Chord.Signature}' for tools: {string.Join(", ", duplicate.ToolIds)}");
        }

        UnregisterAll();

        var registrationId = 1;
        foreach (var tool in toolList.Where(static tool => tool.Enabled))
        {
            var chord = HotkeyChord.FromConfig(tool.Hotkey);
            if (chord is null)
            {
                continue;
            }

            if (!RegisterHotKey(_messageWindow.Handle, registrationId, (uint)ToNativeModifiers(chord.Modifiers), (uint)ToVirtualKey(chord.Key)))
            {
                AppLogger.LogInfo($"Global hotkey registration failed for tool '{tool.DisplayName}' and chord '{chord.Signature}'.");
                UnregisterAll();
                throw new InvalidOperationException(
                    $"Failed to register hotkey '{chord.Signature}' for tool '{tool.DisplayName}'.");
            }

            _toolIdsByRegistrationId[registrationId] = tool.Id;
            registrationId++;
        }
    }

    public void UnregisterAll()
    {
        ThrowIfDisposed();

        foreach (var registrationId in _toolIdsByRegistrationId.Keys.ToArray())
        {
            _ = UnregisterHotKey(_messageWindow.Handle, registrationId);
        }

        _toolIdsByRegistrationId.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnregisterAll();
        _messageWindow.HotkeyPressed -= OnHotkeyPressed;
        _messageWindow.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnHotkeyPressed(object? sender, int registrationId)
    {
        if (_toolIdsByRegistrationId.TryGetValue(registrationId, out var toolId))
        {
            HotkeyPressed?.Invoke(this, toolId);
        }
    }

    private static HotkeyModifiers ToNativeModifiers(IEnumerable<string> modifiers)
    {
        var nativeModifiers = HotkeyModifiers.None;

        foreach (var modifier in modifiers)
        {
            nativeModifiers |= modifier switch
            {
                "Alt" => HotkeyModifiers.Alt,
                "Control" => HotkeyModifiers.Control,
                "Shift" => HotkeyModifiers.Shift,
                "Windows" => HotkeyModifiers.Windows,
                _ => throw new InvalidOperationException($"Unsupported hotkey modifier '{modifier}'."),
            };
        }

        return nativeModifiers;
    }

    private static int ToVirtualKey(string key)
    {
        var normalizedKey = key.Trim().ToUpperInvariant();
        if (normalizedKey.Length == 1 && char.IsLetter(normalizedKey[0]))
        {
            return normalizedKey[0];
        }

        if (normalizedKey.Length == 1 && char.IsDigit(normalizedKey[0]))
        {
            return normalizedKey[0];
        }

        if (normalizedKey.StartsWith('F') && int.TryParse(normalizedKey[1..], out var functionKeyNumber) && functionKeyNumber is >= 1 and <= 24)
        {
            return (int)Keys.F1 + functionKeyNumber - 1;
        }

        if (normalizedKey.StartsWith("NUMPAD", StringComparison.Ordinal)
            && int.TryParse(normalizedKey["NUMPAD".Length..], out var numberPadKey)
            && numberPadKey is >= 0 and <= 9)
        {
            return (int)Keys.NumPad0 + numberPadKey;
        }

        var mappedKey = normalizedKey switch
        {
            "SPACE" => Keys.Space,
            "TAB" => Keys.Tab,
            "ESC" => Keys.Escape,
            "ENTER" => Keys.Enter,
            "BACKSPACE" => Keys.Back,
            "DELETE" => Keys.Delete,
            "INSERT" => Keys.Insert,
            "HOME" => Keys.Home,
            "END" => Keys.End,
            "PAGEUP" => Keys.PageUp,
            "PAGEDOWN" => Keys.PageDown,
            "UP" => Keys.Up,
            "DOWN" => Keys.Down,
            "LEFT" => Keys.Left,
            "RIGHT" => Keys.Right,
            "-" => Keys.OemMinus,
            "=" => Keys.Oemplus,
            "," => Keys.Oemcomma,
            "." => Keys.OemPeriod,
            "/" => Keys.OemQuestion,
            ";" => Keys.OemSemicolon,
            "'" => Keys.OemQuotes,
            "[" => Keys.OemOpenBrackets,
            "]" => Keys.OemCloseBrackets,
            "\\" => Keys.OemPipe,
            "`" => Keys.Oemtilde,
            _ => Keys.None,
        };

        if (mappedKey != Keys.None)
        {
            return (int)mappedKey;
        }

        throw new InvalidOperationException($"Unsupported hotkey key '{key}'.");
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [Flags]
    private enum HotkeyModifiers : uint
    {
        None = 0,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Windows = 0x0008,
    }
}
