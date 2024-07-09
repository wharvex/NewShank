using Shank.ASTNodes;
using Shank.WalkCompliantVisitors;

namespace Shank;

public static class IdtExtensions
{
    public delegate int IntResolver(
        ASTNode node,
        Dictionary<string, InterpreterDataType> variables
    );

    public static InterpreterDataType? GetInnerIdt(
        this InterpreterDataType idt,
        VariableUsageNodeTemp vun,
        IntResolver? resolveInt,
        Dictionary<string, InterpreterDataType>? variables
    )
    {
        switch (idt)
        {
            case RecordDataType rdt:
                if (vun is VariableUsageMemberNode m)
                {
                    return (InterpreterDataType)rdt.Value[m.Right.Name];
                }

                throw new InvalidOperationException(
                    "Wrong lookup node provided for RecordDataType."
                );

            case ArrayDataType adt:
                if (vun is not VariableUsageIndexNode i)
                    throw new InvalidOperationException();

                if (resolveInt is null || variables is null)
                    throw new InvalidOperationException();

                return (InterpreterDataType)adt.Value[resolveInt(i.Right, variables)];
            default:
                return null;
        }
    }
}
