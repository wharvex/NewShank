using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

public class RecordNode : ASTNode
{
    public string Name { get; init; }

    public List<string>? GenericTypeParameterNames { get; init; }

    public string ParentModuleName { get; init; }

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

    public VariableNode? GetFromMembersByName(string name) =>
        Members2.FirstOrDefault(v => v?.Name?.Equals(name) ?? false, null);

    public VariableNode GetFromMembersByNameSafe(string name) =>
        GetFromMembersByName(name)
        ?? throw new ArgumentOutOfRangeException(
            nameof(name),
            "Member " + name + " not found on record."
        );

    public string GetParentModuleSafe()
    {
        return ParentModuleName ?? throw new Exception("Parent module name of RecordNode is null.");
    }

    public override LLVMValueRef Visit(
        LLVMVisitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }
}
