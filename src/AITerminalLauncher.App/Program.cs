using System.Windows.Forms;
using AITerminalLauncher.App.Forms;
using AITerminalLauncher.App.Services;
using AITerminalLauncher.App.Tray;
using AITerminalLauncher.Core.Config;

namespace AITerminalLauncher.App;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Global\AITerminalLauncher.App.SingleInstance";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            using var singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
            if (!createdNew)
            {
                SingleInstanceMessageWindow.RequestShowSettings();
                return;
            }

            var mode = StartupModeResolver.Resolve(Environment.GetCommandLineArgs().Skip(1));
            Application.Run(new LauncherApplicationContext(openSettingsOnStartup: mode == StartupMode.ShowSettings));
        }
        catch (Exception ex)
        {
            AppLogger.LogError("应用启动时发生未处理异常。", ex);
            MessageBox.Show(
                ex.Message,
                "AI Terminal Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

}
