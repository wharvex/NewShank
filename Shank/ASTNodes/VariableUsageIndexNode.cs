using Shank.AstVisitorsTim;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class VariableUsageIndexNode(VariableUsageNodeTemp left, ExpressionNode right)
    : VariableUsageNodeTemp
{
    public VariableUsageNodeTemp Left { get; init; } = ValidateLeft(left);
    public ExpressionNode Right { get; init; } = right;

    public static VariableUsageNodeTemp ValidateLeft(VariableUsageNodeTemp v) =>
        v switch
        {
            VariableUsageMemberNode => v,
            VariableUsagePlainNode => v,
            VariableUsageIndexNode
                => throw new InvalidOperationException("Multidimensional arrays not allowed"),
            _
                => throw new InvalidOperationException(
                    "VariableUsageNode class hierarchy changed, please update this switch accordingly."
                )
        };

    public string BaseName =>
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

    public override void Accept<T>(StatementVisitor v)
    {
        throw new NotImplementedException();
    }

    public override ASTNode? Walk(SAVisitor v)
    {
        return this;
    }

    public void Accept(IVariableUsageIndexVisitor visitor) => visitor.Visit(this);
}
