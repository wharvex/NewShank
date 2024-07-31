using System.Diagnostics;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shank.ASTNodes;
using Shank.IRGenerator.CompilerPractice;
using Shank.WalkCompliantVisitors;

namespace Shank;

public enum OptPasses
{
    Level0,
    Level1,
    Level2,
    Level3
}

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

[Verb("Compile", isDefault: false, HelpText = "invokes the shank LLVM compiler ")]
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

    public OptPasses OptLevel { get; set; }

    [Option(
        'O',
        "optimize",
        Default = "0",
        Required = false,
        HelpText = "Set optimization level.(0 being least 3 being most)"
    )]
    public string? OptimizationLevels
    {
        set
        {
            OptLevel = value switch
            {
                "0" => OptPasses.Level0,
                "1" => OptPasses.Level1,
                "2" => OptPasses.Level2,
                "3" => OptPasses.Level3,
                _ => OptPasses.Level3
            };
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
        Default = "clang"
    )]
    public string LinkerOption { get; set; }

    [Option('l', HelpText = "for linked files")]
    public IEnumerable<string> LinkedFiles { get; set; }

    [Option('u', "unit-test", HelpText = "Unit test options", Default = false)]
    public bool UnitTest { get; set; }

    [Option('L', "LinkPath", Default = "/", HelpText = "for a link path")]
    public string LinkedPath { get; set; }

    [Option(
        "target",
        Default = "generic",
        HelpText = "target cpu (run clang -print-supported-cpus to see list"
    )]
    public string TargetCPU { get; set; }

    [Option('v', "vuop-test", HelpText = "Variable Usage Operation Test", Default = false)]
    public bool VuOpTest { get; set; }
}

[Verb("Interpret", isDefault: false, HelpText = "invokes the shank interpreter")]
public class InterpretOptions
{
    [Value(index: 0, MetaName = "inputFile", HelpText = "The Shank source file", Required = true)]
    public IEnumerable<string> InputFiles { get; set; }

    [Option('u', "unit-test", HelpText = "Unit test options", Default = false)]
    public bool UnitTest { get; set; }

    [Option(
        'v',
        "vuop-test",
        HelpText = "Variable Usage Operation Test (doesnt work with enums)",
        Default = false
    )]
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
    public static void InvokeShank(string[] args)
    {
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
            .WithNotParsed(
                errors =>
                    Console.WriteLine(
                        $"error bad input, consult the documentation or run"
                            + $"shank --help if you want a list of Commands"
                    )
            );
    }

    private static void SealizeSettings(Settings settings)
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

    private static void RunCompiler(CompileOptions options, ProgramNode program)
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

        var fakeInterpretOptions = new InterpretOptions()
        {
            VuOpTest = options.VuOpTest,
            UnitTest = false,
            InputFiles = []
        };
        options
            .InputFile.ToList()
            .ForEach(
                n =>
                    GetFiles(n) //multiple files
                        .ForEach(ip => ScanAndParse(ip, program, fakeInterpretOptions))
            );
        
        // TODO: ANY NEW THINGS YOU WANT TO ADD TO SEMANTIC ANALYSIS PLEASE ADD TO NEWRUNSEMANTICANALYSIS SO THAT WE CAN SWITCH OVER
        RunSemanticAnalysis(fakeInterpretOptions, program);
        // NewRunSemanticAnalysis(options, program);

        if (options.UnitTest)
        {
            Interpreter.ActiveInterpretOptions = fakeInterpretOptions;
            Interpreter.Modules = program.Modules;
            Interpreter.StartModule = program.GetStartModuleSafe();
            InterpretProgramWithTests();
        }

        // GetFiles(options.InputFile).ForEach(ip => ScanAndParse(ip, program));
        // if (options.UnitTest)
        //     It2();
        var monomorphization = new MonomorphizationVisitor(options.UnitTest);
        program.Accept(monomorphization);
        var monomorphizedProgram = monomorphization.ProgramNode;

