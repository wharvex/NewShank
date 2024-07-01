using System.Diagnostics;
using Shank.AstVisitorsTim;

namespace Shank.ASTNodes;

public abstract class VariableUsageNodeTemp : ExpressionNode
{
    public VariableUsagePlainNode GetPlain()
    {
        var ret = this;
        while (ret is not VariableUsagePlainNode)
        {
            ret = ret switch
            {
                VariableUsageIndexNode i => i.Left,
                VariableUsageMemberNode m => m.Left,
                _
                    => throw new UnreachableException(
                        "VUN class hierarchy was altered; please update this switch accordingly."
                    )
            };
        }

        return (VariableUsagePlainNode)ret;
    }

    public int GetDepth()
    {
        var vc = this;
        var ret = 0;
        while (vc is not VariableUsagePlainNode)
        {
            ret++;
            vc = vc switch
            {
                VariableUsageIndexNode i => i.Left,
                VariableUsageMemberNode m => m.Left,
                _ => throw new UnreachableException()
            };
        }

        return ret;
    }

    public (VariableUsagePlainNode, int) GetPlainAndDepth()
    {
        var plainRet = this;
        var intRet = 0;
        while (plainRet is not VariableUsagePlainNode)
        {
            intRet++;
            plainRet = plainRet switch
            {
                VariableUsageIndexNode i => i.Left,
                VariableUsageMemberNode m => m.Left,
                _
                    => throw new UnreachableException(
                        "VUN class hierarchy was altered; please update this switch accordingly."
                    )
            };
        }

        return ((VariableUsagePlainNode)plainRet, intRet);
    }

    public void Accept(IVariableUsageVisitor visitor) => visitor.Visit(this);
}
