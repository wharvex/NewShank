using Shank.ExprVisitors;

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
}
