namespace OverSync.Core.Services;

public static class FileDiscovery
{
    private static readonly string[] ExcludedFragments =
    [
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
        $"{Path.AltDirectorySeparatorChar}.git{Path.AltDirectorySeparatorChar}"
    ];

    private static readonly string[] ExcludedSuffixes =
    [
        ".tmp",
        ".swp",
        ".lock",
        "~"
    ];

    public static IReadOnlyList<string> EnumerateFiles(string rootPath)
    {
        return Directory
            .EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Where(path => !IsExcluded(path))
            .ToList();
    }

    public static bool IsExcluded(string path)
    {
        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        if (ExcludedFragments.Any(normalized.Contains))
        {
            return true;
        }

        return ExcludedSuffixes.Any(suffix => normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    public static string ToRelativePath(string rootPath, string absolutePath)
    {
        var relative = Path.GetRelativePath(rootPath, absolutePath);
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }
}
