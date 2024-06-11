using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class MathOpNode(ExpressionNode left, MathOpNode.MathOpType op, ExpressionNode right)
    : ExpressionNode
{
    public MathOpType Op { get; init; } = op;
    public ExpressionNode Left { get; init; } = left;
    public ExpressionNode Right { get; init; } = right;

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

    public override T Accept<T>(ExpressionVisitor<T> visit) => visit.Visit(this);

    public enum MathOpType
    {
        Plus,
        Minus,
        Times,
        Divide,
        Modulo
    }

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
