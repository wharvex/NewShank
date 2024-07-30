using Shank.ExprVisitors;
using Shank.WalkCompliantVisitors;

namespace Shank.ASTNodes;

public abstract class ASTNode
{
    public string InheritsDirectlyFrom { get; init; }
    public int Line { get; init; }
    public string FileName { get; init; } // "The AST needs filename added near line number and position"

    protected ASTNode()
    {
        InheritsDirectlyFrom = GetType().BaseType?.Name ?? "None";
        Line = Parser.Line;
        FileName = Parser.FileName;
    }

    public abstract void Accept(Visitor v);

    public virtual ASTNode Walk(WalkCompliantVisitor v) => this;

    public abstract ASTNode? Walk(SAVisitor v);
}
