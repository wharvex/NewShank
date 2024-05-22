using System.Text;
using System.Text.Json.Serialization;
using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

[JsonDerivedType(typeof(RecordMemberNode))]
[JsonDerivedType(typeof(AssignmentNode))]
[JsonDerivedType(typeof(FunctionCallNode))]
[JsonDerivedType(typeof(IfNode))]
[JsonDerivedType(typeof(ForNode))]
[JsonDerivedType(typeof(WhileNode))]
[JsonDerivedType(typeof(RepeatNode))]
public abstract class StatementNode : ASTNode
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

    public abstract void VisitStatement(
        IVisitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    );

    public override LLVMValueRef Visit(
        IVisitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        // statement nodes ues visit statement as they do not return something
        throw new NotImplementedException();
    }
}
