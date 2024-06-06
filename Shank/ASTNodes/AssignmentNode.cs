using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

/// <summary>
/// Models an assignment statement.
/// </summary>
public class AssignmentNode : StatementNode
{
    public AssignmentNode(VariableUsageNode target, ASTNode expression)
    {
        Target = target;
        Expression = expression;
    }

    /// <summary>
    /// The target variable to which the expression is assigned (LHS of the :=).
    /// </summary>
    public VariableUsageNode Target { get; init; }

    /// <summary>
    /// The expression assigned to the target variable (RHS of the :=).
    /// </summary>
    public ASTNode Expression { get; init; }

    public override object[] returnStatementTokens()
    {
        object[] arr = { "", Target.Name, Expression.ToString() };

        return arr;
    }

    public override void VisitStatement(
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        builder.BuildStore(
            Expression.Visit(
                context.GetExprFromType(context.GetVaraible(Target.Name).TypeRef),
                context,
                builder,
                module
            ),
            context.GetVaraible(Target.Name).ValueRef
        );
    }

    public override string ToString()
    {
        return $"{Target} := {Expression}";
    }
}
