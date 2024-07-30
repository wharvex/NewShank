using Shank.ExprVisitors;

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
    public ExpressionNode Left { get; set; }
    public ExpressionNode Right { get; set; }

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
