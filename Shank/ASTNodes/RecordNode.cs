using Shank.ExprVisitors;

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

    // public VariableNode? GetFromMembersByName(string name) =>
    //     Members2.FirstOrDefault(v => v?.Name?.Equals(name) ?? false, null);
    //
    // public VariableNode GetFromMembersByNameSafe(string name) =>
    // GetFromMembersByName(name)
    // ?? throw new ArgumentOutOfRangeException(
    //     nameof(name),
    //     "Member " + name + " not found on record."
    // );

    public string GetParentModuleSafe()
    {
        return ParentModuleName ?? throw new Exception("Parent module name of RecordNode is null.");
    }

    public void VisitProto(VisitPrototype v)
    {
        v.Accept(this);
    }

    public void Visit(StatementVisitor visit)
    {
        visit.Accept(this);
    }

    public override string ToString() => Name;

    public override void Accept<T>(StatementVisitor v)
    {
        throw new NotImplementedException();
    }

    public override void Accept(Visitor v) => v.Visit(this);
}
