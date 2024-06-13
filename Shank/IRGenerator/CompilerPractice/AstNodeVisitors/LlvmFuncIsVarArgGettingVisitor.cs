using Shank.ASTNodes;

namespace Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

public class LlvmFuncIsVarArgGettingVisitor : IAstNodeVisitor<bool>
{
    public bool Visit(AssignmentNode a)
    {
        throw new NotImplementedException();
    }

    public bool Visit(BooleanExpressionNode b)
    {
        throw new NotImplementedException();
    }

    public bool Visit(BoolNode b)
    {
        throw new NotImplementedException();
    }

    public bool Visit(BuiltInFunctionNode b)
    {
        return b.Name switch
        {
            "write" => true,
            "read" => true,
            _ => false
        };
    }

    public bool Visit(CharNode c)
    {
        throw new NotImplementedException();
    }

    public bool Visit(ElseNode e)
    {
        throw new NotImplementedException();
    }

    public bool Visit(EmptyNode e)
    {
        throw new NotImplementedException();
    }

    public bool Visit(EnumNode e)
    {
        throw new NotImplementedException();
    }

    public bool Visit(FloatNode f)
    {
        throw new NotImplementedException();
    }

    public bool Visit(ForNode f)
    {
        throw new NotImplementedException();
    }

    public bool Visit(FunctionCallNode f)
    {
        throw new NotImplementedException();
    }

    public bool Visit(FunctionNode f)
    {
        return false;
    }

    public bool Visit(IfNode i)
    {
        throw new NotImplementedException();
    }

    public bool Visit(IntNode i)
    {
        throw new NotImplementedException();
    }

    public bool Visit(MathOpNode m)
    {
        throw new NotImplementedException();
    }

    public bool Visit(ModuleNode m)
    {
        throw new NotImplementedException();
    }

    public bool Visit(ParameterNode p)
    {
        throw new NotImplementedException();
    }

    public bool Visit(ProgramNode p)
    {
        throw new NotImplementedException();
    }

    public bool Visit(RecordNode r)
    {
        throw new NotImplementedException();
    }

    public bool Visit(RepeatNode r)
    {
        throw new NotImplementedException();
    }

    public bool Visit(StatementNode s)
    {
        throw new NotImplementedException();
    }

    public bool Visit(StringNode s)
    {
        throw new NotImplementedException();
    }

    public bool Visit(TestNode t)
    {
        throw new NotImplementedException();
    }

    public bool Visit(VariableNode v)
    {
        throw new NotImplementedException();
    }

    public bool Visit(VariableUsagePlainNode v)
    {
        throw new NotImplementedException();
    }

    public bool Visit(WhileNode w)
    {
        throw new NotImplementedException();
    }
}
