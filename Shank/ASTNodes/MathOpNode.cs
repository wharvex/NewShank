using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class MathOpNode(ASTNode left, MathOpNode.MathOpType op, ASTNode right) : ASTNode
{
    public MathOpType Op { get; init; } = op;
    public ASTNode Left { get; init; } = left;
    public ASTNode Right { get; init; } = right;

    public override string ToString()
    {
        return $"{Left} {Op} {Right}";
    }

    // public override LLVMValueRef Visit(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     return visitor.Visit(this);
    // }

    public override T Visit<T>(ExpressionVisitor<T> visit)
    {
        return visit.Accept(this);
    }

    public enum MathOpType
    {
        Plus,
        Minus,
        Times,
        Divide,
        Modulo
    }
}
