using Shank.ExprVisitors;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class VariableUsageIndexNode(VariableUsageNodeTemp left, ExpressionNode right)
    : VariableUsageNodeTemp
{
    public VariableUsageNodeTemp Left { get; init; } = left;
    public ExpressionNode Right { get; init; } = right;

    public override T Accept<T>(ExpressionVisitor<T> visit)
    {
        throw new NotImplementedException();
    }

    public override void Accept<T>(StatementVisitor v)
    {
        throw new NotImplementedException();
    }

    public override T Accept<T>(IAstNodeVisitor<T> visitor)
    {
        throw new NotImplementedException();
    }
}
