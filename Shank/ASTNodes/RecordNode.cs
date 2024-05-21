using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

public class RecordNode : ASTNode
{
    public string Name { get; init; }

    public List<string>? GenericTypeParameterNames { get; init; }

    public string? ParentModuleName { get; init; }

    public List<StatementNode> Members { get; init; }
    public List<VariableNode> Members2 { get; init; }
    public bool IsPublic { get; set; }

    public RecordNode(string name, string moduleName, List<string>? genericTypeParameterNames)
    {
        Name = name;
        ParentModuleName = moduleName;
        GenericTypeParameterNames = genericTypeParameterNames;
        Members = [];
        Members2 = [];
        IsPublic = false;
    }

    public static RecordMemberNode ToMember(StatementNode? sn) =>
        (RecordMemberNode)(
            sn
            ?? throw new ArgumentNullException(nameof(sn), "Expected StatementNode to not be null")
        );

    public RecordMemberNode? GetFromMembersByName(string name) =>
        (RecordMemberNode?)Members.FirstOrDefault(s => ToMember(s).Name.Equals(name), null);

    public RecordMemberNode GetFromMembersByNameSafe(string name) =>
        GetFromMembersByName(name)
        ?? throw new ArgumentOutOfRangeException(
            nameof(name),
            "Member " + name + " not found on record."
        );

    public string GetParentModuleSafe()
    {
        return ParentModuleName ?? throw new Exception("Parent module name of RecordNode is null.");
    }

    public override LLVMValueRef Visit(IVisitor visitor, Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        throw new NotImplementedException();
    }
}