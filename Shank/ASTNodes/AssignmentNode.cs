using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

/// <summary>
/// Models an assignment statement.
/// </summary>
public class AssignmentNode : StatementNode
{
    public AssignmentNode(VariableUsageNode target, ExpressionNode expression)
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
    public ExpressionNode Expression { get; init; }

    public override object[] returnStatementTokens()
    {
        object[] arr = { "", Target.Name, Expression.ToString() };

        return arr;
    }

    // public override void VisitStatement(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     // visitor.Visit(this);
    //     // var llvmValue = context.GetVaraible(Target.Name);
    //     // if (!llvmValue.IsMutable)
    //     // {
    //     //     throw new Exception($"tried to mutate non mutable variable {Target.Name}");
    //     // }
    //     // builder.BuildStore(Expression.Visit(visitor, context, builder, module), llvmValue.ValueRef);
    // }

    public override void Accept(Visitor v) => v.Visit(this);

    public override string ToString()
    {
        return $"{Target} assigned as {Expression}";
    }

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
