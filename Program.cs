using System.Collections;

namespace Shank
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var inPaths = new List<string>();
            if (args.Length < 1)
            {
                Directory
                    .GetFiles(
                        Directory.GetCurrentDirectory(),
                        "*.shank",
                        SearchOption.AllDirectories
                    )
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

            if (inPaths.Count < 1)
            {
                Console.Write(Directory.GetCurrentDirectory + " " + args[0]);
                throw new Exception(
                    "Options when calling shank from Command Line (CL): "
                        + "1) pass a valid path to a *.shank file; "
                        + "2) pass a valid path to a directory containing at least one *.shank file; "
                        + "3) have at least one *.shank file in the current directory and pass no CL arguments"
                        + Directory.GetDirectories(Directory.GetCurrentDirectory())
                );
            }

            // var count = 0;
            foreach (var inPath in inPaths)
            {
                var lines = File.ReadAllLines(inPath);
                var tokens = new List<Token>();
                var l = new Lexer();
                tokens.AddRange(l.Lex(lines));

                // Prepare to prefix any function name with the name of the file it is in.
                // Technically, a function's name will be prepended with the path of the
                // current file relative to the path of the current directory.
                // This is to facilitate multi-file parsing/interpreting.
                var newFnNamePrefix =
                    Path.GetRelativePath(Directory.GetCurrentDirectory(), inPath)[
                        ..^(".shank".Length)
                    ]
                        .Replace('.', '_')
                        .Replace('\\', '_')
                        .Replace('/', '_') + '_';
                var p = new Parser(tokens, newFnNamePrefix);

                var brokeOutOfWhile = false;
                while (tokens.Any())
                {
                    //FunctionNode? fn = null;
                    ModuleNode module = null;
                    var parserErrorOccurred = false;

                    try
                    {
                        module = p.Module();
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

                    // TODO: Handle global variables here.
                    //if (fn is null)
                    //{
                    //    continue;
                    //}

                    Interpreter.Modules.Add(module.getName(), module);
                }

                // Parser error occurred -- skip to next file.
                if (brokeOutOfWhile)
                {
                    continue;
                }
            }
            Interpreter.handleExports();
            Interpreter.handleImports();
            //Interpreter.moduleSemanticAnalysis();
            // Begin program interpretation and output.
            foreach (KeyValuePair<string, ModuleNode> currentModulePair in Interpreter.Modules) {
                var currentModule = currentModulePair.Value;
                //Console.WriteLine($"\nOutput of {currentModule.getName()}:\n");

                BuiltInFunctions.Register(currentModule.getFunctions());
                if (
                    currentModule.getFunctions().ContainsKey( "start")
                    && currentModule.getFunctions()["start"] is FunctionNode s
                )
                {
                    var interpreterErrorOccurred = false;
                    try
                    {
                        Interpreter.InterpretFunction(s, new List<InterpreterDataType>(), currentModule);
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

                // if (count < 1)
                // {
                //     var gen = new IRGenerator(newFnNamePrefix);
                //     gen.GenerateIR();
                // }

                // count++;
            }
        }
    }
}
