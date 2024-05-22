using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

public class ForNode : StatementNode
{
    public ForNode(
        VariableReferenceNode variable,
        ASTNode from,
        ASTNode to,
        List<StatementNode> children
    )
    {
        Variable = variable;
        From = from;
        To = to;
        Children = children;
    }

    public VariableReferenceNode Variable { get; init; }
    public ASTNode From { get; init; }
    public ASTNode To { get; init; }
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

    public  void VisitStatement(
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        var forStart = module.Context.AppendBasicBlock(context.CurrentFunction, "for.start");
        var afterFor = module.Context.AppendBasicBlock(context.CurrentFunction, "for.after");
        var forBody = module.Context.AppendBasicBlock(context.CurrentFunction, "for.body");
        var forIncremnet = module.Context.AppendBasicBlock(context.CurrentFunction, "for.inc");
        // TODO: assign loop variable initial from value
        builder.BuildBr(forStart);
        builder.PositionAtEnd(forStart);
        // we have to compile the to and from in the loop so that the get run each time, we go through the loop
        // in case we modify them in the loop

        
        
        var fromValue = From.Visit(new IntegerExprVisitor(), context, builder, module);
        var toValue = To.Visit(new IntegerExprVisitor(), context, builder, module);
        var currentIterable = Variable.Visit(new IntegerExprVisitor(), context, builder, module);
        
        // right now we assume, from, to, and the variable are all integers
        // in the future we should check and give some error at runtime/compile time if not
        // TODO: signed or unsigned comparison
        var condition = builder.BuildAnd(
            builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, currentIterable, fromValue),
            builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, currentIterable, toValue)
        );
        builder.BuildCondBr(condition, forBody, afterFor);
        builder.PositionAtEnd(forBody);
        Children.ForEach(c => c.VisitStatement( context, builder, module));
        builder.BuildBr(forIncremnet);
        builder.PositionAtEnd(forIncremnet);
        // TODO: increment
        builder.BuildBr(forStart);
        builder.PositionAtEnd(afterFor);
    }
}