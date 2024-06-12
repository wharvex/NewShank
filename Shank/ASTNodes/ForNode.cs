using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.IRGenerator;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class ForNode : StatementNode
{
    public ForNode(
        VariableUsageNode variable,
        ExpressionNode from,
        ExpressionNode to,
        List<StatementNode> children
    )
    {
        Variable = variable;
        From = from;
        To = to;
        Children = children;
    }

    public VariableUsageNode Variable { get; init; }
    public ExpressionNode From { get; init; }
    public ExpressionNode To { get; init; }
    public List<StatementNode> Children { get; init; }

    public override object[] returnStatementTokens()
    {
        object[] arr = { "For", Variable, From, To, Children };
        return arr;
    }

    public override string ToString()
    {
        return $" For: {Variable} From: {From} To: {To} {Environment.NewLine} {StatementListToString(Children)}";
    }

    // public override void VisitStatement(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     var forStart = context.CurrentFunction.AppendBasicBlock("for.start");
    //     var afterFor = context.CurrentFunction.AppendBasicBlock("for.after");
    //     var forBody = context.CurrentFunction.AppendBasicBlock("for.body");
    //     var forIncremnet = context.CurrentFunction.AppendBasicBlock("for.inc");
    //     // TODO: assign loop variable initial from value
    //     builder.BuildBr(forStart);
    //     builder.PositionAtEnd(forStart);
    //     // we have to compile the to and from in the loop so that the get run each time, we go through the loop
    //     // in case we modify them in the loop
    //
    //
    //
    //     var fromValue = From.Visit(visitor, context, builder, module);
    //     var toValue = To.Visit(visitor, context, builder, module);
    //     var currentIterable = Variable.Visit(visitor, context, builder, module);
    //
    //     // right now we assume, from, to, and the variable are all integers
    //     // in the future we should check and give some error at runtime/compile time if not
    //     // TODO: signed or unsigned comparison
    //     var condition = builder.BuildAnd(
    //         builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, currentIterable, fromValue),
    //         builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, currentIterable, toValue)
    //     );
    //     builder.BuildCondBr(condition, forBody, afterFor);
    //     builder.PositionAtEnd(forBody);
    //     Children.ForEach(c => c.VisitStatement(visitor, context, builder, module));
    //     builder.BuildBr(forIncremnet);
    //     builder.PositionAtEnd(forIncremnet);
    //     // TODO: increment
    //     builder.BuildBr(forStart);
    //     builder.PositionAtEnd(afterFor);
    // }

    public override void Visit(StatementVisitor visit)
    {
        visit.Accept(this);
    }

    public override void Accept(Visitor v) => v.Visit(this);

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
