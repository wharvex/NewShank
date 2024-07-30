using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class ForNode : StatementNode
{
    public ForNode(
        VariableUsagePlainNode variable,
        ExpressionNode from,
        ExpressionNode to,
        List<StatementNode> children
    )
    {
        Variable = variable;
        From = from;
        To = to;
        Children = children;
        NewVariable = new VariableUsagePlainNode("emptyNewVariable", "default");
    }

    public ForNode(
        ExpressionNode from,
        ExpressionNode to,
        List<StatementNode> children,
        VariableUsageNodeTemp newVariable
    )
    {
        Variable = new VariableUsagePlainNode("emptyOldVariable", "default");
        From = from;
        To = to;
        Children = children;
        NewVariable = newVariable;
    }

    // Copy constructor for monomorphization
    public ForNode(ForNode copy, List<StatementNode> children)
    {
        Children = children;
        Variable = copy.Variable;
        NewVariable = copy.NewVariable;
        From = copy.From;
        To = copy.To;
        FileName = copy.FileName;
        Line = copy.Line;
    }

    public VariableUsagePlainNode Variable { get; set; }
    public VariableUsageNodeTemp NewVariable { get; set; }
    public ExpressionNode From { get; init; }
    public ExpressionNode To { get; init; }
    public List<StatementNode> Children { get; set; }

    public override object[] returnStatementTokens()
    {
        object[] arr = { "For", Variable, From, To, Children };
        return arr;
    }

    public override string ToString()
    {
        return $" For: {Variable} From: {From} To: {To} {Environment.NewLine} {StatementListToString(Children)}";
    }

    public override void Accept(Visitor v) => v.Visit(this);

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        Variable = (VariableUsagePlainNode)(Variable.Walk(v) ?? Variable);

        NewVariable = (VariableUsageNodeTemp)(NewVariable.Walk(v) ?? NewVariable);

        for (var index = 0; index < Children.Count; index++)
        {
            Children[index] = (StatementNode)(Children[index].Walk(v) ?? Children[index]);
        }

        return v.PostWalk(this);
    }
}
