using Shank.AstVisitorsTim;
using Shank.ExprVisitors;
using Shank.WalkCompliantVisitors;

namespace Shank.ASTNodes;

public abstract class ExpressionNode : ASTNode
{
    public Type? Type { get; set; }

    public void Accept(IAstExpressionVisitor visitor) => visitor.Visit(this);

    public abstract T Accept<T>(ExpressionVisitor<T> visit);

    public override void Accept<T>(StatementVisitor v)
    {
        throw new NotImplementedException();
    }

    public override void Accept(Visitor v) => throw new NotImplementedException();
}
