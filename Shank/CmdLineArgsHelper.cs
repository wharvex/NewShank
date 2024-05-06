namespace Shank;

public class CmdLineArgsHelper
{
    public string MainArg { get; init; }
    public CmdLineArgsContents ContentsType { get; init; } = CmdLineArgsContents.Invalid;
    public List<string> InPaths { get; } = [];

    public CmdLineArgsHelper(string[] cmdLineArgs)
    {
        if (cmdLineArgs.Length > 0)
        {
            MainArg = cmdLineArgs[0];
            var hasValidTestFlag = cmdLineArgs.Length > 1 && cmdLineArgs[1].Equals("-ut");

            if (Directory.Exists(MainArg))
            {
                ContentsType = hasValidTestFlag
                    ? CmdLineArgsContents.TestDirectory
                    : CmdLineArgsContents.Directory;
            }
            else if (File.Exists(MainArg))
            {
                ContentsType = hasValidTestFlag
                    ? CmdLineArgsContents.TestFile
                    : CmdLineArgsContents.File;
            }
        }
        else
        {
            MainArg = Directory.GetCurrentDirectory();
        }
    }

    public bool HasTestFlag() =>
        ContentsType is CmdLineArgsContents.TestDirectory or CmdLineArgsContents.TestFile;

    public void AddToInPaths()
    {
        switch (ContentsType)
        {
            case CmdLineArgsContents.Directory:
            case CmdLineArgsContents.TestDirectory:
                AddToInPaths(Directory.GetFiles(MainArg, "*.shank", SearchOption.AllDirectories));
                break;
            case CmdLineArgsContents.File:
            case CmdLineArgsContents.TestFile:
                AddToInPaths(MainArg);
                break;
            case CmdLineArgsContents.Invalid:
                throw new InvalidOperationException("Invalid Command Line Arguments.");
            default:
                throw new NotImplementedException(
                    "Support for Command Line Arguments Configuration "
                        + ContentsType
                        + " has not been implemented."
                );
        }

        if (InPaths.Count == 0)
        {
            throw new InvalidOperationException("No Shank files found.");
        }
    }

    private void AddToInPaths(string inPath)
    {
        InPaths.Add(inPath);
    }

    private void AddToInPaths(IEnumerable<string> inPaths)
    {
        InPaths.AddRange(inPaths);
    }

    public enum CmdLineArgsContents
    {
        Invalid,
        Directory,
        File,
        TestDirectory,
        TestFile,
    }
}
