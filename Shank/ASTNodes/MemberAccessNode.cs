using Shank.ExprVisitors;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class MemberAccessNode(string name) : ASTNode
{
    public string Name { get; init; } = name;

    public override void Accept<T>(StatementVisitor v)
    {
        throw new NotImplementedException();
    }

    public override void Accept(Visitor v)
    {
        throw new NotImplementedException();
    }

    public override T Accept<T>(IAstNodeVisitor<T> visitor)
    {
        throw new NotImplementedException();
    }
}
