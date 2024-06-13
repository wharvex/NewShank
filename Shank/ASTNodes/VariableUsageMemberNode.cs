using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class VariableUsageMemberNode(VariableUsageNodeTemp left, MemberAccessNode right)
    : VariableUsageNodeTemp
{
    public VariableUsageNodeTemp Left { get; init; } = left;
    public MemberAccessNode Right { get; init; } = right;

    public override T Accept<T>(ExpressionVisitor<T> visit)
    {
        throw new NotImplementedException();
    }
}
