namespace Shank;

/// <summary>
/// If you're using Windows, these output files should save to something like:
/// C:\Users\[you]\AppData\Roaming
/// You should be able to get the exact path by running the following command in PowerShell:
/// $env:appdata
/// </summary>
public class OutputHelper
{
    public static string DocPath { get; } =
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static void DebugPrintJson(string output, string suffix)
    {
        using var outputFile = new StreamWriter(
            Path.Combine(DocPath, "ShankDebugOutput_" + suffix + ".json")
        );
        outputFile.WriteLine(output);
    }

    public static void DebugPrintTxt(string output, string suffix, bool append = false)
    {
        using var outputFile = new StreamWriter(
            Path.Combine(DocPath, "ShankDebugOutput_" + suffix + ".txt"),
            append
        );
        outputFile.WriteLine(output);
    }
}
