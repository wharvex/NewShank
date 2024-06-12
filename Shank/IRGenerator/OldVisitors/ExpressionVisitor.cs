using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public abstract class ExpressionVisitor<T>
{
    public abstract T Visit(IntNode node);

    public abstract T Visit(FloatNode node);

    public abstract T Visit(VariableUsageNode node);

    public abstract T Visit(CharNode node);

    public abstract T Visit(BoolNode node);

    public abstract T Visit(StringNode node);
    public abstract T Visit(MathOpNode node);
    public abstract T Visit(BooleanExpressionNode node);
    public abstract T Visit(RecordNode node);

    public abstract T Visit(ParameterNode node);
}
