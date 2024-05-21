namespace Shank;

public class Program
{
    public static LinkedList<TestResult> UnitTestResults { get; set; } = [];

    public static void Main(string[] args)
    {
        // Instantiate CmdLineArgsHelper with the `shank' command arguments. CLAH's constructor
        // works out what sort of args it has (e.g. do they indicate a whole directory or just a
        // file, do they include the unit test flag, etc.) and then populates its `InPaths' property
        // with the appropriate input file paths.
        // var cmdLineArgsHelper = new CmdLineArgsHelper(args);
        var cmdLineArgsHelper = new CmdLineArgsHelper(new string[]{"TestLLVM.shank"});
        // Create the root of the AST.
        var program = new ProgramNode();

        // Scan and Parse each input file.
        cmdLineArgsHelper.InPaths.ForEach(ip => ScanAndParse(ip, program));

        // Set the program's entry point.
        program.SetStartModule();

        // Save the pre-SA AST to $env:APPDATA\ShankDebugOutput4.json
        OutputHelper.DebugPrintJson(OutputHelper.GetDebugJsonForProgramNode(program), 4);

        // Add Builtin Functions to program.StartModule's functions.
        BuiltInFunctions.Register(program.GetStartModuleSafe().Functions);

        // Check the program for semantic issues.
        SemanticAnalysis.CheckModules(program);
        
        // Interpret the program in normal or unit test mode.
        // InterpretAndTest(cmdLineArgsHelper, program);
        TestLLVM(program, "IR/output.ll");
    }

    private static void ScanAndParse(string inPath, ProgramNode program)
    {
        List<Token> tokens = [];
        var lexer = new Lexer();

        // Read the file and turn it into tokens.
        var lines = File.ReadAllLines(inPath);
        tokens.AddRange(lexer.Lex(lines));

        // Save the tokens to $env:APPDATA\ShankDebugOutput1.json
        OutputHelper.DebugPrintJson(OutputHelper.GetDebugJsonForTokenList(tokens), 1);

        var parser = new Parser(tokens);

        // Parse the tokens and turn them into an AST.
        while (tokens.Count > 0)
        {
            var module = parser.Module();
            program.AddToModules(module);
        }
    }

    private static void InterpretAndTest(CmdLineArgsHelper cmdLineArgsHelper, ProgramNode program)
    {
        Interpreter.Modules = program.Modules;
        Interpreter.StartModule = program.GetStartModuleSafe();

        if (!cmdLineArgsHelper.HasTestFlag())
        {
            It1(program);
        }
        // Unit test interpreter mode.
        else
        {
            It2();
        }
    }

    private static void It1(ProgramNode program)
    {
        Interpreter.InterpretFunction(
            program.GetStartModuleSafe().GetStartFunctionSafe(),
            [],
            program.GetStartModuleSafe()
        );
    }

    private static void It2()
    {
        Interpreter
            .GetModulesAsList()
            .ForEach(module =>
            {
                module
                    .GetFunctionsAsList()
                    .ForEach(f => f.ApplyActionToTests(Interpreter.InterpretFunction, module));

                Console.WriteLine(
                    "[[ Tests from " + module.Name + " ]]\n" + string.Join("\n", UnitTestResults)
                );
            });
    }

    public static void TestLLVM(ProgramNode programNode, string outFile)
    {

        LLVMCodeGen a = new LLVMCodeGen();
        a.CodeGen(outFile, programNode);
    }
}
