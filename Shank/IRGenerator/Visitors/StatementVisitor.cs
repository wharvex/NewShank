using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public abstract class StatementVisitor
{
    public abstract void Accept(FunctionCallNode node);
    public abstract void Accept(FunctionNode node);

    public abstract void Accept(WhileNode node);

    public abstract void Accept(AssignmentNode node);
    public abstract void Accept(EnumNode node); //
    public abstract void Accept(ModuleNode node);
    public abstract void Accept(IfNode node); //
    public abstract void Accept(RepeatNode node); //
    public abstract void Accept(RecordNode node); //
    public abstract void Accept(VariableNode node); //
    public abstract void Accept(ProgramNode node); //

    //
}
