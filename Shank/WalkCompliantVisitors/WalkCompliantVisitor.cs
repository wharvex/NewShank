using Shank.ASTNodes;

namespace Shank.WalkCompliantVisitors;

public class WalkCompliantVisitor
{
    public virtual ASTNode? Visit(ASTNode n) => null;

    public virtual ASTNode Visit(ASTNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(ASTNode n) => n;

    public virtual ASTNode? Visit(CallableNode n) => null;

    public virtual ASTNode Visit(CallableNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(CallableNode n) => n;

    public virtual ASTNode? Visit(OverloadedFunctionNode n) => null;

    public virtual ASTNode Visit(OverloadedFunctionNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(OverloadedFunctionNode n) => n;

    public virtual ASTNode? Visit(BuiltInFunctionNode n) => null;

    public virtual ASTNode Visit(BuiltInFunctionNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(BuiltInFunctionNode n) => n;

    public virtual ASTNode? Visit(BuiltInVariadicFunctionNode n) => null;

    public virtual ASTNode Visit(BuiltInVariadicFunctionNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(BuiltInVariadicFunctionNode n) => n;

    public virtual ASTNode? Visit(FunctionNode n) => null;

    public virtual ASTNode Visit(FunctionNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(FunctionNode n) => n;

    public virtual ASTNode? Visit(TestNode n) => null;

    public virtual ASTNode Visit(TestNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(TestNode n) => n;

    public virtual ASTNode? Visit(EmptyNode n) => null;

    public virtual ASTNode Visit(EmptyNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(EmptyNode n) => n;

    public virtual ASTNode? Visit(EnumNode n) => null;

    public virtual ASTNode Visit(EnumNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(EnumNode n) => n;

    public virtual ASTNode? Visit(ExpressionNode n) => null;

    public virtual ASTNode Visit(ExpressionNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(ExpressionNode n) => n;

    public virtual ASTNode? Visit(BoolNode n) => null;

    public virtual ASTNode Visit(BoolNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(BoolNode n) => n;

    public virtual ASTNode? Visit(BooleanExpressionNode n) => null;

    public virtual ASTNode Visit(BooleanExpressionNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(BooleanExpressionNode n) => n;

    public virtual ASTNode? Visit(CharNode n) => null;

    public virtual ASTNode Visit(CharNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(CharNode n) => n;

    public virtual ASTNode? Visit(FloatNode n) => null;

    public virtual ASTNode Visit(FloatNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(FloatNode n) => n;

    public virtual ASTNode? Visit(IntNode n) => null;

    public virtual ASTNode Visit(IntNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(IntNode n) => n;

    public virtual ASTNode? Visit(MathOpNode n) => null;

    public virtual ASTNode Visit(MathOpNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(MathOpNode n) => n;

    public virtual ASTNode? Visit(StringNode n) => null;

    public virtual ASTNode Visit(StringNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(StringNode n) => n;

    public virtual ASTNode? Visit(VariableUsageNodeTemp n) => null;

    public virtual ASTNode Visit(VariableUsageNodeTemp n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(VariableUsageNodeTemp n) => n;

    public virtual ASTNode? Visit(VariableUsageIndexNode n) => null;

    public virtual ASTNode Visit(VariableUsageIndexNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(VariableUsageIndexNode n) => n;

    public virtual ASTNode? Visit(VariableUsageMemberNode n) => null;

    public virtual ASTNode Visit(VariableUsageMemberNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(VariableUsageMemberNode n) => n;

    public virtual ASTNode? Visit(VariableUsagePlainNode n) => null;

    public virtual ASTNode Visit(VariableUsagePlainNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(VariableUsagePlainNode n) => n;

    public virtual ASTNode? Visit(MemberAccessNode n) => null;

    public virtual ASTNode Visit(MemberAccessNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(MemberAccessNode n) => n;

    public virtual ASTNode? Visit(ModuleNode n) => null;

    public virtual ASTNode Visit(ModuleNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(ModuleNode n) => n;

    public virtual ASTNode? Visit(ProgramNode n) => null;

    public virtual ASTNode Visit(ProgramNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(ProgramNode n) => n;

    public virtual ASTNode? Visit(RecordNode n) => null;

    public virtual ASTNode Visit(RecordNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(RecordNode n) => n;

    public virtual ASTNode? Visit(StatementNode n) => null;

    public virtual ASTNode Visit(StatementNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(StatementNode n) => n;

    public virtual ASTNode? Visit(AssignmentNode n) => null;

    public virtual ASTNode Visit(AssignmentNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(AssignmentNode n) => n;

    public virtual ASTNode? Visit(ForNode n) => null;

    public virtual ASTNode Visit(ForNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(ForNode n) => n;

    public virtual ASTNode? Visit(FunctionCallNode n) => null;

    public virtual ASTNode Visit(FunctionCallNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(FunctionCallNode n) => n;

    public virtual ASTNode? Visit(IfNode n) => null;

    public virtual ASTNode Visit(IfNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(IfNode n) => n;

    public virtual ASTNode? Visit(ElseNode n) => null;

    public virtual ASTNode Visit(ElseNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(ElseNode n) => n;

    public virtual ASTNode? Visit(RepeatNode n) => null;

    public virtual ASTNode Visit(RepeatNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(RepeatNode n) => n;

    public virtual ASTNode? Visit(WhileNode n) => null;

    public virtual ASTNode Visit(WhileNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(WhileNode n) => n;

    public virtual ASTNode? Visit(VariableDeclarationNode n) => null;

    public virtual ASTNode Visit(VariableDeclarationNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        return n;
    }

    public virtual ASTNode Final(VariableDeclarationNode n) => n;

    public Dictionary<string, T> VisitDictionary<T>(Dictionary<string, T> d)
        where T : ASTNode
    {
        // This method could be a one-liner (see commented-out return statement below), but then for
        // some reason that syntax won't let us step into the "Walk" method in the debugger.
        var ret = new Dictionary<string, T>();
        foreach (var p in d)
        {
            ret[p.Key] = (T)p.Value.Walk(this);
        }

        return ret;

        //return d.Select(kvp => new KeyValuePair<string, T>(kvp.Key, (T)kvp.Value.Walk(this)))
        //    .ToDictionary();
    }

    public List<T> VisitList<T>(List<T> l)
        where T : ASTNode
    {
        // This method could be a one-liner (see commented-out return statement below), but then for
        // some reason that syntax won't let me step into the "Walk" method in the debugger.
        var ret = new List<T>();
        foreach (var n in l)
        {
            ret.Add((T)n.Walk(this));
        }

        return ret;
        //return [..l.Select(e => (T)e.Walk(this))];
    }
}
