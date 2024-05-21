using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank;

/// <summary>
/// Models an assignment statement.
/// </summary>
public class AssignmentNode : StatementNode
{
    public AssignmentNode(VariableReferenceNode target, ASTNode expression)
    {
        Target = target;
        Expression = expression;
    }

    /// <summary>
    /// The target variable to which the expression is assigned (LHS of the :=).
    /// </summary>
    public VariableReferenceNode Target { get; init; }

    /// <summary>
    /// The expression assigned to the target variable (RHS of the :=).
    /// </summary>
    public ASTNode Expression { get; init; }

    public override object[] returnStatementTokens()
    {
        object[] arr = { "", Target.Name, Expression.ToString() };

        return arr;
    }

    

    public override string ToString()
    {
        return $"{Target} := {Expression}";
    }
}