using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shank;

public class OutputHelper
{
    public static void DebugPrint(string output)
    {
        var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        using var outputFile = new StreamWriter(
            Path.Combine(docPath, "ShankDebugOutput.txt"),
            true
        );
        outputFile.WriteLine(output);
    }
}
