namespace Shank;

public class ElseNode : IfNode
{
    public ElseNode(List<StatementNode> children)
        : base(children) { }

    public override string ToString()
    {
        return $" Else: {StatementListToString(Children)} {((NextIfNode == null) ? string.Empty : NextIfNode)}";
    }
}
