using Shank.ASTNodes;

namespace Shank;

public class Program
{
    public static LinkedList<TestResult> UnitTestResults { get; set; } = [];

    public static void Main(string[] args)
    {
        //hopefully i didn't break it up i removed all the useless code here
        //consult the docs to see the different arguments

        CommandLineArgsParser.InvokeShank(args);
    }
}
