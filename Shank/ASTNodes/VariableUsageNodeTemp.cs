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

    public VariableUsageNodeTemp GetVunAtDepth(int depth)
    {
        var d = 0;
        var vc = this;
        while (d < depth)
        {
            vc = vc switch
            {
                VariableUsageIndexNode i => i.Left,
                VariableUsageMemberNode m => m.Left,
                VariableUsagePlainNode p => p,
                _
                    => throw new UnreachableException(
                        "VUN class hierarchy was altered; please update this switch accordingly."
                    )
            };
            d++;
        }

        return vc;
    }

    public Type GetInnerType(Type outerType, Dictionary<string, VariableDeclarationNode> vDecs)
    {
        // Get the innermost vun vc in this vun's structure, and vc's depth.
        (VariableUsageNodeTemp vc, var d) = GetPlainAndDepth();

        // Set t to the outermost type in the target's type structure.
        var t = outerType;

        if (Line == 9)
        {
            OutputHelper.DebugPrintJson(this, "head_data_var");
            OutputHelper.DebugPrintJson(t, "head_data_ty");
        }

        // Ensure vun and type agree internally, and end up with t set to the type which the
        // assignment's "expression" (it's RHS) should be.
        while (true)
        {
            // Back up through the vun structure (backward recursion).
            if (--d < 0)
                break;
            vc = GetVunAtDepth(d);

            // Ensure vc and t agree internally.
            vc.Accept(new VunVsTypeCheckingVisitor(t, vDecs));

            // Set t to its own inner type (forward recursion).
            var itVis = new InnerTypeGettingVisitor(vc);
            t.Accept(itVis);
            t = itVis.InnerType;

            // itVis.InnerType is null if there are no more inner types (this shouldn't happen).
            if (t is null)
                break;
        }

        return t ?? throw new InvalidOperationException();
    }

    public void Accept(IVariableUsageVisitor visitor) => visitor.Visit(this);
}
