using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class CharNode : ExpressionNode
{
    public CharNode(char value)
    {
        Value = value;
    }

    public char Value { get; set; }

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
