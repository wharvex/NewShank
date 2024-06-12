using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.IRGenerator;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class RecordNode : StatementNode
{
    public string Name { get; init; }

    // public List<string>? GenericTypeParameterNames { get; init; }

    public RecordType NewType;

    public string ParentModuleName { get; init; }

    // public List<StatementNode> Members { get; init; }
    // public List<VariableNode> Members2 { get; init; }
    public bool IsPublic { get; set; }

    public RecordNode(
        string name,
        string moduleName,
        List<VariableNode> members,
        List<string>? genericTypeParameterNames
    )
    {
        Name = name;
        ParentModuleName = moduleName;
        NewType = new RecordType(
            name,
            members.Select(member => (member.Name, NewType: member.Type)).ToDictionary(),
            genericTypeParameterNames ?? []
        );
        // GenericTypeParameterNames = genericTypeParameterNames;
        // Members = [];
        // Members2 = [];
        IsPublic = false;
    }

    public static RecordMemberNode ToMember(StatementNode? sn) =>
        (RecordMemberNode)(
            sn
            ?? throw new ArgumentNullException(nameof(sn), "Expected StatementNode to not be null")
        );

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

    public override void Visit(StatementVisitor visit)
    {
        visit.Accept(this);
    }

    public override string ToString() => Name;

    public override void Accept(Visitor v) => v.Visit(this);

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
