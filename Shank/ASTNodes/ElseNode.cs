using System.Text;

namespace Shank.ASTNodes;

public class ElseNode : IfNode
{
    public ElseNode(List<StatementNode> children)
        : base(children) { }

    // Copy constructor for monomorphization
    public ElseNode(ElseNode copy, List<StatementNode> children)
        : base(children)
    {
        FileName = copy.FileName;
        Line = copy.Line;
        Expression = copy.Expression;
        NextIfNode = copy.NextIfNode;
    }

    public override string ToString()
    {
        var linePrefix = $"else, line {Line}, ";
        var b = new StringBuilder();
        b.AppendLine(linePrefix + "statements begin");
        Children.ForEach(c => b.AppendLine(c.ToString()));
        b.AppendLine(linePrefix + "statements end");
        return b.ToString();
    }
}
