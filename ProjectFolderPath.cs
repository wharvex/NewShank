// Adapted from https://stackoverflow.com/a/66285728
// Use this to produce the same project folder path value
// whether you run the project from your IDE or the command line
// Access the path value with ProjectFolderPath.Value

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Shank;

internal static class ProjectFolderPath
{
    private const string MyRelativePath = nameof(ProjectFolderPath) + ".cs";
    private static string? _lazyValue;
    public static string Value => _lazyValue ??= CalculatePath();

    private static string CalculatePath()
    {
        var pathName = GetSourceFilePathName();
        Debug.Assert(pathName.EndsWith(MyRelativePath, StringComparison.Ordinal));
        return pathName.Substring(0, pathName.Length - MyRelativePath.Length);
    }

    public static string GetSourceFilePathName(
        [CallerFilePath] string? callerFilePath = null
    ) //
        => callerFilePath ?? "";
}
