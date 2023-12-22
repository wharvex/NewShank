// Code that gets project root adapted from https://stackoverflow.com/a/66285728

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Shank;

internal static class PathHelper
{
    private const string MyRelativePath = nameof(PathHelper) + ".cs";
    private static string? _lazyValue;
    public static string ProjectRoot => _lazyValue ??= CalculatePath();

    // This is an environment variable which appears when running the project
    // from Rider or Visual Studio, but not when running it from the command
    // line with `dotnet run`
    private static bool _isRunningInIDE = !string.IsNullOrEmpty(
        Environment.GetEnvironmentVariable("DOTNET_HOTRELOAD_NAMEDPIPE_NAME")
    );
    private static string _workingDir = Directory.GetCurrentDirectory();
    public static string TimWinOutPath =
        @"C:\Users\tgudl\OneDrive\projects\c-sharp\ShankCompiler\IR\generated_IR.ll";
    public static string TimLinuxOutPath =
        "/home/tim/projects/c-sharp/ShankCompiler/IR/generated_IR.ll";
    public static string TimWinInPath =
        @"C:\Users\tgudl\OneDrive\projects\c-sharp\ShankCompiler\Shank\fibonacci.shank";
    public static string TimLinuxInPath =
        "/home/tim/projects/c-sharp/ShankCompiler/Shank/fibonacci.shank";

    // If running from command line, use _workingDir
    // If running from an IDE, use ProjectRoot/Shank
    // Because if running from an IDE, _workingDir is
    // ProjectRoot/bin/Debug/net6.0
    public static string InPathToUse = _isRunningInIDE
        ? Path.Combine(ProjectRoot, "Shank")
        : _workingDir;

    private static string _genIROutPathToUse = _isRunningInIDE
        ? Path.Combine(ProjectRoot, "IR")
        : Path.Combine(_workingDir, "IR");

    private static string CalculatePath()
    {
        var pathName = GetSourceFilePathName();
        Debug.Assert(pathName.EndsWith(MyRelativePath, StringComparison.Ordinal));
        return pathName[..^MyRelativePath.Length];
    }

    private static string GetSourceFilePathName([CallerFilePath] string? callerFilePath = null) =>
        callerFilePath ?? "";

    public static string GetInPathToUse(string fileName)
    {
        return Path.Combine(InPathToUse, fileName);
    }

    public static string GetGenIROutPathToUse()
    {
        Directory.CreateDirectory(_genIROutPathToUse);
        return Path.Combine(_genIROutPathToUse, "generated_IR.ll");
    }
}
