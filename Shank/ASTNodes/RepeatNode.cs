using LLVMSharp.Interop;
using Shank.ExprVisitors;

namespace Shank;

public class RepeatNode : StatementNode
{
    public RepeatNode(BooleanExpressionNode exp, List<StatementNode> children)
    {
        Expression = exp;
        Children = children;
    }

    public BooleanExpressionNode Expression { get; init; }
    public List<StatementNode> Children { get; set; }

    public override string ToString()
    {
        return $" REPEAT: {Expression} {StatementListToString(Children)}";
    }
    public override void VisitStatement(IVisitor visitor, Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        var whileBody = module.Context.AppendBasicBlock(context.CurrentFunction, "while.body");
        var whileDone = module.Context.AppendBasicBlock(context.CurrentFunction, "while.done");
        // first execute the body
        builder.BuildBr(whileBody);
        builder.PositionAtEnd(whileBody);
        Children.ForEach(c => c.Visit(visitor, context, builder, module));
        // and then test the condition
        var condition = Expression.Visit(visitor, context, builder, module);
        builder.BuildCondBr(condition, whileBody, whileDone);
        builder.PositionAtEnd(whileDone);

    }
}
