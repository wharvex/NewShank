using System.Text.Json.Serialization;
using LLVMSharp.Interop;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

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

    public List<ASTNode?> GetChildNodes(Func<ASTNode, List<ASTNode?>> contentsCollector) =>
        contentsCollector(this);

    public List<ASTNode?> GetChildNodes<T>(Func<ASTNode, List<ASTNode?>> contentsCollector)
        where T : ASTNode => [..GetChildNodes(contentsCollector).Where(n => n is T)];
}
