namespace Shank;

public class WhileNode : StatementNode
{
    public WhileNode(BooleanExpressionNode exp, List<StatementNode> children)
    {
        Expression = exp;
        Children = children;
    }

    public BooleanExpressionNode Expression { get; init; }
    public List<StatementNode> Children { get; set; }

    public override object[] returnStatementTokens()
    {
        object[] arr = { "WHILE", Expression.Left, Expression.Op, Expression.Right, Children };

        return arr;
    }

    public override string ToString()
    {
        return $" WHILE: {Expression} {StatementListToString(Children)}";
    }
}
