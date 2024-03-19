using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Shank;

public class OutputHelper
{
    public static void DebugPrint(string output)
    {
        // If you're using windows, the value of docPath should be:
        // C:\Users\[you]\AppData\Roaming
        // Which should also be the output of the following command in PowerShell:
        // $env:appdata
        var docPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        using var outputFile = new StreamWriter(
            Path.Combine(docPath, "ShankDebugOutput.txt"),
            true
        );
        outputFile.WriteLine(output);
    }

    public static void DebugPrintJson(object obj)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        DebugPrint(JsonSerializer.Serialize(obj, options));
    }
}
