using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class EmptyNode(string comment) : ASTNode
{
    public string Comment { get; set; } = comment;

    public override void Accept(Visitor v)
    {
        throw new NotImplementedException();
    }

    public override ASTNode? Walk(SAVisitor v)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return "Empty " + Comment;
    }
}
