using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class BoolNode : ExpressionNode
{
    public BoolNode(bool value)
    {
        Value = value;
    }

    public bool Value { get; set; }

    public int GetValueAsInt() => Value ? 1 : 0; //Get as int (used for the "ulong" requirment)

    public override string ToString()
    {
        return $"{Value}";
    }

    public override void Accept(Visitor v) => v.Visit(this);

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        return v.PostWalk(this);
    }
}
