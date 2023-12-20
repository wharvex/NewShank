using Microsoft.VisualBasic.FileIO;

namespace Shank
{
    public class Program
    {
        public static void Main(string[] args)
        {
            const string timWinPath =
                @"C:\Users\tgudl\OneDrive\projects\c-sharp\ShankCompiler\Shank\fibonacci.shank";
            const string timLinuxPath =
                "/home/tim/projects/c-sharp/ShankCompiler/Shank/fibonacci.shank";
            var lines = File.ReadAllLines(timLinuxPath);
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
            //while (tokens.Any())
            //{
            //    var exp = p.ParseExpressionLine();
            //    Console.WriteLine(exp?.ToString()??"<<<NULL>>>");
            //    if (exp != null)
            //        Console.WriteLine($" calculated: {ir.Resolve(exp)} ");
            //}
            //var fibPath = System.IO.Path.GetDirectoryName(AppContext.BaseDirectory);
            var agnosticWorkingDir = Directory.GetCurrentDirectory();
            var agnosticProjectDir = Directory
                .GetParent(agnosticWorkingDir)
                ?.Parent
                ?.Parent
                ?.FullName;
            Console.WriteLine("Testing..." + agnosticProjectDir);
            Console.WriteLine(string.Join('-', args));
            Console.WriteLine(ProjectFolderPath.Value);
        }
    }
}
