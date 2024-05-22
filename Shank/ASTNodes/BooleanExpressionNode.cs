using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

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

    public override string ToString()
    {
        return $"({Left.ToString()} {Op} {Right.ToString()})";
    }

    public override LLVMValueRef Visit(
        Visitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }
}
