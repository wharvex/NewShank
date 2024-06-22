using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public abstract class ExpressionNode : ASTNode
{
    public abstract T Accept<T>(ExpressionVisitor<T> visit);

    public override void Accept<T>(StatementVisitor v)
    {
        throw new NotImplementedException();
    }

    public override void Accept(Visitor v) => throw new NotImplementedException();
}
