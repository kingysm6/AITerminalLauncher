using System.Runtime.InteropServices;
using AITerminalLauncher.App.Services;
using AITerminalLauncher.Core.Explorer;

namespace AITerminalLauncher.App.Explorer;

public sealed class ShellExplorerWindowProvider
{
    public ExplorerWindowSnapshot? GetActiveSnapshot()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return null;
        }

        var shellType = Type.GetTypeFromProgID("Shell.Application");
        if (shellType is null)
        {
            return null;
        }

        object? shell = null;
        object? windows = null;

        try
        {
            shell = Activator.CreateInstance(shellType);
            if (shell is null)
            {
                return null;
            }

            windows = shellType.InvokeMember(
                "Windows",
                System.Reflection.BindingFlags.InvokeMethod,
                binder: null,
                target: shell,
                args: null);

            if (windows is null)
            {
                return null;
            }

            var count = Convert.ToInt32(windows.GetType().InvokeMember(
                "Count",
                System.Reflection.BindingFlags.GetProperty,
                binder: null,
                target: windows,
                args: null));

            for (var index = 0; index < count; index++)
            {
                object? window = null;
                object? document = null;
                object? selectedItems = null;

                try
                {
                    window = windows.GetType().InvokeMember(
                        "Item",
                        System.Reflection.BindingFlags.InvokeMethod,
                        binder: null,
                        target: windows,
                        args: [index]);

                    if (window is null)
                    {
                        continue;
                    }

                    var windowHandleValue = Convert.ToInt64(window.GetType().InvokeMember(
                        "HWND",
                        System.Reflection.BindingFlags.GetProperty,
                        binder: null,
                        target: window,
                        args: null));

                    if (new IntPtr(windowHandleValue) != foregroundWindow)
                    {
                        continue;
                    }

                    var locationUrl = window.GetType().InvokeMember(
                        "LocationURL",
                        System.Reflection.BindingFlags.GetProperty,
                        binder: null,
                        target: window,
                        args: null) as string;

                    if (!TryGetFolderPathFromLocationUrl(locationUrl, out var currentFolder))
                    {
                        return null;
                    }

                    document = window.GetType().InvokeMember(
                        "Document",
                        System.Reflection.BindingFlags.GetProperty,
                        binder: null,
                        target: window,
                        args: null);

                    if (document is null)
                    {
                        return new ExplorerWindowSnapshot(currentFolder, []);
                    }

                    selectedItems = document.GetType().InvokeMember(
                        "SelectedItems",
                        System.Reflection.BindingFlags.InvokeMethod,
                        binder: null,
                        target: document,
                        args: null);

                    var items = ReadSelectedItems(selectedItems);
                    return new ExplorerWindowSnapshot(currentFolder, items);
                }
                catch
                {
                    AppLogger.LogInfo("Explorer snapshot resolution fell back to folder picker after a COM read failure.");
                    return null;
                }
                finally
                {
                    ReleaseComObject(selectedItems);
                    ReleaseComObject(document);
                    ReleaseComObject(window);
                }
            }

            return null;
        }
        finally
        {
            ReleaseComObject(windows);
            ReleaseComObject(shell);
        }
    }

    private static List<SelectedItemSnapshot> ReadSelectedItems(object? selectedItems)
    {
        var items = new List<SelectedItemSnapshot>();
        if (selectedItems is null)
        {
            return items;
        }

        var count = Convert.ToInt32(selectedItems.GetType().InvokeMember(
            "Count",
            System.Reflection.BindingFlags.GetProperty,
            binder: null,
            target: selectedItems,
            args: null));

        for (var index = 0; index < count; index++)
        {
            object? item = null;

            try
            {
                item = selectedItems.GetType().InvokeMember(
                    "Item",
                    System.Reflection.BindingFlags.InvokeMethod,
                    binder: null,
                    target: selectedItems,
                    args: [index]);

                if (item is null)
                {
                    continue;
                }

                var path = item.GetType().InvokeMember(
                    "Path",
                    System.Reflection.BindingFlags.GetProperty,
                    binder: null,
                    target: item,
                    args: null) as string;

                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var isFolder = Convert.ToBoolean(item.GetType().InvokeMember(
                    "IsFolder",
                    System.Reflection.BindingFlags.GetProperty,
                    binder: null,
                    target: item,
                    args: null));

                items.Add(new SelectedItemSnapshot(path, isFolder));
            }
            finally
            {
                ReleaseComObject(item);
            }
        }

        return items;
    }

    private static bool TryGetFolderPathFromLocationUrl(string? locationUrl, out string? currentFolder)
    {
        currentFolder = null;

        if (string.IsNullOrWhiteSpace(locationUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(locationUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.IsFile)
        {
            return false;
        }

        currentFolder = uri.LocalPath;
        return !string.IsNullOrWhiteSpace(currentFolder);
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            _ = Marshal.FinalReleaseComObject(value);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
