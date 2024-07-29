using Shank.AstVisitorsTim;
using Shank.ExprVisitors;
using Shank.WalkCompliantVisitors;

namespace Shank.ASTNodes;

public abstract class ExpressionNode : ASTNode
{
    private Type? _type;
    public Type Type
    {
        get => _type ?? Type.Default;
        set => _type = value;
    }

    public void Accept(IAstExpressionVisitor visitor) => visitor.Visit(this);

    public override void Accept(Visitor v) => throw new NotImplementedException();

    public override ASTNode Walk(WalkCompliantVisitor v)
    {
        var ret = v.Visit(this, out var shortCircuit);

        if (shortCircuit)
            return ret;

        return v.Final(this);
    }
}
