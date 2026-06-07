namespace AITerminalLauncher.App.Services;

internal static class ScriptPathResolver
{
    public static string ResolveRequiredScriptPath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidatePath = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"无法从“{AppContext.BaseDirectory}”定位所需脚本“{fileName}”。");
    }
}
