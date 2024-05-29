using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CommandLine;
using LLVMSharp.Interop;
using Shank;
using Shank.IRGenerator.CompilerPractice;

[Verb("Compile", isDefault: false)]
public class CompileOptions
{
    [Value(index: 0, MetaName = "inputFile", HelpText = "The Shank source file", Required = true)]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "returns an output file", Default = "a")]
    public string OutFile { get; set; }

    [Option('c', "compile", HelpText = "compile to object file")]
    public bool CompileToObj { get; set; }

    public LLVMCodeGenOptLevel OptLevel { get; set; }

    [Option('O', "optimize", Default = "0", Required = false, HelpText = "Set optimization level.")]
    public string? OptimizationLevels
    {
        get { return null; }
        set
        {
            if (value.Equals("1"))
                OptLevel = LLVMCodeGenOptLevel.LLVMCodeGenLevelLess;
            else if (value.Equals("2"))
                OptLevel = LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault;
            else if (value.Equals("3"))
                OptLevel = LLVMCodeGenOptLevel.LLVMCodeGenLevelAggressive;
            else if (value.Equals("0"))
                OptLevel = LLVMCodeGenOptLevel.LLVMCodeGenLevelNone;
        }
    }

    [Option("emit-ir", HelpText = "writes IR to file")]
    public bool emitIR { get; set; }

    [Option(
        'a',
        "assembly",
        HelpText = "option to generate a assembly file appears in /Shank-assembly/ directory"
    )]
    public bool Assembly { get; set; }

    [Option("print-ir", HelpText = "prints IR code gen in console appears in /Shank-IR/ directory")]
    public bool printIR { get; set; }
}

[Verb("Interpret", isDefault: false)]
public class InterptOptions
{
    [Value(index: 0, MetaName = "inputFile", HelpText = "The Shank source file", Required = true)]
    public string? file { get; set; }

    [Option('u', "ut", HelpText = "Unit test options", Default = false)]
    public bool unitTest { get; set; }
}

[Verb("CompilePractice", isDefault: false)]
public class CompilePracticeOptions
{
    [Value(index: 0, MetaName = "inputFile", HelpText = "The Shank source file", Required = true)]
    public string? File { get; set; }

    [Option('u', "ut", HelpText = "Unit test options", Default = false)]
    public bool UnitTest { get; set; }

    public string GetFileSafe() =>
        File ?? throw new InvalidOperationException("Expected File to not be null.");
}

public class CommandLineArgsParser
{
    private string[] _args { get; }

    public CommandLineArgsParser(string[] args)
    {
        _args = args;
        // new Options().InputFiles = "a";
        ProgramNode program = new ProgramNode();
        CommandLine
            .Parser.Default.ParseArguments<CompileOptions, InterptOptions, CompilePracticeOptions>(
                args
            )
            .WithParsed<CompileOptions>(options => RunCompiler(options, program))
            .WithParsed<InterptOptions>(options => RunInterptrer(options, program))
            .WithParsed<CompilePracticeOptions>(options => RunCompilePractice(options, program))
            .WithNotParsed(errors => Console.WriteLine("Error"));
    }

    public void RunCompiler(CompileOptions options, ProgramNode program)
    {
        LLVMCodeGen a = new LLVMCodeGen();
        GetFiles(options.InputFile).ForEach(ip => ScanAndParse(ip, program));
        program.SetStartModule();
        OutputHelper.DebugPrintJson(OutputHelper.GetDebugJsonForProgramNode(program), 4);
        SemanticAnalysis.CheckModules(program);

        Interpreter.Modules = program.Modules;
        Interpreter.StartModule = program.GetStartModuleSafe();
        Console.WriteLine(options.OutFile);
        a.CodeGen(options, program);
    }

    public void RunInterptrer(InterptOptions options, ProgramNode program)
    {
        // scan and parse :p
        GetFiles(options.file).ForEach(ip => ScanAndParse(ip, program));
        program.SetStartModule();
        OutputHelper.DebugPrintJson(OutputHelper.GetDebugJsonForProgramNode(program), 4);
        BuiltInFunctions.Register(program.GetStartModuleSafe().Functions);
        SemanticAnalysis.CheckModules(program);

        Interpreter.Modules = program.Modules;
        Interpreter.StartModule = program.GetStartModuleSafe();
        if (!options.unitTest)
            It1(program);
        else
            It2();
    }

    public void RunCompilePractice(CompilePracticeOptions options, ProgramNode program)
    {
        GetFiles(options.GetFileSafe()).ForEach(ip => ScanAndParse(ip, program));
        program.SetStartModule();
        OutputHelper.DebugPrintJson(OutputHelper.GetDebugJsonForProgramNode(program), 4);
        BuiltInFunctions.Register(program.GetStartModuleSafe().Functions);
        SemanticAnalysis.CheckModules(program);
        var irGen = new IrGenerator(program);
        irGen.GenerateIr();

        Interpreter.Modules = program.Modules;
        Interpreter.StartModule = program.GetStartModuleSafe();
        if (!options.UnitTest)
            It1(program);
        else
            It2();
    }

    private void It1(ProgramNode program)
    {
        Interpreter.InterpretFunction(
            program.GetStartModuleSafe().GetStartFunctionSafe(),
            [],
            program.GetStartModuleSafe()
        );
    }

    private void ScanAndParse(string inPath, ProgramNode program)
    {
        List<Token> tokens = [];
        var lexer = new Lexer();

        // Read the file and turn it into tokens.
        var lines = File.ReadAllLines(inPath);
        tokens.AddRange(lexer.Lex(lines));

        // Save the tokens to $env:APPDATA\ShankDebugOutput1.json
        OutputHelper.DebugPrintJson(OutputHelper.GetDebugJsonForTokenList(tokens), 1);

        var parser = new Shank.Parser(tokens);

        // Parse the tokens and turn them into an AST.
        while (tokens.Count > 0)
        {
            var module = parser.Module();
            program.AddToModules(module);
        }
    }

    private static void It2()
    {
        // LinkedList<TestResult> UnitTestResults = new LinkedList<TestResult>();
        Interpreter
            .GetModulesAsList()
            .ForEach(module =>
            {
                module
                    .GetFunctionsAsList()
                    .ForEach(f => f.ApplyActionToTests(Interpreter.InterpretFunction, module));

                Console.WriteLine(
                    "[[ Tests from "
                        + module.Name
                        + " ]]\n"
                        + string.Join("\n", Program.UnitTestResults)
                );
            });
    }

    private List<string> GetFiles(string dir)
    {
        if (Directory.Exists(dir))
        {
            return [..Directory.GetFiles(dir, "*.shank", SearchOption.AllDirectories)];
        }
        else if (File.Exists(dir))
        {
            return [dir];
        }

        throw new FileNotFoundException($"file or dir {dir} doesnt exist");
    }
}
