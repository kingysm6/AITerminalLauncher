using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AITerminalLauncher.App;

public sealed class SingleInstanceMessageWindow : NativeWindow, IDisposable
{
    private const string MessageName = "AITerminalLauncher.ShowSettings";
    private static readonly int ShowSettingsMessage = RegisterWindowMessage(MessageName);

    public SingleInstanceMessageWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "AITerminalLauncher.SingleInstanceMessageWindow",
        });
    }

    public event EventHandler? ShowSettingsRequested;

    public static void RequestShowSettings()
    {
        if (ShowSettingsMessage == 0)
        {
            return;
        }

        _ = PostMessage(HWND_BROADCAST, ShowSettingsMessage, IntPtr.Zero, IntPtr.Zero);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == ShowSettingsMessage)
        {
            ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
            return;
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

    private static readonly IntPtr HWND_BROADCAST = new(0xffff);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
