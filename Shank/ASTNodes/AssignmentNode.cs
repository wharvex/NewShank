using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;
using Shank.WalkCompliantVisitors;

namespace Shank.ASTNodes;

/// <summary>
/// Models an assignment statement.
/// </summary>
public class AssignmentNode : StatementNode
{
    public AssignmentNode(VariableUsagePlainNode target, ExpressionNode expression)
    {
        Target = target;
        Expression = expression;
        NewTarget = new VariableUsagePlainNode("emptyNewTarget", "default");
    }

    public AssignmentNode(
        VariableUsageNodeTemp target,
        ExpressionNode expression,
        bool isVuopReroute
    )
    {
        NewTarget = target;
        Expression = expression;
        Target = new VariableUsagePlainNode("emptyOldTarget", "default");
    }

    /// <summary>
    /// The target variable to which the expression is assigned (LHS of the :=).
    /// </summary>
    public VariableUsagePlainNode Target { get; set; }

    public VariableUsageNodeTemp NewTarget { get; set; }

    /// <summary>
    /// The expression assigned to the target variable (RHS of the :=).
    /// </summary>
    public ExpressionNode Expression { get; set; }

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

    public override ASTNode Walk(WalkCompliantVisitor v)
    {
        var ret = v.Visit(this);
        if (ret is not null)
        {
            return ret;
        }

        NewTarget = (VariableUsageNodeTemp)NewTarget.Walk(v);
        Expression = (ExpressionNode)Expression.Walk(v);

        ret = v.Final(this);
        return ret ?? this;
    }

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        Target = (VariableUsagePlainNode)(Target.Walk(v) ?? Target);

        Expression = (ExpressionNode)(Expression.Walk(v) ?? Expression);

        return v.PostWalk(this);
    }
}
