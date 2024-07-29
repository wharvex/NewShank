using System.Text;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class IfNode : StatementNode
{
    protected IfNode(List<StatementNode> children)
    {
        Expression = null;
        Children = children;
        NextIfNode = null;
    }

    public IfNode(
        ExpressionNode expression,
        List<StatementNode> children,
        IfNode? nextIfNode = null
    )
    {
        Expression = expression;
        Children = children;
        NextIfNode = nextIfNode;
    }

    public IfNode(IfNode copy, List<StatementNode> children, IfNode? nextIfNode = null)
    {
        Children = children;
        FileName = copy.FileName;
        Line = copy.Line;
        Expression = copy.Expression;
        NextIfNode = nextIfNode;
    }

    public ExpressionNode? Expression { get; set; }
    public List<StatementNode> Children { get; init; }
    public IfNode? NextIfNode { get; set; }

    public override string ToString()
    {
        var linePrefix = $"if, line {Line}, {Expression}, ";
        var b = new StringBuilder();
        b.AppendLine(linePrefix + "begin");
        b.AppendLine(linePrefix + "statements begin");
        Children.ForEach(s => b.AppendLine(s.ToString()));
        b.AppendLine(linePrefix + "statements end");
        b.AppendLine(linePrefix + "next begin");
        if (NextIfNode != null)
        {
            b.Append(NextIfNode);
        }
        b.AppendLine(linePrefix + "next end");
        b.Append(linePrefix + "end");

        return b.ToString();
    }

    public override void Accept(Visitor v) => v.Visit(this);

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        if (Expression != null)
            Expression = (ExpressionNode)(Expression.Walk(v) ?? Expression);

        for (var index = 0; index < Children.Count; index++)
        {
            Children[index] = (StatementNode)(Children[index].Walk(v) ?? Children[index]);
        }

        if (NextIfNode != null)
            NextIfNode = (IfNode)(NextIfNode.Walk(v) ?? NextIfNode);

        return v.PostWalk(this);
    }
}
