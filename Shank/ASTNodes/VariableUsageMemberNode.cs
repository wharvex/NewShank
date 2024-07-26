using Shank.AstVisitorsTim;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

///<summary>
///     Represents a node for member access within a variable usage in the abstract syntax tree (AST).
///</summary>
///<remarks>
///     This node allows for accessing members of a record or object, and is part of the hierarchy of variable usage nodes.
///</remarks>
///<param name="left">The left-hand side of the member access, typically another variable usage node.</param>
///<param name="right">The right-hand side of the member access, representing the member being accessed.</param>
///
public class VariableUsageMemberNode(VariableUsageNodeTemp left, MemberAccessNode right)
    : VariableUsageNodeTemp
{
    ///<summary>
    ///     Gets the left-hand side of the member access.
    ///</summary>
    ///<value>The left-hand side of the member access, typically another variable usage node.</value>
    public VariableUsageNodeTemp Left { get; init; } = left;

    ///<summary>
    ///     Gets the right-hand side of the member access.
    ///</summary>
    ///<value>The right-hand side of the member access, representing the member being accessed.</value>
    public MemberAccessNode Right { get; init; } = right;

    ///<summary>
    ///     Gets the base name of the variable usage.
    ///</summary>
    ///<returns>
    ///     A string representing the base name of the variable usage.
    ///     Throws an <see cref="InvalidOperationException"/> if the variable usage node class hierarchy has changed or if multidimensional arrays are encountered.
    ///</returns>
    ///<exception cref="InvalidOperationException">
    ///     Thrown if multidimensional arrays are encountered or if the variable usage node class hierarchy has changed.
    ///</exception>
    ///<remarks>
    ///     The method navigates through the hierarchy of variable usage nodes to determine the base name.
    ///</remarks>
    public string GetBaseName() =>
        Left switch
        {
            VariableUsageMemberNode m => m.Right.Name,
            VariableUsagePlainNode p => p.Name,
            VariableUsageIndexNode
                => throw new InvalidOperationException("Multidimensional arrays not allowed."),
            _
                => throw new InvalidOperationException(
                    "VariableUsageNode class hierarchy changed, please update this switch accordingly."
                )
        };

    ///<summary>
    ///     Accepts a generic expression visitor for processing this node.
    ///</summary>
    ///<typeparam name="T">The type of the result returned by the visitor.</typeparam>
    ///<param name="visit">The expression visitor to accept.</param>
    ///<returns>
    ///     The result of the visit operation.
    ///</returns>
    ///<exception cref="NotImplementedException">
    ///     Thrown because this method is not implemented.
    ///</exception>
    ///<remarks>
    ///     This method is part of the visitor pattern implementation and should be overridden in derived classes.
    ///</remarks>
    public override T Accept<T>(ExpressionVisitor<T> visit)
    {
        throw new NotImplementedException();
    }

    ///<summary>
    ///     Walks the node with a semantic analysis visitor, allowing the visitor to process the node.
    ///</summary>
    ///<param name="v">The semantic analysis visitor that processes the node.</param>
    ///<returns>
    ///     The current <see cref="VariableUsageMemberNode"/> instance.
    ///</returns>
    ///<remarks>
    ///     This method is part of the visitor pattern implementation for semantic analysis.
    ///</remarks>
    public override ASTNode? Walk(SAVisitor v)
    {
        return this;
    }
}
