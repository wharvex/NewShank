using Shank.ExprVisitors;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class VariableUsageOpNode(
    ASTNode left,
    ASTNode right,
    VariableUsageOpNode.VariableUsageOpType opType
) : ExpressionNode
{
    public enum VariableUsageOpType
    {
        ArrayIndex,
        MemberAccess
    }

    public ASTNode Left { get; init; } = left;
    public ASTNode Right { get; init; } = right;
    public VariableUsageOpType OpType { get; init; } = opType;

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
