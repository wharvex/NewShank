using Shank.ExprVisitors;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public abstract class ExpressionNode : ASTNode
{
    public abstract T Accept<T>(ExpressionVisitor<T> visit);

    public override void Accept<T>(StatementVisitor v)
    {
        throw new NotImplementedException();
    }

    public override T Accept<T>(IAstNodeVisitor<T> visitor)
    {
        throw new NotImplementedException();
    }
}
