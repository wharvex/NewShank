using System.Diagnostics;
using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class WhileNode : StatementNode
{
    public WhileNode(ExpressionNode exp, List<StatementNode> children)
    {
        Expression = exp;
        Children = children;
    }

    // Copy constructor for monomorphization
    public WhileNode(WhileNode copy, List<StatementNode> children)
    {
        Children = children;
        FileName = copy.FileName;
        Line = copy.Line;
        Expression = copy.Expression;
    }

    public ExpressionNode Expression { get; set; }
    public List<StatementNode> Children { get; set; }

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

    public override void Visit(StatementVisitor visit)
    {
        visit.Accept(this);
    }

    public override void Accept(Visitor v) => v.Visit(this);

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
