using Shank.ExprVisitors;
using Shank.WalkCompliantVisitors;

namespace Shank.ASTNodes;

/// <summary>
/// Models an assignment statement.
/// </summary>
public class AssignmentNode : StatementNode
{
    ///<summary>
    /// Initializes a new instance of the <see cref="AssignmentNode"/> class.
    ///</summary>
    ///<param name="target">The target variable to which the expression is assigned.</param>
    ///<param name="expression">The expression assigned to the target variable.</param>

    public AssignmentNode(VariableUsagePlainNode target, ExpressionNode expression)
    {
        Target = target;
        Expression = expression;
        NewTarget = new VariableUsagePlainNode("emptyNewTarget", "default");
    }

    ///<summary>
    /// Initializes a new instance of the <see cref="AssignmentNode"/> class.
    ///</summary>
    ///<param name="target">The target variable (temporary).</param>
    ///<param name="expression">The expression assigned to the target variable.</param>
    ///<param name="isVuopReroute">A boolean flag for rerouting.</param>


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
    ///
    public VariableUsagePlainNode Target { get; set; }

    ///<summary>
    /// The temporary target variable.
    ///</summary>

    public VariableUsageNodeTemp NewTarget { get; set; }

    /// <summary>
    /// The expression assigned to the target variable (RHS of the :=).
    /// </summary>
    public ExpressionNode Expression { get; set; }

    ///<summary>
    /// Returns an array of statement tokens.
    ///</summary>
    ///<returns>An array containing the statement tokens.</returns>

    public override object[] returnStatementTokens()
    {
        object[] arr = { "", Target.Name, Expression.ToString() };

        return arr;
    }

    public override void Accept(Visitor v) => v.Visit(this);

    public override string ToString()
    {
        return $"{Target} assigned as {Expression}";
    }

    ///<summary>
    /// Walks the node with a compliant visitor.
    ///</summary>
    ///<param name="v">The visitor to walk with.</param>
    ///<returns>The resulting AST node.</returns>

    public override ASTNode Walk(WalkCompliantVisitor v)
    {
        var ret = v.Visit(this, out var shortCircuit);
        if (shortCircuit)
        {
            return ret;
        }

        if (v is ExpressionTypingVisitor etv && etv.GetVuopTestFlag())
        {
            NewTarget = (VariableUsageNodeTemp)NewTarget.Walk(etv);
        }
        else
        {
            Target = (VariableUsagePlainNode)Target.Walk(v);
        }

        Expression = (ExpressionNode)Expression.Walk(v);

        return v.Final(this);
    }

    ///<summary>
    /// Walks the node with a semantic analysis visitor.
    ///</summary>
    ///<param name="v">The visitor to walk with.</param>
    ///<returns>The resulting AST node if changes are made, or null if no changes are made.</returns>

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
