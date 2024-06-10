using Shank.ASTNodes;

namespace Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

public interface IAstNodeVisitor<T>
{
    T Visit(AssignmentNode a);
    T Visit(BooleanExpressionNode b);
    T Visit(BoolNode b);
    T Visit(BuiltInFunctionNode b);
    T Visit(CharNode c);
    T Visit(ElseNode e);
    T Visit(EnumNode e);
    T Visit(FloatNode f);
    T Visit(ForNode f);
    T Visit(FunctionCallNode f);
    T Visit(FunctionNode f);
    T Visit(IfNode i);
    T Visit(IntNode i);
    T Visit(MathOpNode m);
    T Visit(ModuleNode m);
    T Visit(ParameterNode p);
    T Visit(ProgramNode p);
    T Visit(RecordNode r);
    T Visit(RepeatNode r);
    T Visit(StatementNode s);
    T Visit(StringNode s);
    T Visit(TestNode t);
    T Visit(VariableNode v);
    T Visit(VariableUsageNode v);
    T Visit(WhileNode w);
}
