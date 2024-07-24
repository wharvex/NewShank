using BenchMark;
using BenchmarkDotNet.Running;

public class Program
{
    public static void Main(string[] args)
    {
        var m = BenchmarkRunner.Run<BenchMarkShank>();
        // Console.WriteLine("hello");
        // Setup.ParseAndLex();
        // Setup.RunInterpeter();
    }
}
