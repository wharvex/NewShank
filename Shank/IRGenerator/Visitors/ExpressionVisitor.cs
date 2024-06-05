using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public abstract class ExpressionVisitor<T>
{
    public abstract T Accept(IntNode node);

    public abstract T Accept(FloatNode node);

    public abstract T Accept(VariableReferenceNode node);

    public abstract T Accept(CharNode node);

    public abstract T Accept(BoolNode node);

    public abstract T Accept(StringNode node);
    public abstract T Accept(MathOpNode node);
    public abstract T Accept(BooleanExpressionNode node);
    public abstract T Accept(RecordNode node);

    public abstract T Accept(ParameterNode node);
}
