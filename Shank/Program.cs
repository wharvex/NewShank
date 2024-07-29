using Shank.ASTNodes;

namespace Shank;

public class Program
{
    public static LinkedList<TestResult> UnitTestResults { get; set; } = [];

    public static void Main(string[] args)
    {
        //hopefully i didnt break it up i removed all the useless code here
        //cosnult the docs to see the different arguments
        new CommandLineArgsParser(args);
    }
}
