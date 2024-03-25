using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace Shank;

public class OutputHelper
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static void DebugPrint(string output, int i)
    {
        // If you're using windows, the value of docPath should be:
        // C:\Users\[you]\AppData\Roaming
        // Which should also be the output of the following command in PowerShell:
        // $env:appdata
        var docPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        using var outputFile = new StreamWriter(
            Path.Combine(docPath, $"ShankDebugOutput{i}.json"),
            true
        );
        outputFile.WriteLine(output);
    }

    public static string GetDebugJsonForModuleNode(ModuleNode mn)
    {
        return JsonSerializer.Serialize(mn, ModuleNodeContext.Default.ModuleNode);
    }

    public static string GetDebugJsonForTokenList(List<Token> tokenList)
    {
        return JsonSerializer.Serialize(tokenList, LexerContext.Default.ListToken);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ModuleNode))]
internal partial class ModuleNodeContext : JsonSerializerContext { }

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<Token>))]
internal partial class LexerContext : JsonSerializerContext { }
