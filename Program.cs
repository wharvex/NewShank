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
                throw new Exception(
                    "Options when calling shank from Command Line (CL): "
                        + "1) pass a valid path to a *.shank file; "
                        + "2) pass a valid path to a directory containing at least one *.shank file; "
                        + "3) have at least one *.shank file in the current directory and pass no CL arguments"
                );
            }

            foreach (var inPath in inPaths)
            {
                var lines = File.ReadAllLines(inPath);
                var tokens = new List<Token>();
                var l = new Lexer();
                tokens.AddRange(l.Lex(lines));

                //foreach (var t in tokens)
                //    Console.WriteLine(t.ToString());

                var p = new Parser(tokens);
                while (tokens.Any())
                {
                    var fb = p.Function();
                    if (fb != null)
                    {
                        //Console.WriteLine(fb.ToString());
                        Console.WriteLine(
                            Path.GetRelativePath(Directory.GetCurrentDirectory(), inPath)[
                                ..^(".shank".Length)
                            ]
                                .Replace('\\', '_')
                                .Replace('/', '_')
                                + '_'
                                + fb.Name
                        );

                        Interpreter.Functions.Add(fb.Name, fb);

                        fb.LLVMCompile();
                    }
                }

                BuiltInFunctions.Register(Interpreter.Functions);
                if (
                    Interpreter.Functions.ContainsKey("start")
                    && Interpreter.Functions["start"] is FunctionNode s
                )
                {
                    Interpreter.InterpretFunction(s, new List<InterpreterDataType>());
                }
            }

            inPaths.ForEach(Console.WriteLine);

            var testPaths = Directory
                .GetFiles(Directory.GetCurrentDirectory(), "*.shank", SearchOption.AllDirectories)
                .ToList();

            foreach (var testPath in testPaths)
            {
                var lines = File.ReadAllLines(testPath);
                var tokens = new List<Token>();
                var l = new Lexer();
                tokens.AddRange(l.Lex(lines));

                //foreach (var t in tokens)
                //    Console.WriteLine(t.ToString());

                var p = new Parser(tokens);
                var brokeOutOfWhile = false;
                while (tokens.Any())
                {
                    FunctionNode? fb = null;
                    var errorOccurred = false;
                    try
                    {
                        fb = p.Function();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception encountered in file {testPath}\n{e.Message}");
                        errorOccurred = true;
                    }

                    if (errorOccurred)
                    {
                        brokeOutOfWhile = true;
                        break;
                    }

                    if (fb == null)
                    {
                        continue;
                    }

                    // Prepend function name with the name of the file it is in
                    // Technically, prepend function name with the path of the current file relative to the current directory
                    var newFbName =
                        Path.GetRelativePath(Directory.GetCurrentDirectory(), testPath)[
                            ..^(".shank".Length)
                        ]
                            .Replace('\\', '_')
                            .Replace('/', '_')
                        + '_'
                        + fb.Name;

                    Console.WriteLine(newFbName);
                    if (Interpreter.Functions.ContainsKey(newFbName))
                    {
                        Console.WriteLine($"Function {newFbName} already exists. Overwriting it.");
                    }

                    fb.Name = newFbName;

                    Interpreter.Functions.Add(fb.Name, fb);

                    fb.LLVMCompile();
                }

                if (brokeOutOfWhile)
                {
                    continue;
                }
            }

            //while (tokens.Any())
            //{
            //    var exp = p.ParseExpressionLine();
            //    Console.WriteLine(exp?.ToString()??"<<<NULL>>>");
            //    if (exp != null)
            //        Console.WriteLine($" calculated: {ir.Resolve(exp)} ");
            //}
            //var fibPath = System.IO.Path.GetDirectoryName(AppContext.BaseDirectory);
        }
    }
}
