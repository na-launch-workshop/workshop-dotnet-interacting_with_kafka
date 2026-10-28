namespace HomeGrid;

/// <summary>
/// Locates the repository root, where <c>data/appliances.db</c> lives, no matter which
/// directory <c>dotnet run</c> was started from.
/// </summary>
public static class RepoRoot
{
    public static string Find()
    {
        // Dev Spaces sets this to the cloned project directory.
        if (Environment.GetEnvironmentVariable("PROJECT_SOURCE") is { Length: > 0 } projectSource)
        {
            return projectSource;
        }

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "HomeGrid.sln")))
            {
                return dir.FullName;
            }
        }

        return Directory.GetCurrentDirectory();
    }
}
