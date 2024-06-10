using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public abstract class VisitPrototype
{
    public abstract void Accept(FunctionNode node);
    public abstract void Accept(ModuleNode node);
    public abstract void Accept(RecordNode node);
    public abstract void Accept(VariableNode node);
}
