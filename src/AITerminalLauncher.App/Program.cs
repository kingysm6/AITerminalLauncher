using System.Windows.Forms;
using AITerminalLauncher.App.Forms;
using AITerminalLauncher.App.Services;
using AITerminalLauncher.App.Tray;
using AITerminalLauncher.Core.Config;

namespace AITerminalLauncher.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
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
