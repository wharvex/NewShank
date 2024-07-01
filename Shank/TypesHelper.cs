using System.Diagnostics;
using Shank.ASTNodes;

namespace Shank;

public class TypesHelper
{
    public static Type? GetInnerType(Type t, VariableUsageNodeTemp vun)
    {
        switch (t)
        {
            case InstantiatedType it:
                if (vun is VariableUsageMemberNode m)
                {
                    return it.GetMemberSafe(m.Right.Name, m);
                }
                throw new SemanticErrorException("Cannot dot into a " + vun.GetType(), vun);

            case ArrayType at:
                return at.Inner;
            default:
                return null;
        }
    }
}
