using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class RepeatNode : StatementNode
{
    public RepeatNode(ExpressionNode exp, List<StatementNode> children)
    {
        Expression = exp;
        Children = children;
    }

    // Copy constructor for monomorphization
    public RepeatNode(RepeatNode copy, List<StatementNode> children)
    {
        Children = children;
        FileName = copy.FileName;
        Line = copy.Line;
        Expression = copy.Expression;
    }

    public ExpressionNode Expression { get; set; }
    public List<StatementNode> Children { get; set; }

    public override string ToString()
    {
        return $" REPEAT: {Expression} {StatementListToString(Children)}";
    }

    // public override void VisitStatement(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     var whileBody = context.CurrentFunction.AppendBasicBlock("while.body");
    //     var whileDone = context.CurrentFunction.AppendBasicBlock("while.done");
    //     // first execute the body
    //     builder.BuildBr(whileBody);
    //     builder.PositionAtEnd(whileBody);
    //     Children.ForEach(c => c.VisitStatement(visitor, context, builder, module));
    //     // and then test the condition
    //     var condition = Expression.Visit(visitor, context, builder, module);
    //     builder.BuildCondBr(condition, whileBody, whileDone);
    //     builder.PositionAtEnd(whileDone);
    // }

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
