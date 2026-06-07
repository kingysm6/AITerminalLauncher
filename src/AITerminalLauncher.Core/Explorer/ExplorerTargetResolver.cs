namespace AITerminalLauncher.Core.Explorer;

public static class ExplorerTargetResolver
{
    public static string? Resolve(ExplorerWindowSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        var selectedFolder = snapshot.SelectedItems
            .FirstOrDefault(static item => item.IsFolder && !string.IsNullOrWhiteSpace(item.Path));

        if (selectedFolder is not null)
        {
            return selectedFolder.Path;
        }

        return string.IsNullOrWhiteSpace(snapshot.CurrentFolder)
            ? null
            : snapshot.CurrentFolder;
    }
}
