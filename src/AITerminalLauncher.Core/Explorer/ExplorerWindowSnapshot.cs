namespace AITerminalLauncher.Core.Explorer;

public sealed record ExplorerWindowSnapshot(string? CurrentFolder, IReadOnlyList<SelectedItemSnapshot> SelectedItems);
