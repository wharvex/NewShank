using Shank.ASTNodes;

namespace Shank.WalkCompliantVisitors;

public class WalkCompliantVisitor
{
    public virtual ASTNode? Visit(ASTNode n) => null;

    public virtual ASTNode? Final(ASTNode n) => null;

    public virtual ASTNode? Visit(CallableNode n) => null;

    public virtual ASTNode? Final(CallableNode n) => null;

    public virtual ASTNode? Visit(BuiltInFunctionNode n) => null;

    public virtual ASTNode? Final(BuiltInFunctionNode n) => null;

    public virtual ASTNode? Visit(BuiltInVariadicFunctionNode n) => null;

    public virtual ASTNode? Final(BuiltInVariadicFunctionNode n) => null;

    public virtual ASTNode? Visit(FunctionNode n) => null;

    public virtual ASTNode? Final(FunctionNode n) => null;

    public virtual ASTNode? Visit(TestNode n) => null;

    public virtual ASTNode? Final(TestNode n) => null;

    public virtual ASTNode? Visit(EmptyNode n) => null;

    public virtual ASTNode? Final(EmptyNode n) => null;

    public virtual ASTNode? Visit(EnumNode n) => null;

    public virtual ASTNode? Final(EnumNode n) => null;

    public virtual ASTNode? Visit(ExpressionNode n) => null;

    public virtual ASTNode? Final(ExpressionNode n) => null;

    public virtual ASTNode? Visit(BoolNode n) => null;

    public virtual ASTNode? Final(BoolNode n) => null;

    public virtual ASTNode? Visit(BooleanExpressionNode n) => null;

    public virtual ASTNode? Final(BooleanExpressionNode n) => null;

    public virtual ASTNode? Visit(CharNode n) => null;

    public virtual ASTNode? Final(CharNode n) => null;

    public virtual ASTNode? Visit(FloatNode n) => null;

    public virtual ASTNode? Final(FloatNode n) => null;

    public virtual ASTNode? Visit(IntNode n) => null;

    public virtual ASTNode? Final(IntNode n) => null;

    public virtual ASTNode? Visit(MathOpNode n) => null;

    public virtual ASTNode? Final(MathOpNode n) => null;

    public virtual ASTNode? Visit(StringNode n) => null;

    public virtual ASTNode? Final(StringNode n) => null;

    public virtual ASTNode? Visit(VariableUsageNodeTemp n) => null;

    public virtual ASTNode? Final(VariableUsageNodeTemp n) => null;

    public virtual ASTNode? Visit(VariableUsageIndexNode n) => null;

    public virtual ASTNode? Final(VariableUsageIndexNode n) => null;

    public virtual ASTNode? Visit(VariableUsageMemberNode n) => null;

    public virtual ASTNode? Final(VariableUsageMemberNode n) => null;

    public virtual ASTNode? Visit(VariableUsagePlainNode n) => null;

    public virtual ASTNode? Final(VariableUsagePlainNode n) => null;

    public virtual ASTNode? Visit(MemberAccessNode n) => null;

    public virtual ASTNode? Final(MemberAccessNode n) => null;

    public virtual ASTNode? Visit(ModuleNode n) => null;

    public virtual ASTNode? Final(ModuleNode n) => null;

    public virtual ASTNode? Visit(ProgramNode n) => null;

    public virtual ASTNode? Final(ProgramNode n) => null;

    public virtual ASTNode? Visit(RecordNode n) => null;

    public virtual ASTNode? Final(RecordNode n) => null;

    public virtual ASTNode? Visit(StatementNode n) => null;

    public virtual ASTNode? Final(StatementNode n) => null;

    public virtual ASTNode? Visit(AssignmentNode n) => null;

    public virtual ASTNode? Final(AssignmentNode n) => null;

    public virtual ASTNode? Visit(ForNode n) => null;

    public virtual ASTNode? Final(ForNode n) => null;

    public virtual ASTNode? Visit(FunctionCallNode n) => null;

    public virtual ASTNode? Final(FunctionCallNode n) => null;

    public virtual ASTNode? Visit(IfNode n) => null;

    public virtual ASTNode? Final(IfNode n) => null;

    public virtual ASTNode? Visit(ElseNode n) => null;

    public virtual ASTNode? Final(ElseNode n) => null;

    public virtual ASTNode? Visit(RepeatNode n) => null;

    public virtual ASTNode? Final(RepeatNode n) => null;

    public virtual ASTNode? Visit(WhileNode n) => null;

    public virtual ASTNode? Final(WhileNode n) => null;

    public virtual ASTNode? Visit(VariableDeclarationNode n) => null;

    public virtual ASTNode? Final(VariableDeclarationNode n) => null;

    public Dictionary<string, T> VisitDictionary<T>(Dictionary<string, T> d)
        where T : ASTNode =>
        d.Select(kvp => new KeyValuePair<string, T>(kvp.Key, (T)kvp.Value.Walk(this)))
            .ToDictionary();

    public List<T> VisitList<T>(List<T> l)
        where T : ASTNode => [..l.Select(e => (T)e.Walk(this))];
}
