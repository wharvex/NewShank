using LLVMSharp.Interop;
using Shank.ExprVisitors;

namespace Shank;

public class WhileNode : StatementNode
{
    public WhileNode(BooleanExpressionNode exp, List<StatementNode> children)
    {
        Expression = exp;
        Children = children;
    }

    public BooleanExpressionNode Expression { get; init; }
    public List<StatementNode> Children { get; set; }

    public override object[] returnStatementTokens()
    {
        object[] arr = { "WHILE", Expression.Left, Expression.Op, Expression.Right, Children };

        return arr;
    }

    public override string ToString()
    {
        return $" WHILE: {Expression} {StatementListToString(Children)}";
    }

    public void VisitStatement(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        // since the condition checking happens first we need another block (unlike in repeat)
        // when the condition happens after the body, we can jump immediatly back to the body, but in a while loop
        // where we need to check the condition first, we can't just jump back to the start (the condition checking),
        // so we need an extra block to jump to
        var whileCond = module.Context.AppendBasicBlock(context.CurrentFunction, "while.cond");
        var whileBody = module.Context.AppendBasicBlock(context.CurrentFunction, "while.body");
        var whileDone = module.Context.AppendBasicBlock(context.CurrentFunction, "while.done");
        builder.BuildBr(whileCond);
        builder.PositionAtEnd(whileCond);
        var condition = Expression.Visit(new BoolExprVisitor(), context, builder, module);
        builder.BuildCondBr(condition, whileBody, whileDone);
        builder.PositionAtEnd(whileBody);
        Children.ForEach(c => c.VisitStatement(context, builder, module));
        builder.BuildBr(whileCond);
        builder.PositionAtEnd(whileDone);
    }
}
