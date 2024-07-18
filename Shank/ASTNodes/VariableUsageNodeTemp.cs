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

    public static Type GetTypeOfVariableUsage(
        VariableUsageNodeTemp variableReferenceNode,
        Dictionary<string, VariableDeclarationNode> variableDeclarations
    )
    {
        return variableReferenceNode switch
        {
            VariableUsagePlainNode v
                => (
                    variableDeclarations.GetValueOrDefault(v.Name)
                    ?? throw new SemanticErrorException(
                        $"Variable {v.Name} not found",
                        variableReferenceNode
                    )
                ).Type,
            VariableUsageIndexNode iv
                => SemanticAnalysis.GetTypeOfExpression(iv.Right, variableDeclarations)
                is IntegerType
                    ? GetTypeOfVariableUsage(iv.Left, variableDeclarations) switch
                    {
                        ArrayType a => a.Inner,
                        var notAArrayType
                            => throw new SemanticErrorException(
                                $"cannot index non array type {notAArrayType}",
                                variableReferenceNode
                            )
                    }
                    : throw new SemanticErrorException(
                        $"cannot index into array with non integer value {iv.Right}",
                        variableReferenceNode
                    ),
            VariableUsageMemberNode mv
                => GetTypeOfVariableUsage(mv.Left, variableDeclarations) switch
                {
                    InstantiatedType record
                        => record.Inner.GetMember(mv.Right.Name, record.InstantiatedGenerics)
                            ?? throw new SemanticErrorException(
                                $"member {mv.Right.Name} is not declared for {record}"
                            ),
                    ReferenceType(InstantiatedType record)
                        => record.Inner.GetMember(mv.Right.Name, record.InstantiatedGenerics)
                            ?? throw new SemanticErrorException(
                                $"member {mv.Right.Name} is not declared for {record}"
                            ),
                    var notARecord
                        => throw new SemanticErrorException(
                            $"cannot access non record type {notARecord}, with member {mv.Right.Name}",
                            variableReferenceNode
                        ),
                },
        };
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
