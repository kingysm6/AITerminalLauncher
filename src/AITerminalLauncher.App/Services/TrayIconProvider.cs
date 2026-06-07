using System.Drawing;
using System.Reflection;

namespace AITerminalLauncher.App.Services;

internal static class TrayIconProvider
{
    /// <summary>
    /// Loads the embedded application icon for tray use. The returned icon is
    /// owned by the caller and must be disposed. Falls back to a clone of the
    /// system application icon if the embedded resource cannot be loaded, so the
    /// caller can always dispose the result uniformly.
    /// </summary>
    public static Icon GetTrayIcon()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("app.ico", StringComparison.OrdinalIgnoreCase));

            if (resourceName is not null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is not null)
                {
                    // Keep all frames so Windows can pick the crispest size per DPI.
                    return new Icon(stream);
                }
            }

            AppLogger.LogInfo("未找到嵌入的应用图标资源,回退到系统默认图标。");
        }
        catch (Exception ex)
        {
            AppLogger.LogError("加载托盘图标失败,回退到系统默认图标。", ex);
        }

        return (Icon)SystemIcons.Application.Clone();
    }
}
