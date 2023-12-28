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
                    "Please pass a valid path to a directory containing a *.shank file or to a *.shank file, or have a *.shank file in the current directory and pass no command line arguments"
                );
            }
            var lines = File.ReadAllLines(
                Path.Combine(Directory.GetCurrentDirectory(), "fibonacci.shank")
            );
            var tokens = new List<Token>();
            var l = new Lexer();
            tokens.AddRange(l.Lex(lines));

            //foreach (var t in tokens)
            //    Console.WriteLine(t.ToString());

            var p = new Parser(tokens);
            var ir = new Interpreter();
            while (tokens.Any())
            {
                var fb = p.Function();
                if (fb != null)
                {
                    //Console.WriteLine(fb.ToString());
                    Interpreter.Functions.Add(fb.Name ?? string.Empty, fb);

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
            inPaths.ForEach(Console.WriteLine);
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
