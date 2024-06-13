using System.Diagnostics;
using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

public class LlvmFuncParamsTypesGettingVisitor(LLVMContextRef llvmContext)
    : IAstNodeVisitor<LLVMTypeRef[]>
{
    public LLVMContextRef LlvmContext { get; init; } = llvmContext;

    public LLVMTypeRef[] Visit(AssignmentNode a)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(BooleanExpressionNode b)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(BoolNode b)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(BuiltInFunctionNode b)
    {
        return b.Name switch
        {
            "Write" => [LLVMTypeRef.CreatePointer(LlvmContext.Int8Type, 0)],
            _ => throw new UnreachableException()
        };
    }

    public LLVMTypeRef[] Visit(CharNode c)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(ElseNode e)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(EmptyNode e)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(EnumNode e)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(FloatNode f)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(ForNode f)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(FunctionCallNode f)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(FunctionNode f)
    {
        return f.ParameterVariables.Select(
            v => v.Accept(new LlvmFuncParamTypeGettingVisitor(LlvmContext))
        )
            .ToArray();
    }

    public LLVMTypeRef[] Visit(IfNode i)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(IntNode i)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(MathOpNode m)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(ModuleNode m)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(ParameterNode p)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(ProgramNode p)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(RecordNode r)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(RepeatNode r)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(StatementNode s)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(StringNode s)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(TestNode t)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(VariableNode v)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(VariableUsagePlainNode v)
    {
        throw new NotImplementedException();
    }

    public LLVMTypeRef[] Visit(WhileNode w)
    {
        throw new NotImplementedException();
    }
}
