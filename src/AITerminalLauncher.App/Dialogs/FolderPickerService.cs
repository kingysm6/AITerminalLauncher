using System.Windows.Forms;

namespace AITerminalLauncher.App.Dialogs;

public sealed class FolderPickerService
{
    public string? PickFolder(IWin32Window? owner = null)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a folder to launch the AI CLI in.",
            ShowNewFolderButton = false,
        };

        return dialog.ShowDialog(owner) == DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }
}