        a.CodeGen(options, monomorphizedProgram);
    }

    private static void RunInterpreter(InterpretOptions options, ProgramNode program)
    {
        options
            .InputFiles.ToList()
            .ForEach(
                n =>
                    GetFiles(n) //multiple files
                        .ForEach(ip => ScanAndParse(ip, program, options))
            );
        // TODO: ANY NEW THINGS YOU WANT TO ADD TO SEMANTIC ANALYSIS PLEASE ADD TO NEWRUNSEMANTICANALYSIS SO THAT WE CAN SWITCH OVER
        RunSemanticAnalysis(options, program);
        // NewRunSemanticAnalysis(options, program);

        Interpreter.ActiveInterpretOptions = options;
        Interpreter.Modules = program.Modules;
        Interpreter.StartModule = program.GetStartModuleSafe();
        if (!options.UnitTest)
            InterpretProgram(program);
        else
            InterpretProgramWithTests();
    }

    // extract semantic analysis into one function so that both compiler and interpreter do the same thing
    // this is copied from interpreter as it seems to have had the most up to date semantic analysis at the time of doing this
    // TODO: ANY NEW THINGS YOU WANT TO ADD TO SEMANTIC ANALYSIS PLEASE ADD TO NEWRUNSEMANTICANALYSIS SO THAT WE CAN SWITCH OVER
    private static void RunSemanticAnalysis(InterpretOptions options, ProgramNode program)
    {
        program.SetStartModule();
        SemanticAnalysis.ActiveInterpretOptions = options;
        BuiltInFunctions.Register(program.GetStartModuleSafe().Functions);
        
        SAVisitor.ActiveInterpretOptions = options;
        program.Walk(new ImportVisitor());
        SemanticAnalysis.AreExportsDone = true;
        SemanticAnalysis.AreImportsDone = true;
        
        // This resolves unknown types.
        //program.Walk(new RecordVisitor());
        
        program.Walk(new UnknownTypesVisitor());
        SemanticAnalysis.AreSimpleUnknownTypesDone = true;
        
        // program.Walk(new TestVisitor());
        
        // Create WalkCompliantVisitors.
        var nuVis = new NestedUnknownTypesResolvingVisitor(SemanticAnalysis.ResolveType);
        var vgVis = new VariablesGettingVisitor();
        var etVis = new ExpressionTypingVisitor(SemanticAnalysis.GetTypeOfExpression)
        {
            ActiveInterpretOptions = options
        };
        
        // Apply WalkCompliantVisitors.
        program.Walk(nuVis);
        SemanticAnalysis.AreNestedUnknownTypesDone = true;
        program.Walk(vgVis);
        program.Walk(etVis);
        
        //Run old SA.
        OutputHelper.DebugPrintJson(program, "pre_old_sa");
        SemanticAnalysis.CheckModules(program);
        OutputHelper.DebugPrintJson(program, "post_old_sa");
        
        NewSemanticAnalysis.Run(program);
    }

    private static void NewRunSemanticAnalysis(InterpretOptions options, ProgramNode program)
    {
        program.SetStartModule();
        SemanticAnalysis.ActiveInterpretOptions = options;
        
        SAVisitor.ActiveInterpretOptions = options;
        
        // Handles imports.
        program.Walk(new ImportVisitor());
        // This resolves unknown types.
        program.Walk(new RecordVisitor());
        // This resolves simple unkown types.
        program.Walk(new UnknownTypesVisitor());
        
        program.Walk(new TestVisitor());
        
        // Create WalkCompliantVisitors.
        var nuVis = new NestedUnknownTypesResolvingVisitor(SemanticAnalysis.ResolveType);
        var vgVis = new VariablesGettingVisitor();
        var etVis = new ExpressionTypingVisitor(SemanticAnalysis.GetTypeOfExpression)
        {
            ActiveInterpretOptions = options
        };
        
        // Apply WalkCompliantVisitors.
        program.Walk(nuVis);
        program.Walk(vgVis);
        program.Walk(etVis);
        
        // Run SAVisitors
        program.Walk(new InvalidRecursiveTypeChecker());
        program.Walk(new MathOpNodeVisitor()); // Comes before anything that uses it
        program.Walk(new VariableDeclarationVisitor());// Comes before function calls
        program.Walk(new AssignmentVisitor());
        program.Walk(new BooleanExpressionNodeVisitor());
        program.Walk(new BooleanExpectedVisitor());
        program.Walk(new FunctionCallExistsVisitor()); // Has to be first function call check
        program.Walk(new FunctionCallCountVisitor());
        program.Walk(new FunctionCallTypeVisitor()); 
        program.Walk(new FunctionCallDefaultVisitor()); // Comes before mutability check
        program.Walk(new FunctionCallMutabilityVisitor());
        program.Walk(new BuiltInFunctionCallVisitor());
        program.Walk(new FunctionCallGenericsVariadicsVisitor());
        program.Walk(new ForNodeVisitor());
        program.Walk(new MathOpNodeOptimizer());
    }

    private static void RunCompilePractice(CompilePracticeOptions options, ProgramNode program)
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
            InterpretProgram(program);
        else
            InterpretProgramWithTests();
    }

    private static int InterpretProgram(ProgramNode program)
    {
        Interpreter.InterpretFunction(
            program.GetStartModuleSafe().GetStartFunctionSafe(),
            [],
            program.GetStartModuleSafe()
        );
        return 1;
    }

    private static void ScanAndParse(
        string inPath,
        ProgramNode program,
        InterpretOptions? options = null
    )
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

    private static void InterpretProgramWithTests()
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

    private static List<string> GetFiles(string dir)
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
