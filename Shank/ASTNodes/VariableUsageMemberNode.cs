using Shank.AstVisitorsTim;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class VariableUsageMemberNode(VariableUsageNodeTemp left, MemberAccessNode right)
    : VariableUsageNodeTemp
{
    public VariableUsageNodeTemp Left { get; init; } = left;
    public MemberAccessNode Right { get; init; } = right;

    public string GetBaseName() =>
        Left switch
        {
            VariableUsageMemberNode m => m.Right.Name,
            VariableUsagePlainNode p => p.Name,
            VariableUsageIndexNode
                => throw new InvalidOperationException("Multidimensional arrays not allowed."),
            _
                => throw new InvalidOperationException(
                    "VariableUsageNode class hierarchy changed, please update this switch accordingly."
                )
        };

    public override T Accept<T>(ExpressionVisitor<T> visit)
    {
        throw new NotImplementedException();
    }
}
