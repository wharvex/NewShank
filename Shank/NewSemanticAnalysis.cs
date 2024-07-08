using Shank.ASTNodes;

namespace Shank;

public class NewSemanticAnalysis
{
    public static void Run(ProgramNode program)
    {
        program.Walk(new ForNodeVisitor());
        program.Walk(new BooleanExpressionNodeVisitor());
        program.Walk(new FunctionCallCountVisitor());
        program.Walk(new FunctionCallTypeVisitor());
        program.Walk(new FunctionCallDefaultVisitor());

        program.Walk(new MathOpNodeOptimizer());
    }
}
