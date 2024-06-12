using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class BooleanExpressionNode : ExpressionNode
{
    public BooleanExpressionNode(
        ExpressionNode left,
        BooleanExpressionOpType op,
        ExpressionNode right
    )
    {
        Left = left;
        Op = op;
        Right = right;
    }

    public BooleanExpressionOpType Op { get; init; }
    public ExpressionNode Left { get; init; }
    public ExpressionNode Right { get; init; }

    // public override LLVMValueRef Visit(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     return visitor.Visit(this);
    // }



    public override string ToString()
    {
        return $"{Left} {Op} {Right}";
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

    public override T Accept<T>(ExpressionVisitor<T> visit) => visit.Visit(this);

    public override void Accept(Visitor v) => v.Visit(this);

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
