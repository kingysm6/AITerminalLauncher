using System.Windows.Forms;

namespace AITerminalLauncher.App.Hotkeys;

public sealed class HotkeyMessageWindow : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;

    public event EventHandler<int>? HotkeyPressed;

    public HotkeyMessageWindow()
    {
        CreateHandle(new CreateParams());
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey)
        {
            HotkeyPressed?.Invoke(this, m.WParam.ToInt32());
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            DestroyHandle();
        }

        GC.SuppressFinalize(this);
    }
}
