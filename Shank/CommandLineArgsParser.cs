using System.Diagnostics;
using CommandLine;
using LLVMSharp.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shank.ASTNodes;
using Shank.IRGenerator.CompilerPractice;
using Shank.WalkCompliantVisitors;

namespace Shank;

[Verb("Settings", isDefault: false, HelpText = "sets settings for you")]
public class Settings
{
    [Option("set-linker", HelpText = "sets the default linker")]
    public string? Setlinker { get; set; }

    [Option("set-cpu", HelpText = "sets the default CPU")]
    public string? SetCPU { get; set; }

    [Option("set-op-level", HelpText = "sets the default op level")]
    public string? SetOpLevel { get; set; }

    [Option("print-settings", HelpText = "displays default")]
    public bool PrintDefaultSettings { get; set; }

    [Option("clear-default", HelpText = "deletes default settings")]
    public bool DeleteSettings { get; set; }

    public override string ToString()
    {
        return "linker: " + Setlinker + " op level: " + SetOpLevel + " cpu: " + SetCPU;
    }
}

[Verb("Compile", isDefault: false, HelpText = "Runs the shank compiler")]
public class CompileOptions
{
    [Option(
        "use-default-settings",
        Default = false,
        HelpText = "uses the default settings in AppData/~ShankData"
    )]
    public bool DefaultSettings { get; set; }

    [Value(index: 0, MetaName = "inputFile", HelpText = "The Shank source file", Required = true)]
    public IEnumerable<string> InputFile { get; set; }

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

    [Option(
        'S',
        "compile-off",
        HelpText = "no exe or object file will be produced here but you may generate .s, .ll files",
        Default = false
    )]
    public bool CompileOff { get; set; }

    [Option(
        "linker",
        HelpText = "add whatever linker you feel if non specified it defaults to the GNU linker (ld)",
        Default = "ld"
    )]
    public string LinkerOption { get; set; }

    [Option('l', HelpText = "for linked files")]
    public IEnumerable<string> LinkedFiles { get; set; }

    [Option('L', "LinkPath", Default = "/", HelpText = "for a link path")]
    public string LinkedPath { get; set; }

    [Option(
        "target",
        Default = "generic",
        HelpText = "target cpu (run clang -print-supported-cpus to see list"
    )]
    public string TargetCPU { get; set; }
}

[Verb("Interpret", isDefault: false, HelpText = "runs the shank interpreter")]
public class InterpretOptions
{
    [Value(index: 0, MetaName = "inputFile", HelpText = "The Shank source file", Required = true)]
    public IEnumerable<string> InputFiles { get; set; }

    [Option('u', "unit-test", HelpText = "Unit test options", Default = false)]
    public bool unitTest { get; set; }

    [Option('v', "vuop-test", HelpText = "Variable Usage Operation Test", Default = false)]
    public bool VuOpTest { get; set; }
}

[Verb("CompilePractice", isDefault: false, HelpText = "dev use only")]
public class CompilePracticeOptions
{
    [Value(index: 0, MetaName = "inputFile", HelpText = "The Shank source file")]
    public string File { get; set; } = "";

    [Option('u', "ut", HelpText = "Unit test options", Default = false)]
    public bool UnitTest { get; set; }

