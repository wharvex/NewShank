using System.Text;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class ElseNode : IfNode
{
    // code for generating llvm ir is done currently within IfNode
    public ElseNode(List<StatementNode> children)
        : base(children) { }

    public override string ToString()
    {
        var linePrefix = $"else, line {Line}, ";
        var b = new StringBuilder();
        b.AppendLine(linePrefix + "statements begin");
        Children.ForEach(c => b.AppendLine(c.ToString()));
        b.AppendLine(linePrefix + "statements end");
        return b.ToString();
    }

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
