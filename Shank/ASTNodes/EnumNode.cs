using Shank.ExprVisitors;
using Shank.WalkCompliantVisitors;

namespace Shank.ASTNodes;

public class EnumNode : ExpressionNode
{
    public string TypeName => EType.Name;
    public EnumType EType { get; set; }

    public List<string> EnumElements => EType.Variants;
    public string ParentModuleName { get; set; }
    public bool IsPublic { get; set; } = false;

    public List<VariableDeclarationNode> EnumElementsVariables { get; set; }

    public EnumNode(string type, string parentModuleName, List<string> enumElements)
    {
        EType = new EnumType(type, parentModuleName, enumElements);
        ParentModuleName = parentModuleName;

        EnumElementsVariables = EType
            .Variants.Select(
                s => new VariableDeclarationNode(true, EType, s, ParentModuleName, true)
            )
            .ToList();
    }

    public override void Accept(Visitor v) => v.Visit(this);

    public override ASTNode Walk(WalkCompliantVisitor v)
    {
        var ret = v.Visit(this, out var shortCircuit);
        if (shortCircuit)
        {
            return ret;
        }

        EnumElementsVariables = EnumElementsVariables.WalkList(v);

        return v.Final(this);
    }

    public override ASTNode? Walk(SAVisitor v)
    {
        throw new NotImplementedException();
    }
}
