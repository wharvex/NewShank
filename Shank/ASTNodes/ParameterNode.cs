using LLVMSharp.Interop;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

/**
 * ParameterNodes are the arguments passed to a FunctionCallNode.
 * For the parameters of FunctionNodes, see VariableNode.
 */
public class ParameterNode : ASTNode
{
    public ParameterNode(ASTNode constant)
    {
        IsVariable = false;
        Variable = null;
        Constant = constant;
    }

    /**
     * If IsVariable is true, it means the param was preceded by the "var" keyword when it was
     * passed in, meaning it can be changed in the function, and its new value will persist in
     * the caller's scope.
     */
    public ParameterNode(VariableUsageNode variable, bool isVariable)
    {
        IsVariable = isVariable;
        Variable = variable;
        Constant = null;
    }

    public ASTNode? Constant { get; init; }
    public VariableUsageNode? Variable { get; init; }
    public bool IsVariable { get; init; }

    public VariableUsageNode GetVariableSafe() =>
        Variable ?? throw new InvalidOperationException("Expected Variable to not be null");

    public ASTNode GetConstantSafe() =>
        Constant ?? throw new InvalidOperationException("Expected Constant to not be null");

    // Original (bad) approach for implementing overloads. See 'EqualsWrtTypeAndVar' for
    // revised approach.
    public string ToStringForOverload(
        Dictionary<string, ASTNode> imports,
        Dictionary<string, RecordNode> records,
        Dictionary<string, VariableNode> variables
    ) =>
        "_"
        + (IsVariable ? "VAR_" : "")
        + (
            // TODO
            // If GetSpecificType would give us a user-created type, it should be Unknown, and then
            // we need to resolve that to its specific string.
            Variable
                ?.GetSpecificType(records, imports, variables, Variable.Name)
                .ToString()
                .ToUpper()
            ?? Parser
                .GetDataTypeFromConstantNodeType(
                    Constant
                        ?? throw new InvalidOperationException(
                            "A ParameterNode should not have both Variable and Constant set to"
                                + " null."
                        )
                )
                .ToString()
                .ToUpper()
        );

    /// <summary>
    /// Ensure this ParameterNode's invariants hold.
    /// </summary>
    /// <exception cref="InvalidOperationException">If this ParameterNode is in an invalid state
    /// </exception>
    /// <remarks>Author: Tim Gudlewski</remarks>
    private void ValidateState()
    {
        if (
            (Variable is not null && Constant is not null) || (Variable is null && Constant is null)
        )
        {
            throw new InvalidOperationException(
                "This ParameterNode is in an undefined state because Constant and Variable are"
                    + " both null, or both non-null."
            );
        }

        if (Constant is not null && IsVariable)
        {
            throw new InvalidOperationException(
                "This ParameterNode is in an undefined state because its value is stored in"
                    + " Constant, but it is also 'var'."
            );
        }
    }

    public bool ValueIsStoredInVariable()
    {
        ValidateState();
        return Variable is not null;
    }

    public bool EqualsWrtTypeAndVar(
        VariableNode givenVariable,
        Dictionary<string, VariableNode> variablesInScope
    )
    {
        return ValueIsStoredInVariable()
            ? VarStatusEquals(givenVariable) && VariableTypeEquals(givenVariable, variablesInScope)
            : ConstantTypeEquals(givenVariable);
    }

    public bool VarStatusEquals(VariableNode givenVariable)
    {
        return IsVariable != givenVariable.IsConstant;
    }

    public bool ConstantTypeEquals(VariableNode givenVariable)
    {
        return givenVariable.Type == GetConstantType();
    }

    public VariableNode.DataType GetConstantType()
    {
        return Parser.GetDataTypeFromConstantNodeType(GetConstantSafe());
    }

    public bool VariableTypeEquals(
        VariableNode givenVariable,
        Dictionary<string, VariableNode> variablesInScope
    )
    {
        // Check if the types are unequal.
        if (givenVariable.Type != GetVariableType(variablesInScope))
        {
            return false;
        }

        // The types are equal. If they're not Unknown, no further action needed.
        if (givenVariable.Type != VariableNode.DataType.Unknown)
        {
            return true;
        }

        // The types are equal and Unknown. Check their UnknownTypes (string comparison).
        return givenVariable
            .GetUnknownTypeSafe()
            .Equals(GetVariableDeclarationSafe(variablesInScope).GetUnknownTypeSafe());
    }

    public VariableNode GetVariableDeclarationSafe(
        Dictionary<string, VariableNode> variablesInScope
    )
    {
        if (variablesInScope.TryGetValue(GetVariableSafe().Name, out var varDec))
        {
            return varDec;
        }

        throw new InvalidOperationException("Could not find given variable in scope");
    }

    public VariableNode.DataType GetVariableType(Dictionary<string, VariableNode> variablesInScope)
    {
        return GetVariableDeclarationSafe(variablesInScope).Type;
    }

    public override string ToString()
    {
        if (Variable != null)
            return $"   {(IsVariable ? "var " : "")} {Variable.Name}";
        else
            return $"   {Constant}";
    }

    public override LLVMValueRef Visit(
        Visitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }
}
