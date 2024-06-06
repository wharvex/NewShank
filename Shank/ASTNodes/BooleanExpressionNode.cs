using LLVMSharp.Interop;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class BooleanExpressionNode : ASTNode
{
    public BooleanExpressionNode(ASTNode left, BooleanExpressionOpType op, ASTNode right)
    {
        Left = left;
        Op = op;
        Right = right;
    }

    public BooleanExpressionOpType Op { get; init; }
    public ASTNode Left { get; init; }
    public ASTNode Right { get; init; }

    public override LLVMValueRef Visit(
        LLVMVisitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        return visitor.Visit(this);
    }

    public override T Visit<T>(ExpressionVisitor<T> visit)
    {
        return visit.Accept(this);
    }

    public override string ToString()
    {
        return $"({Left.ToString()} {Op} {Right.ToString()})";
    }

    public enum BooleanExpressionOpType
    {
        lt,
        le,
        gt,
        ge,
        eq,
        ne
    }
}
