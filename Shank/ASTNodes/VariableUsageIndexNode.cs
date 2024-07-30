using Shank.AstVisitorsTim;

namespace Shank.ASTNodes;

///<summary>
///     Represents a node for indexed variable usage in the abstract syntax tree (AST).
///</summary>
///<remarks>
///     This node allows for accessing elements within an array-like structure and is part of the hierarchy of variable usage nodes.
///</remarks>
///<param name="left">The left-hand side of the index operation, typically another variable usage node.</param>
///<param name="right">The right-hand side of the index operation, representing the index expression.</param>
///<exception cref="InvalidOperationException">
///     Thrown if multidimensional arrays are encountered or if the variable usage node class hierarchy has changed.
///</exception>

public class VariableUsageIndexNode(VariableUsageNodeTemp left, ExpressionNode right)
    : VariableUsageNodeTemp
{
    ///<summary>
    ///     Gets the left-hand side of the index operation.
    ///</summary>
    ///<value>The left-hand side of the index operation, typically another variable usage node.</value>
    ///<exception cref="InvalidOperationException">
    ///     Thrown if multidimensional arrays are encountered or if the variable usage node class hierarchy has changed.
    ///</exception>
    public VariableUsageNodeTemp Left { get; init; } = ValidateLeft(left);

    ///<summary>
    ///     Gets the right-hand side of the index operation.
    ///</summary>
    ///<value>The right-hand side of the index operation, representing the index expression.</value>
    public ExpressionNode Right { get; init; } = right;

    ///<summary>
    ///     Validates the left-hand side of the index operation to ensure it is a valid node type.
    ///</summary>
    ///<param name="v">The left-hand side variable usage node to validate.</param>
    ///<returns>The validated variable usage node.</returns>
    ///<exception cref="InvalidOperationException">
    ///     Thrown if multidimensional arrays are encountered or if the variable usage node class hierarchy has changed.
    ///</exception>
    ///<remarks>
    ///     This method ensures that the left-hand side of the index operation is either a member node or a plain node, but not another index node.
    ///</remarks>
    public static VariableUsageNodeTemp ValidateLeft(VariableUsageNodeTemp v) =>
        v switch
        {
            VariableUsageMemberNode => v,
            VariableUsagePlainNode => v,
            VariableUsageIndexNode
                => throw new InvalidOperationException("Multidimensional arrays not allowed"),
            _
                => throw new InvalidOperationException(
                    "VariableUsageNode class hierarchy changed, please update this switch accordingly."
                )
        };

    ///<summary>
    ///     Gets the base name of the variable usage.
    ///</summary>
    ///<value>A string representing the base name of the variable usage.</value>
    ///<exception cref="InvalidOperationException">
    ///     Thrown if multidimensional arrays are encountered or if the variable usage node class hierarchy has changed.
    ///</exception>
    ///<remarks>
    ///     The property navigates through the hierarchy of variable usage nodes to determine the base name.
    ///</remarks>
    public string BaseName =>
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
    ///     Walks the node with a semantic analysis visitor, allowing the visitor to process the node.
    ///</summary>
    ///<param name="v">The semantic analysis visitor that processes the node.</param>
    ///<returns>The current <see cref="VariableUsageIndexNode"/> instance.</returns>
    ///<remarks>
    ///     This method is part of the visitor pattern implementation for semantic analysis.
    ///</remarks>
    public override ASTNode? Walk(SAVisitor v)
    {
        return this;
    }

    ///<summary>
    ///     Accepts a variable usage index visitor for processing this node.
    ///</summary>
    ///<param name="visitor">The variable usage index visitor to accept.</param>
    ///<remarks>
    ///     This method is part of the visitor pattern implementation for variable usage index nodes.
    ///</remarks>
    public void Accept(IVariableUsageIndexVisitor visitor) => visitor.Visit(this);
}
