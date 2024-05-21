using Shank.ASTNodes;

namespace Shank;

public class ForNode : StatementNode
{
    public ForNode(
        VariableReferenceNode variable,
        ASTNode from,
        ASTNode to,
        List<StatementNode> children
    )
    {
        Variable = variable;
        From = from;
        To = to;
        Children = children;
    }

    public VariableReferenceNode Variable { get; init; }
    public ASTNode From { get; init; }
    public ASTNode To { get; init; }
    public List<StatementNode> Children { get; init; }

    public override object[] returnStatementTokens()
    {
        object[] arr = { "For", Variable, From, To, Children };
        return arr;
    }

    public override string ToString()
    {
        return $" For: {Variable} From: {From} To: {To} {Environment.NewLine} {StatementListToString(Children)}";
    }
}
