using System.Diagnostics;
using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

///<summary>
///     Represents a 'while' loop node in the abstract syntax tree (AST).
///</summary>
///
public class WhileNode : StatementNode
{
    ///<summary>
    ///     Initializes a new instance of the <see cref="WhileNode"/> class with the specified expression and child statements.
    ///</summary>
    ///<param name="exp">The condition expression for the 'while' loop.</param>
    ///<param name="children">The list of statements to be executed within the loop.</param>

    public WhileNode(ExpressionNode exp, List<StatementNode> children)
    {
        Expression = exp;
        Children = children;
    }

    ///<summary>
    ///     Copy constructor for creating a new instance of the <see cref="WhileNode"/> class with modified children.
    ///</summary>
    ///<param name="copy">The original <see cref="WhileNode"/> instance to copy.</param>
    ///<param name="children">The list of statements to be executed within the loop.</param>

    public WhileNode(WhileNode copy, List<StatementNode> children)
    {
        Children = children;
        FileName = copy.FileName;
        Line = copy.Line;
        Expression = copy.Expression;
    }

    ///<summary>
    ///     Gets or sets the condition expression of the 'while' loop.
    ///</summary>
    ///
    public ExpressionNode Expression { get; set; }

    ///<summary>
    ///     Gets or sets the list of statements to be executed within the loop.
    ///</summary>

    public List<StatementNode> Children { get; set; }

    ///<summary>
    ///     Returns an array of tokens representing the 'while' loop statement.
    ///</summary>
    ///<returns>
    ///     An array containing the tokens of the 'while' loop statement.
    ///     Throws a <see cref="SemanticErrorException"/> if the expression type is not supported.
    ///</returns>

    public override object[] returnStatementTokens()
    {
        // object[] arr;
        // if(Expression is BooleanExpressionNode booleanExpressionNode)
        //     arr = new object[] { "WHILE", booleanExpressionNode.Left, booleanExpressionNode.Op, booleanExpressionNode.Right, Children };
        return Expression switch
        {
            BooleanExpressionNode booleanExpressionNode
                =>
                [
                    "WHILE",
                    booleanExpressionNode.Left,
                    booleanExpressionNode.Op,
                    booleanExpressionNode.Right,
                    Children
                ],
            BoolNode boolNode => ["WHILE", boolNode, Children],
            VariableUsageNodeTemp variableUsageNodeTemp
                => ["WHILE", variableUsageNodeTemp, Children],
            _ => throw new SemanticErrorException("Wrong tokens in while loop.", Expression)
        };

        // return arr;
    }

    ///<summary>
    ///     Returns a string representation of the 'while' loop node.
    ///</summary>
    ///<returns>A string representing the 'while' loop node.</returns>

    public override string ToString()
    {
        return $" WHILE: {Expression} {StatementListToString(Children)}";
    }

    // public override void VisitStatement(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     // since the condition checking happens first we need another block (unlike in repeat)
    //     // when the condition happens after the body, we can jump immediatly back to the body, but in a while loop
    //     // where we need to check the condition first, we can't just jump back to the start (the condition checking),
    //     // so we need an extra block to jump to
    //     visitor.Visit(this);
    //     // var whileCond = context.CurrentFunction.AppendBasicBlock("while.cond");
    //     // var whileBody = context.CurrentFunction.AppendBasicBlock("while.body");
    //     // var whileDone = context.CurrentFunction.AppendBasicBlock("while.done");
    //     // builder.BuildBr(whileCond);
    //     // builder.PositionAtEnd(whileCond);
    //     // var condition = Expression.Visit(visitor, context, builder, module);
    //     // builder.BuildCondBr(condition, whileBody, whileDone);
    //     // builder.PositionAtEnd(whileBody);
    //     // Children.ForEach(c => c.VisitStatement(visitor, context, builder, module));
    //     // builder.BuildBr(whileCond);
    //     // builder.PositionAtEnd(whileDone);
    // }


    ///<summary>
    ///     Accepts a statement visitor for processing this node.
    ///</summary>
    ///<param name="visit">The statement visitor to accept.</param>

    public override void Visit(StatementVisitor visit)
    {
        visit.Accept(this);
    }

    ///<summary>
    ///     Accepts a generic visitor for processing this node.
    ///</summary>
    ///<param name="v">The generic visitor to accept.</param>

    public override void Accept(Visitor v) => v.Visit(this);

    ///<summary>
    ///     Walks the node with a semantic analysis visitor, allowing the visitor
    ///     to process and potentially modify the node and its children.
    ///</summary>
    ///<param name="v">The semantic analysis visitor that processes the node.</param>
    ///<returns>
    ///     <para>The resulting AST node if changes are made.</para>
    ///     <para>Returns <c>null</c> if no changes are made.</para>
    ///</returns>

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        Expression = (ExpressionNode)(Expression.Walk(v) ?? Expression);

        for (var index = 0; index < Children.Count; index++)
        {
            Children[index] = (StatementNode)(Children[index].Walk(v) ?? Children[index]);
        }

        return v.PostWalk(this);
    }
}
