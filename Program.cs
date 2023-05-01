using LLVMSharp.Interop;

namespace Shank
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var lines = File.ReadAllLines("Shank/fibonacci.shank");
            var tokens = new List<Token>();
            var l = new Lexer();
            tokens.AddRange(l.Lex(lines));

            //foreach (var t in tokens)
                //Console.WriteLine(t.ToString());

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
            if (Interpreter.Functions.ContainsKey("start") && Interpreter.Functions["start"] is FunctionNode s)
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
        }
    }
}