    [Option('f', "flat", HelpText = "Use flattened IR generation", Default = "")]
    public string Flat { get; set; } = "";
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
            .Parser.Default.ParseArguments<
                CompileOptions,
                Settings,
                InterpretOptions,
                CompilePracticeOptions
            >(args)
            .WithParsed<Settings>(options => SealizeSettings(options))
            .WithParsed<CompileOptions>(options => RunCompiler(options, program))
            .WithParsed<InterpretOptions>(options => RunInterpreter(options, program))
            .WithParsed<CompilePracticeOptions>(options => RunCompilePractice(options, program))
            .WithNotParsed(errors => Console.WriteLine($"error with running Shank"));
    }

    public void SealizeSettings(Settings settings)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "~ShankData"
        );
        var filePath = Path.Combine(path, "DefaultSettings.json");
        if (settings.DeleteSettings)
        {
            File.Delete(filePath);
        }

        if (settings.PrintDefaultSettings)
        {
            Settings s = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(filePath));
            Console.WriteLine(s.ToString());
        }

        if (settings.PrintDefaultSettings) { }

        if (File.Exists(filePath))
        {
            Settings s = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(filePath));
            if (settings.Setlinker == null)
            {
                settings.Setlinker = s.Setlinker;
            }

            if (settings.SetOpLevel == null)
            {
                settings.SetOpLevel = s.SetOpLevel;
            }

            if (settings.SetCPU == null)
            {
                settings.SetCPU = s.SetCPU;
            }
        }
        else
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        File.WriteAllText(filePath, JsonConvert.SerializeObject(settings));
        Console.WriteLine(JsonConvert.SerializeObject(settings));
        Console.WriteLine("settings saved");
    }

    public void RunCompiler(CompileOptions options, ProgramNode program)
    {
        if (options.DefaultSettings)
        {
            if (
                File.Exists(
                    Path.Combine(OutputHelper.DocPath, "~ShankData", "DefaultSettings.json")
                )
            )
            {
                Settings? s = JsonConvert.DeserializeObject<Settings>(
                    File.ReadAllText(
                        Path.Combine(OutputHelper.DocPath, "~ShankData", "DefaultSettings.json")
                    )
                );
                if (s.Setlinker != null)
                {
                    options.LinkerOption = s.Setlinker;
                }

                if (s.SetCPU != null)
                {
                    options.TargetCPU = s.SetCPU;
                }

                if (s.SetOpLevel != null)
                {
                    options.OptimizationLevels = s.SetOpLevel;
                }
            }
        }

        LLVMCodeGen a = new LLVMCodeGen();
        options.InputFile.ToList().ForEach(n => Console.WriteLine(n));

        options
            .InputFile.ToList()
            .ForEach(
                n =>
                    GetFiles(n) //multiple files
                        .ForEach(ip => ScanAndParse(ip, program))
            );

        // GetFiles(options.InputFile).ForEach(ip => ScanAndParse(ip, program));
        program.SetStartModule();
        BuiltInFunctions.Register(program.GetStartModuleSafe().Functions);
        SemanticAnalysis.CheckModules(program);
        var monomorphization = new MonomorphizationVisitor();
        program.Accept(monomorphization);
        var monomorphizedProgram = monomorphization.ProgramNode;

        a.CodeGen(options, program);
    }

    public void RunInterpreter(InterpretOptions options, ProgramNode program)
    {
        options
            .InputFiles.ToList()
            .ForEach(
                n =>
                    GetFiles(n) //multiple files
                        .ForEach(ip => ScanAndParse(ip, program, options))
            );
        program.SetStartModule();
        OutputHelper.DebugPrintAst(program, "pre-SA");
        BuiltInFunctions.Register(program.GetStartModuleSafe().Functions);
        SemanticAnalysis.ActiveInterpretOptions = options;
        SemanticVisitor sv = new SemanticVisitor();
        sv.Visit(program);
        SemanticAnalysis.CheckModules(program);

        // Some visiting.
        var vgVis = new VariablesGettingVisitor();
        var etVis = new ExpressionTypingVisitor(SemanticAnalysis.GetTypeOfExpression)
        {
            ActiveInterpretOptions = options
        };
        program.Walk(vgVis);
        OutputHelper.DebugPrintAst(program, "post-vgVis");
        program.Walk(etVis);

        OutputHelper.DebugPrintAst(program, "post-etVis");
        Interpreter.ActiveInterpretOptions = options;
        Interpreter.Modules = program.Modules;
        Interpreter.StartModule = program.GetStartModuleSafe();
        if (!options.unitTest)
            It1(program);
        else
            It2();
    }

    public void RunCompilePractice(CompilePracticeOptions options, ProgramNode program)
    {
        GetFiles(options.File).ForEach(ip => ScanAndParse(ip, program));
        program.SetStartModule();
        BuiltInFunctions.Register(program.GetStartModuleSafe().Functions);
        SemanticAnalysis.AstRoot = program;
        SemanticAnalysis.CheckModules(program);
        var jSets = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = [new StringEnumConverter()],
            Formatting = Formatting.Indented
        };
        OutputHelper.DebugPrintJsonOutput(JsonConvert.SerializeObject(program, jSets), "ast2");
        switch (options.Flat)
        {
            case "":
                var irGen = new IrGenerator(program);
                irGen.GenerateIr();
                break;
            case "PrintStr":
                irGen = new IrGenerator();
                irGen.GenerateIrFlatPrintStr("root");
                break;
            case "PrintInt":
                irGen = new IrGenerator();
                irGen.GenerateIrFlatPrintInt("root");
                break;
            default:
                throw new UnreachableException();
        }

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

    private void ScanAndParse(string inPath, ProgramNode program, InterpretOptions? options = null)
    {
        List<Token> tokens = [];
        var lexer = new Lexer();

        // Read the file and turn it into tokens.
        var lines = File.ReadAllLines(inPath);
        tokens.AddRange(lexer.Lex(lines));

        var parser = new Shank.Parser(tokens, options);

        // Parse the tokens and turn them into an AST.
        while (tokens.Count > 0)
        {
            var module = parser.Module();
            program.AddToModules(module);
        }
    }

    private static void It2()
    {
        LinkedList<TestResult> UnitTestResults = new LinkedList<TestResult>();
        Interpreter
            .GetModulesAsList()
            .ForEach(module =>
            {
                module
                    .GetFunctionsAsList()
                    .ForEach(f => f.ApplyActionToTests(Interpreter.InterpretFunction, module));

                //wierdly doesnt work


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
