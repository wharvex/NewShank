using Shank.ASTNodes;

namespace Shank;

public class NewSemanticAnalysis
{
    public static void Run(ProgramNode program)
    {
        program.Walk(new MathOpNodeOptimizer());
    }
}
