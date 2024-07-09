using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shank.ASTNodes;

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

    public static void DebugPrintAst(ProgramNode program, string postSuffix)
    {
        DebugPrintJson(program, "ast_" + postSuffix);
    }

    public static void DebugPrintJson(object obj, string suffix)
    {
        var jSets = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = [new StringEnumConverter()],
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.All
        };
        DebugPrintJsonOutput(JsonConvert.SerializeObject(obj, jSets), suffix);
    }

    public static void DebugPrintJsonOutput(string output, string suffix)
    {
        using var outputFile = new StreamWriter(
            Path.Combine(DocPath, "ShankDebugOutput_" + suffix + ".json")
        );
        outputFile.WriteLine(output);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>DebugPrintTxt</c> creates a file to hold our debug text and writes the output provided to the file
    ///     </para>
    /// </summary>
    /// <param name="output">The output to be put in the file destination</param>
    /// <param name="suffix">A suffix added to the file name</param>
    /// <param name="append">Whether or not to append the doc path to the name of the file</param>

    public static void DebugPrintTxt(string output, string suffix, bool append = false)
    {
        //we are naming and creating the output file
        using var outputFile = new StreamWriter(
            Path.Combine(DocPath, "ShankDebugOutput_" + suffix + ".txt"),
            append
        );

        //output text is written to the file
        outputFile.WriteLine(output);
    }
}
