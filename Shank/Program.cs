namespace Shank;

public class Program
{
    public static LinkedList<TestResult> UnitTestResults { get; set; } = [];

    public static void Main2(string[] args)
    {
        List<string> inPaths = [];

        if (args.Length < 1)
        {
            Directory
                .GetFiles(Directory.GetCurrentDirectory(), "*.shank", SearchOption.AllDirectories)
                .ToList()
                .ForEach(inPaths.Add);
        }
        else
        {
            if (Directory.Exists(args[0]))
            {
                Directory
                    .GetFiles(args[0], "*.shank", SearchOption.AllDirectories)
                    .ToList()
                    .ForEach(inPaths.Add);
            }
            else if (File.Exists(args[0]) && args[0].EndsWith(".shank"))
            {
                inPaths.Add(args[0]);
            }
        }

        if (inPaths.Count < 1 && args.Length > 1 && args[1] != "-ut")
        {
            Console.Write(Directory.GetCurrentDirectory() + " " + args[0]);
            throw new Exception(
                "Options when calling shank from Command Line (CL): "
                    + "1) pass a valid path to a *.shank file; "
                    + "2) pass a valid path to a directory containing at least one *.shank file; "
                    + "3) have at least one *.shank file in the current directory and pass no CL arguments"
                    + Directory.GetDirectories(Directory.GetCurrentDirectory())
            );
        }
        string interpreterMode = "";
        if (args.Length == 2)
            interpreterMode = args[1];
        foreach (var inPath in inPaths)
        {
            var lines = File.ReadAllLines(inPath);
            var tokens = new List<Token>();
            var l = new Lexer();
            tokens.AddRange(l.Lex(lines));

            OutputHelper.DebugPrintJson(OutputHelper.GetDebugJsonForTokenList(tokens), 1);
            var p = new Parser(tokens);

            var brokeOutOfWhile = false;
            while (tokens.Any())
            {
                //FunctionNode? fn = null;
                ModuleNode module = null;
                var parserErrorOccurred = false;

                try
                {
                    module = p.Module();
                    if (module.getName() == null)
                    {
                        if (Interpreter.getModules().ContainsKey("default"))
                            Interpreter.getModules()["default"].mergeModule(module);
                        else
                        {
                            module.setName("default");
                            Interpreter.Modules.Add("default", module);
                        }
                    }
                }
                catch (SyntaxErrorException e)
                {
                    Console.WriteLine(
                        $"\nParsing error encountered in file {inPath}:\n{e}\nskipping..."
                    );
                    parserErrorOccurred = true;
                }

                if (parserErrorOccurred)
                {
                    brokeOutOfWhile = true;
                    break;
                }
                if (module.getName() != null && module.getName() != "default")
                    Interpreter.Modules.Add(module.getName(), module);
            }

            // Parser error occurred -- skip to next file.
            if (brokeOutOfWhile)
            {
                continue;
            }
        }
        if (interpreterMode == "")
        {
            // Begin program interpretation and output.
            foreach (KeyValuePair<string, ModuleNode> currentModulePair in Interpreter.Modules)
            {
                var currentModule = currentModulePair.Value;
                //Console.WriteLine($"\nOutput of {currentModule.getName()}:\n");

                if (
                    currentModule.getFunctions().ContainsKey("start")
                    && currentModule.getFunctions()["start"] is FunctionNode s
                )
                {
                    Interpreter.setStartModule();
                    OutputHelper.DebugPrintJson(
                        OutputHelper.GetDebugJsonForModuleNode(currentModule),
                        2
                    );
                    BuiltInFunctions.Register(currentModule.getFunctions());
                    SemanticAnalysis.checkModules();
                    var interpreterErrorOccurred = false;
                    try
                    {
                        Interpreter.InterpretFunction(s, [], currentModule);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(
                            $"\nInterpretation error encountered in file {currentModulePair.Key}:\n{e}\nskipping..."
                        );
                        interpreterErrorOccurred = true;
                    }

                    if (interpreterErrorOccurred)
                    {
                        continue;
                    }
                }
            }
        }
        //Unit test interpreter mode
        else
        {
            if (interpreterMode != "-ut")
                throw new Exception(
                    $"The available interpreter run modes are the following: -ut. {interpreterMode} is invalid"
                );

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

    public static void Main(string[] args)
    {
        var cmdLineArgsHelper = new CmdLineArgsHelper(args);
        cmdLineArgsHelper.AddToInPaths();
        Main2A(cmdLineArgsHelper);
    }

    public static void Main2A(CmdLineArgsHelper cmdLineArgsHelper)
    {
        foreach (var inPath in cmdLineArgsHelper.InPaths)
        {
            var lines = File.ReadAllLines(inPath);
            var tokens = new List<Token>();
            var lexer = new Lexer();
            tokens.AddRange(lexer.Lex(lines));

            OutputHelper.DebugPrintJson(OutputHelper.GetDebugJsonForTokenList(tokens), 1);
            var parser = new Parser(tokens);

            while (tokens.Count > 0)
            {
                ModuleNode module = null;
                module = parser.Module();
                if (module.getName() == null)
                {
                    if (Interpreter.getModules().ContainsKey("default"))
                        Interpreter.getModules()["default"].mergeModule(module);
                    else
                    {
                        module.setName("default");
                        Interpreter.Modules.Add("default", module);
                    }
                }

                if (module.getName() != null && module.getName() != "default")
                    Interpreter.Modules.Add(module.getName(), module);
            }
        }
        if (!cmdLineArgsHelper.HasTestFlag())
        {
            // Begin program interpretation and output.
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
