using System.Diagnostics;
using Shank.AstVisitorsTim;

namespace Shank.ASTNodes;

/// <summary>
/// Abstract base class for the three types of variable usage (plain, member, and index).
/// </summary>
public abstract class VariableUsageNodeTemp : ExpressionNode
{
    /// <summary>
    /// Indicates whether this <see cref="VariableUsageNodeTemp"/> references a global variable.
    /// </summary>
    public bool NewReferencesGlobalVariable { get; set; }

    /// <summary>
    /// Indicates whether this <see cref="VariableUsageNodeTemp"/> is in a function call and
    /// preceded by `var`.
    /// </summary>
    public bool NewIsInFuncCallWithVar { get; set; }

    /// <summary>
    /// Returns the monomorphized name of the variable, considering whether it references a global variable.
    /// </summary>
    /// <returns>
    /// An <see cref="Index"/> representing the monomorphized name of the variable.
    /// </returns>
    public Index NewMonomorphizedName()
    {
        var plain = GetPlain();
        return NewReferencesGlobalVariable
            ? new ModuleIndex(new NamedIndex(plain.Name), plain.ModuleName)
            : new NamedIndex(plain.Name);
    }

    /// <summary>
    /// Gets the <see cref="VariableUsagePlainNode" /> (i.e. the left-most identifier or "root")
    /// from the potentially nested structure of this <see cref="VariableUsageNodeTemp" />.
    /// </summary>
    /// <returns>
    /// The <see cref="VariableUsagePlainNode"/> of this <see cref="VariableUsageNodeTemp" />.
    /// Throws <see cref="UnreachableException"/> if an unknown child of
    /// <see cref="VariableUsageNodeTemp" /> is found during the traversal.
    /// </returns>
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

    /// <summary>
    /// Gets the depth of this <see cref="VariableUsageNodeTemp" />'s potentially nested structure.
    /// A depth of <c>0</c> indicates this <see cref="VariableUsageNodeTemp" /> is a
    /// <see cref="VariableUsagePlainNode" />.
    /// </summary>
    /// <returns>
    /// The depth of this <see cref="VariableUsageNodeTemp" /> as an integer.
    /// Throws <see cref="UnreachableException"/> if an unknown child of
    /// <see cref="VariableUsageNodeTemp" /> is found during the traversal.
    /// </returns>
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

    ///<summary>
    ///     Gets the plain variable usage node and its depth in the hierarchy.
    ///</summary>
    ///<returns>
    ///     A tuple containing the plain variable usage node of type <see cref="VariableUsagePlainNode"/> and its depth as an integer.
    ///     Throws an <see cref="UnreachableException"/> if the variable usage node class hierarchy is altered.
    ///</returns>
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

    ///<summary>
    ///     Gets the variable usage node at the specified depth.
    ///</summary>
    ///<param name="depth">The depth at which to retrieve the variable usage node.</param>
    ///<returns>
    ///     The variable usage node of type <see cref="VariableUsageNodeTemp"/> at the specified depth.
    ///     Throws an <see cref="UnreachableException"/> if the variable usage node class hierarchy is altered.
    ///</returns>
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

    ///<summary>
    ///     Gets the type of the current variable usage node.
    ///     Adapted from Mendel's GetTypeOfVariableUsage.
    ///</summary>
    ///<param name="dexInScope">The dictionary of variable declarations in scope.</param>
    ///<param name="exTyGetter">A function to get the type of an expression node.</param>
    ///<returns>
    ///     The <see cref="Type"/> of the current variable usage node.
    ///     Throws a <see cref="SemanticErrorException"/> if the variable is not in scope, the index is not an integer, or the member is not found.
    ///</returns>
    public Type GetMyType(
        Dictionary<string, VariableDeclarationNode> dexInScope,
        Func<ExpressionNode, Dictionary<string, VariableDeclarationNode>, Type> exTyGetter
    )
    {
        switch (this)
        {
            case VariableUsagePlainNode p:
                if (!dexInScope.TryGetValue(p.Name, out var dec))
                    throw new SemanticErrorException(
                        "Only variables in scope can be used. Found: " + p.Name,
                        p
                    );
                p.NewReferencesGlobalVariable = dec.IsGlobal;
                p.ReferencesGlobalVariable = dec.IsGlobal;
                return dec.Type;
            case VariableUsageIndexNode i:
                var idxTy = exTyGetter(i.Right, dexInScope);
                if (idxTy is not IntegerType)
                    throw new SemanticErrorException(
                        "Only integers can index into arrays. Found: " + idxTy.GetType(),
                        i.Right
                    );

                var maybeArrRet = i.Left.GetMyType(dexInScope, exTyGetter);
                return maybeArrRet switch
                {
                    ArrayType arrRet => arrRet.Inner,
                    ReferenceType(Inner: ArrayType arrRet) => arrRet.Inner,
                    _
                        => throw new SemanticErrorException(
                            "Only arrays can be indexed into. Found: " + maybeArrRet.GetType(),
                            i.Left
                        )
                };

            case VariableUsageMemberNode m:
                return m.Left.GetMyType(dexInScope, exTyGetter) switch
                {
                    InstantiatedType rec => rec.GetMemberSafe(m.Right.Name, m),

                    // This is record deconstruction.
                    // See: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/deconstruct#record-types
                    ReferenceType(InstantiatedType rec) => rec.GetMemberSafe(m.Right.Name, m),

                    var bad
                        => throw new SemanticErrorException(
                            "Only records can be dotted into. Found: " + bad.GetType(),
                            m
                        )
                };

            default:
                throw new UnreachableException();
        }
    }

    // Mendel's version of GetInnerType.
    ///<summary>
    ///     Gets the type of the specified variable usage node.
    ///</summary>
    ///<param name="variableReferenceNode">The variable usage node to get the type for.</param>
    ///<param name="variableDeclarations">The dictionary of variable declarations.</param>
    ///<returns>
    ///     The <see cref="Type"/> of the specified variable usage node.
    ///     Throws a <see cref="SemanticErrorException"/> if the variable is not found, the index is not an integer, or the member is not found.
    ///</returns>
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
                        ReferenceType(ArrayType a) => a.Inner,
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

    ///<summary>
    ///     Gets the inner type of a variable usage node given an outer type.
    ///</summary>
    ///<param name="outerType">The outer type of the variable usage node.</param>
    ///<param name="vDecs">The dictionary of variable declarations.</param>
    ///<returns>
    ///     The inner <see cref="Type"/> of the variable usage node.
    ///     Throws an <see cref="InvalidOperationException"/> if the inner type is not found.
    ///</returns>
    public Type GetInnerType(Type outerType, Dictionary<string, VariableDeclarationNode> vDecs)
    {
        // Get the innermost vun vc in this vun's structure, and vc's depth.
        (VariableUsageNodeTemp vc, var d) = GetPlainAndDepth();

        // Set t to the outermost type in the target's type structure.
        var t = outerType;

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

    ///<summary>
    ///     Accepts a variable usage visitor for processing this node.
    ///</summary>
    ///<param name="visitor">The variable usage visitor to accept.</param>
    public void Accept(IVariableUsageVisitor visitor) => visitor.Visit(this);
}
