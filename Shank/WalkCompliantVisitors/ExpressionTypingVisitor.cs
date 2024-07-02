using Shank.ASTNodes;

namespace Shank.WalkCompliantVisitors;

public class ExpressionTypingVisitor(
    ExpressionTypingVisitor.ExprTyGetter getExpressionType,
    Dictionary<string, VariableDeclarationNode> variablesArg
) : WalkCompliantVisitor
{
    public delegate Type ExprTyGetter(
        ExpressionNode node,
        Dictionary<string, VariableDeclarationNode> variables
    );

    private Dictionary<string, VariableDeclarationNode> VariablesArg { get; } = variablesArg;

    public ExprTyGetter GetExpressionType { get; init; } = getExpressionType;

    public override ASTNode? Visit(ASTNode n)
    {
        switch (n)
        {
            case IntNode i:
                i.Type = GetExpressionType(i, VariablesArg);
                return i;
            default:
                throw new NotSupportedException();
        }
    }
}
