using System.Net.Sockets;
using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class MathOpNode(ExpressionNode left, MathOpNode.MathOpType op, ExpressionNode right)
    : ExpressionNode
{
    public MathOpType Op { get; init; } = op;
    public ExpressionNode Left { get; set; } = left;
    public ExpressionNode Right { get; set; } = right;

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

    public override void Accept(Visitor v) => v.Visit(this);

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        Left = (ExpressionNode)(Left.Walk(v) ?? Left);

        Right = (ExpressionNode)(Right.Walk(v) ?? Right);

        return v.PostWalk(this);
    }
}
