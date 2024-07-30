using System.Text;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class StatementNode : ASTNode
{
    protected static string StatementListToString(List<StatementNode> statements)
    {
        var b = new StringBuilder();
        statements.ForEach(c => b.Append("\t" + c));
        return b.ToString();
    }

    public virtual object[] returnStatementTokens()
    {
        object[] arr = { };
        return arr;
    }

    public override void Accept(Visitor v) => throw new NotImplementedException();

    public override ASTNode? Walk(SAVisitor v)
    {
        throw new NotImplementedException();
    }

    //public override ASTNode Walk(WalkCompliantVisitor v)
    //{
    //    var ret = v.Visit(this, out var shortCircuit);
    //    if (shortCircuit)
    //    {
    //        return ret;
    //    }

    //    return v.Final(this);
    //}
}
