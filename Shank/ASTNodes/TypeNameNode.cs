using Shank.ExprVisitors;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class TypeNameNode(string name, TypeNameNode? innerName = null) : ASTNode
{
    public string Name { get; set; } = name;
    public TypeNameNode? InnerName { get; set; } = innerName;

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
