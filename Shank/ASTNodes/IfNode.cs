using System.Text;
using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class IfNode : StatementNode
{
    protected IfNode(List<StatementNode> children)
    {
        Expression = null;
        Children = children;
        NextIfNode = null;
    }

    public IfNode(
        BooleanExpressionNode expression,
        List<StatementNode> children,
        IfNode? nextIfNode = null
    )
    {
        Expression = expression;
        Children = children;
        NextIfNode = nextIfNode;
    }

    public BooleanExpressionNode? Expression { get; init; }
    public List<StatementNode> Children { get; init; }
    public IfNode? NextIfNode { get; init; }

    public override string ToString()
    {
        var linePrefix = $"if, line {Line}, {Expression}, ";
        var b = new StringBuilder();
        b.AppendLine(linePrefix + "begin");
        b.AppendLine(linePrefix + "statements begin");
        Children.ForEach(s => b.AppendLine(s.ToString()));
        b.AppendLine(linePrefix + "statements end");
        b.AppendLine(linePrefix + "next begin");
        if (NextIfNode != null)
        {
            b.Append(NextIfNode);
        }
        b.AppendLine(linePrefix + "next end");
        b.Append(linePrefix + "end");

        return b.ToString();
    }

    // public override void VisitStatement(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     // visitor.Visit(this);
    //     // if (Expression != null)
    //     // // if the condition is null then it's an else statement, which can only happen after an if statement
    //     // // so is it's an if statement, and since we compile if statements recursively, like how we parse them
    //     // // we know that we already created the block for the else statement, when compiling the if part
    //     // // so we just compile the statements in the else block
    //     // // if the condition is not null we compile the condition, create two blocks one for if it's true, and for when the condition is false
    //     // // we then just compile the statements for when the condition is true under the true block, followed by a goto to an after block
    //     // // and we visit(compile) the IfNode for when the condition is false if needed, followed by a goto to the after branch
    //     // // note we could make this a bit better by checking if next is null and then make the conditional branch to after block in the false cas
    //     // {
    //     //     var condition = Expression.Visit(visitor, context, builder, module);
    //     //     var ifBlock = context.CurrentFunction.AppendBasicBlock("if block");
    //     //     var elseBlock = context.CurrentFunction.AppendBasicBlock("else block");
    //     //     var afterBlock = context.CurrentFunction.AppendBasicBlock("after if statement");
    //     //     builder.BuildCondBr(condition, ifBlock, elseBlock);
    //     //;
    //     //     builder.PositionAtEnd(ifBlock);
    //     //     Children.ForEach(c => c.VisitStatement(visitor, context, builder, module));
    //     //     builder.BuildBr(afterBlock);
    //     //     builder.PositionAtEnd(elseBlock);
    //     //     NextIfNode?.VisitStatement(visitor, context, builder, module);
    //     //     builder.BuildBr(afterBlock);
    //     //     builder.PositionAtEnd(afterBlock);
    //     // }
    //     // else
    //     // {
    //     //     Children.ForEach(c => c.VisitStatement(visitor, context, builder, module));
    //     // }
    // }

    public override void Visit(StatementVisitor visit)
    {
        visit.Accept(this);
    }

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
