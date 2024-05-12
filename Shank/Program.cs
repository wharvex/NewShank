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
        var cmdLineArgsHelper = new CmdLineArgsHelper(args);

        // Create the root of the AST.
        var program = new ProgramNode();

        // Scan and Parse each input file.
        foreach (var inPath in cmdLineArgsHelper.InPaths)
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

        Interpreter.Modules = program.Modules;

        // Set the program's entry point.
        program.SetStartModule();

        // Save the pre-SA AST to $env:APPDATA\ShankDebugOutput4.json
        OutputHelper.DebugPrintJson(OutputHelper.GetDebugJsonForProgramNode(program), 4);

        // Interpretation and testing.
        if (!cmdLineArgsHelper.HasTestFlag())
        {
            foreach (KeyValuePair<string, ModuleNode> currentModulePair in Interpreter.Modules)
            {
                var currentModule = currentModulePair.Value;

                if (
                    currentModule.getFunctions().ContainsKey("start")
                    && currentModule.getFunctions()["start"] is FunctionNode s
                )
                {
                    Interpreter.SetStartModule();
                    OutputHelper.DebugPrintJson(
                        OutputHelper.GetDebugJsonForModuleNode(currentModule),
                        2
                    );
                    BuiltInFunctions.Register(currentModule.getFunctions());
                    SemanticAnalysis.checkModules();
                    Interpreter.InterpretFunction(s, [], currentModule);
                }
            }
        }
        // Unit test interpreter mode
        else
        {
            Interpreter.setStartModule();
            SemanticAnalysis.checkModules();
            BuiltInFunctions.Register(Interpreter.getStartModule().getFunctions());
            foreach (var module in Interpreter.Modules)
            {
                foreach (var function in module.Value.getFunctions())
                {
                    if (function.Value is BuiltInFunctionNode)
                        continue;
                    foreach (var test in ((FunctionNode)function.Value).Tests)
                    {
                        Interpreter.InterpretFunction(test.Value, new List<InterpreterDataType>());
                    }
                }
                Console.WriteLine($"Tests from {module.Key}:");
                foreach (var testResult in UnitTestResults)
                {
                    Console.WriteLine(
                        $"  Test {testResult.testName} (line: {testResult.lineNum}) results:"
                    );
                    foreach (var assertResult in testResult.Asserts)
                    {
                        Console.WriteLine(
                            $"      {assertResult.parentTestName} assertIsEqual (line: {assertResult.lineNum}) "
                                + $"{assertResult.comparedValues} : {(assertResult.passed ? "passed" : "failed")}"
                        );
                    }
                }
            }
        }
    }
}
