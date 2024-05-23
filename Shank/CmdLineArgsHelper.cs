namespace Shank;

public class CmdLineArgsHelper
{
    public string MainArg { get; init; }
    public CmdLineArgsContents ContentsType { get; init; }
    public List<string> InPaths { get; init; }
    private readonly string[] _cmdLineArgs;
    private readonly int _utLocation;

    public CmdLineArgsHelper(string[] cmdLineArgs)
    {
        _cmdLineArgs = cmdLineArgs;

        ValidateCmdLineArgsLen();

        var utLocations = CalculateUtLocations();

        ValidateUtLocationsCount(utLocations.Count);

        _utLocation = utLocations.FirstOrDefault(-1);

        MainArg = CalculateMainArg();

        ContentsType = CalculateContentsType(_utLocation > -1);

        var inPaths = CalculateInPaths();

        ValidateInPathsCount(inPaths.Count);

        InPaths = inPaths;
    }

    public bool HasTestFlag() =>
        ContentsType is CmdLineArgsContents.TestDirectory or CmdLineArgsContents.TestFile;

    private void ValidateCmdLineArgsLen()
    {
        if (_cmdLineArgs.Length > 2)
        {
            throw new InvalidOperationException("Too many command line args.");
        }
    }

    

    private static void ValidateUtLocationsCount(int utLocationsCount)
    {
        if (utLocationsCount > 1)
        {
            throw new InvalidOperationException("More than one -ut flag not allowed.");
        }
    }

    private static void ValidateInPathsCount(int inPathsCount)
    {
        if (inPathsCount == 0)
        {
            throw new InvalidOperationException("No Shank files found.");
        }
    }

    private List<string> CalculateInPaths() =>
        ContentsType switch
        {
            CmdLineArgsContents.Directory
                or CmdLineArgsContents.TestDirectory
                => [..Directory.GetFiles(MainArg, "*.shank", SearchOption.AllDirectories)],
            CmdLineArgsContents.File or CmdLineArgsContents.TestFile => [MainArg],
            _
                => throw new NotImplementedException(
                    "Support for command line arguments contents type "
                    + ContentsType
                    + " has not been implemented."
                )
        };

    private CmdLineArgsContents CalculateContentsType(bool hasUt) =>
        IsMainArgDir() switch
        {
            true when hasUt => CmdLineArgsContents.TestDirectory,
            true when !hasUt => CmdLineArgsContents.Directory,
            false when hasUt => CmdLineArgsContents.TestFile,
            false when !hasUt => CmdLineArgsContents.File,
            _
                => throw new InvalidOperationException(
                    "Invalid configuration of `mainArgIsDir' and `hasUt' values."
                )
        };

    private string CalculateMainArg() =>
        _cmdLineArgs.Length switch
        {
            0 => Directory.GetCurrentDirectory(),
            1 when _utLocation is 0 => Directory.GetCurrentDirectory(),
            1 when _utLocation is -1 => _cmdLineArgs[0],
            2 when _utLocation is 1 => _cmdLineArgs[0],
            2 when _utLocation is 0 => _cmdLineArgs[1],
            _
                => throw new InvalidOperationException(
                    "Bad command line args configuration for determining the main arg."
                )
        };

    private bool IsMainArgDir()
    {
        if (Directory.Exists(MainArg))
        {
            return true;
        }

        if (File.Exists(MainArg))
        {
            return false;
        }

        throw new InvalidOperationException("File or folder not found.");
    }

    private List<int> CalculateUtLocations() =>
        Enumerable.Range(0, _cmdLineArgs.Length).Where(i => _cmdLineArgs[i].Equals("-ut")).ToList();

    public enum CmdLineArgsContents
    {
        Directory,
        File,
        TestDirectory,
        TestFile
    }
}