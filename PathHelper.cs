// Adapted from https://stackoverflow.com/a/66285728
// Use PathHelper.PathHelper to produce the same project folder path
// value whether you run the project from your IDE or the command line

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Shank;

internal static class PathHelper
{
    private const string MyRelativePath = nameof(PathHelper) + ".cs";
    private static string? _lazyValue;
    private static bool _isRunningInIDE = !string.IsNullOrEmpty(
        Environment.GetEnvironmentVariable("DOTNET_HOTRELOAD_NAMEDPIPE_NAME")
    );
    private static string _workingDir = Directory.GetCurrentDirectory();
    public static string ProjectFolderPath => _lazyValue ??= CalculatePath();
    public static string TimWinOutPath =
        @"C:\Users\tgudl\OneDrive\projects\c-sharp\ShankCompiler\IR\generated_IR.ll";
    public static string TimLinuxOutPath =
        "/home/tim/projects/c-sharp/ShankCompiler/IR/generated_IR.ll";
    public static string TimWinInPath =
        @"C:\Users\tgudl\OneDrive\projects\c-sharp\ShankCompiler\Shank\fibonacci.shank";
    public static string TimLinuxInPath =
        "/home/tim/projects/c-sharp/ShankCompiler/Shank/fibonacci.shank";
    public static string PathToUse = _isRunningInIDE ? ProjectFolderPath : _workingDir;

    private static string CalculatePath()
    {
        var pathName = GetSourceFilePathName();
        //Debug.Assert(pathName.EndsWith(MyRelativePath, StringComparison.Ordinal));
        return pathName[..^MyRelativePath.Length];
    }

    private static string GetSourceFilePathName([CallerFilePath] string? callerFilePath = null) =>
        callerFilePath ?? "";
}
