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

    public static string GetDebugJsonForProgramNode(ProgramNode pn)
    {
        return JsonSerializer.Serialize(pn, ProgramNodeContext.Default.ProgramNode);
    }

    public static string GetDebugJsonForModuleNode(ModuleNode mn)
    {
        return JsonSerializer.Serialize(mn, ModuleNodeContext.Default.ModuleNode);
    }

    public static string GetDebugJsonForTokenList(List<Token> tokenList)
    {
        return JsonSerializer.Serialize(tokenList, LexerContext.Default.ListToken);
    }

    public static string GetDebugJsonForRecordDataType(RecordDataType rdt)
    {
        return JsonSerializer.Serialize(rdt, RecordDataTypeContext.Default.RecordDataType);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ProgramNode))]
internal partial class ProgramNodeContext : JsonSerializerContext { }

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ModuleNode))]
internal partial class ModuleNodeContext : JsonSerializerContext { }

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<Token>))]
internal partial class LexerContext : JsonSerializerContext { }

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(RecordDataType))]
[JsonSerializable(typeof(Int32))]
internal partial class RecordDataTypeContext : JsonSerializerContext { }
