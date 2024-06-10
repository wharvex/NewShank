using System.Text;
using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;

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

    public override T Visit<T>(ExpressionVisitor<T> visit)
    {
        throw new NotImplementedException();
    }

    public virtual void Visit(StatementVisitor visit) { }
}
