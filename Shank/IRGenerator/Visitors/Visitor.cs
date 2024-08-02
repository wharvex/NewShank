using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public abstract class Visitor
{
    public abstract void Visit(IntNode node);

    public abstract void Visit(FloatNode node);

    public abstract void Visit(VariableUsagePlainNode node);

    public abstract void Visit(CharNode node);

    public abstract void Visit(BoolNode node);

    public abstract void Visit(StringNode node);
    public abstract void Visit(MathOpNode node);
    public abstract void Visit(BooleanExpressionNode node);
    public abstract void Visit(RecordNode node);

    //public abstract void Visit(ParameterNode node);

    public abstract void Visit(FunctionCallNode node);
    public abstract void Visit(FunctionNode node);

    public abstract void Visit(WhileNode node);

    public abstract void Visit(AssignmentNode node);
    public abstract void Visit(EnumNode node); //
    public abstract void Visit(ModuleNode node);
    public abstract void Visit(IfNode node); //
    public abstract void Visit(RepeatNode node); //
    public abstract void Visit(VariableDeclarationNode node); //
    public abstract void Visit(ProgramNode node); //
    public abstract void Visit(ForNode node); //

    public abstract void Visit(BuiltInFunctionNode node);
    public abstract void Visit(OverloadedFunctionNode node);
}
