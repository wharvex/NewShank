using Shank.ASTNodes;

namespace Shank;

public class TypeUsage
{
    public VariableNode.DataType Type { get; init; }
    public string? UserDefinedName { get; init; }

    public TypeUsage(VariableNode.DataType type, string? userDefinedName = null)
    {
        Type = type;
        UserDefinedName = userDefinedName;
    }
}
