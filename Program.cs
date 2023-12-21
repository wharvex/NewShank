using System.Collections;

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
            var pathToUse = timLinuxPath;
            // pathToUse = timWinPath;
            var lines = File.ReadAllLines(pathToUse);
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
            Console.WriteLine("Testing..." + agnosticWorkingDir);
            Console.WriteLine(string.Join('-', args));
            Console.WriteLine(ProjectFolderPath.Value);
            var entries = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>();
            var sortedEntries = entries.OrderBy(x => (string)x.Key);
            var fs = File.Create(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "h47.txt"
                )
            );
            var fw = new StreamWriter(fs);
            // fw.WriteLine("From Rider");
            fw.WriteLine("From shank2");
            fw.Flush();
            foreach (var y in sortedEntries)
            {
                var lineToWrite = $"Key = {y.Key}, Value = {y.Value}";
                Console.WriteLine(lineToWrite);
                fw.WriteLine(lineToWrite);
            }
            fw.Flush();

            // If the project is being run from the command line, use Directory.GetCurrentDirectory()
            // If the project is being run from an IDE, use ProjectFolderPath.Value
            var isRunningInIDE = !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("DOTNET_HOTRELOAD_NAMEDPIPE_NAME")
            );
            Console.WriteLine("Is running in IDE: {0}", isRunningInIDE);
        }
    }
}
