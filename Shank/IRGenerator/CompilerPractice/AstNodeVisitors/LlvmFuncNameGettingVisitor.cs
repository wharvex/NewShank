using System.Diagnostics;
using Shank.ASTNodes;

namespace Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

public class LlvmFuncNameGettingVisitor : IAstNodeVisitor<string>
{
    public string Visit(AssignmentNode a)
    {
        throw new NotImplementedException();
    }

    public string Visit(BooleanExpressionNode b)
    {
        throw new NotImplementedException();
    }

    public string Visit(BoolNode b)
    {
        throw new NotImplementedException();
    }

    public string Visit(BuiltInFunctionNode b)
    {
        return b.Name switch
        {
            "Write" => "printf",
            "Read" => "scanf",
            _ => throw new UnreachableException()
        };
    }

    public string Visit(CharNode c)
    {
        throw new NotImplementedException();
    }

    public string Visit(ElseNode e)
    {
        throw new NotImplementedException();
    }

    public string Visit(EmptyNode e)
    {
        throw new NotImplementedException();
    }

    public string Visit(EnumNode e)
    {
        throw new NotImplementedException();
    }

    public string Visit(FloatNode f)
    {
        throw new NotImplementedException();
    }

    public string Visit(ForNode f)
    {
        throw new NotImplementedException();
    }

    public string Visit(FunctionCallNode f)
    {
        throw new NotImplementedException();
    }

    public string Visit(FunctionNode f)
    {
        return f.Name.Equals("start") ? "main" : f.Name;
    }

    public string Visit(IfNode i)
    {
        throw new NotImplementedException();
    }

    public string Visit(IntNode i)
    {
        throw new NotImplementedException();
    }

    public string Visit(MathOpNode m)
    {
        throw new NotImplementedException();
    }

    public string Visit(ModuleNode m)
    {
        throw new NotImplementedException();
    }

    public string Visit(ParameterNode p)
    {
        throw new NotImplementedException();
    }

    public string Visit(ProgramNode p)
    {
        throw new NotImplementedException();
    }

    public string Visit(RecordNode r)
    {
        throw new NotImplementedException();
    }

    public string Visit(RepeatNode r)
    {
        throw new NotImplementedException();
    }

    public string Visit(StatementNode s)
    {
        throw new NotImplementedException();
    }

    public string Visit(StringNode s)
    {
        throw new NotImplementedException();
    }

    public string Visit(TestNode t)
    {
        throw new NotImplementedException();
    }

    public string Visit(VariableNode v)
    {
        throw new NotImplementedException();
    }

    public string Visit(VariableUsageNode v)
    {
        throw new NotImplementedException();
    }

    public string Visit(WhileNode w)
    {
        throw new NotImplementedException();
    }
}
