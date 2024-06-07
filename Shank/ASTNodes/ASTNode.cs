using System.Text.Json.Serialization;
using LLVMSharp.Interop;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

[JsonDerivedType(typeof(StringNode))]
[JsonDerivedType(typeof(IntNode))]
[JsonDerivedType(typeof(FloatNode))]
[JsonDerivedType(typeof(BoolNode))]
[JsonDerivedType(typeof(CharNode))]
[JsonDerivedType(typeof(VariableReferenceNode))]
[JsonDerivedType(typeof(MathOpNode))]
[JsonDerivedType(typeof(BooleanExpressionNode))]
[JsonDerivedType(typeof(StatementNode))]
[JsonDerivedType(typeof(FunctionNode))]
public abstract class ASTNode
{
    public string NodeName { get; init; }
    public string InheritsDirectlyFrom { get; init; }
    public int Line { get; init; }
    public string FileName { get; init; } // "The AST needs filename added near line number and position"

    protected ASTNode()
    {
        NodeName = GetType().Name;
        InheritsDirectlyFrom = GetType().BaseType?.Name ?? "None";
        Line = Parser.Line;
        FileName = Parser.FileName;
    }

    // public abstract LLVMValueRef Accept(LLVMBuilderRef builder, LLVMModuleRef module);
    public abstract LLVMValueRef Visit(
        LLVMVisitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    );

    public abstract T Visit<T>(ExpressionVisitor<T> visit);

    public List<ASTNode> GetContents(Func<ASTNode, List<ASTNode>> contentsCollector)
    {
        return contentsCollector(this);
    }

    public List<ASTNode> GetContents<T>(Func<T, List<ASTNode>> contentsCollector)
        where T : ASTNode => contentsCollector((T)this);

    public List<ASTNode> GetContents<TTarget, TFilter>(
        Func<TTarget, List<ASTNode>> contentsCollector
    )
        where TTarget : ASTNode
        where TFilter : ASTNode =>
        contentsCollector((TTarget)this).Where(n => n is TFilter).ToList();
}
