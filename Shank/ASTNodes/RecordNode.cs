using Shank.ExprVisitors;
using Shank.WalkCompliantVisitors;

namespace Shank.ASTNodes;

public class RecordNode(
    string name,
    string moduleName,
    List<VariableDeclarationNode> members,
    List<string>? genericTypeParameterNames
) : ASTNode
{
    public string Name => Type.Name;

    public List<string> GenericTypeParameterNames => Type.Generics;

    public RecordType Type =
        new(
            name,
            moduleName,
            members.Select(member => (member.GetNameSafe(), NewType: member.Type)).ToDictionary(),
            genericTypeParameterNames ?? []
        );

    public string ParentModuleName { get; init; } = moduleName;

    // why not just rely on the list of variable nodes passed into the constructor? because during semantic analysis the underlying type can be changed as we figure out more about
    public List<VariableDeclarationNode> Members =>
        Type.Fields.Select(
            (field, index) =>
                new VariableDeclarationNode()
                {
                    Name = field.Key,
                    Type = field.Value,
                    Line = Line + index + 1
                }
        )
            .ToList();
    public bool IsPublic { get; set; } = false;

    public string GetParentModuleSafe()
    {
        return ParentModuleName ?? throw new Exception("Parent module name of RecordNode is null.");
    }

    public override string ToString() => Name;

    public override void Accept(Visitor v) => v.Visit(this);

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        for (var index = 0; index < members.Count; index++)
        {
            members[index] = (VariableDeclarationNode)(members[index].Walk(v) ?? members[index]);
        }

        return v.PostWalk(this);
    }

    public override ASTNode Walk(WalkCompliantVisitor v)
    {
        var ret = v.Visit(this, out var shortCircuit);

        if (shortCircuit)
            return ret;

        return v.Final(this);
    }
}
