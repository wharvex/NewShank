using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

public class LlvmFuncGettingVisitor(LLVMModuleRef llvmModule) : IAstNodeVisitor<LLVMValueRef>
{
    public LLVMModuleRef LlvmModule { get; init; } = llvmModule;

    public LLVMValueRef Visit(AssignmentNode a)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(BooleanExpressionNode b)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(BoolNode b)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(BuiltInFunctionNode b)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(CharNode c)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(ElseNode e)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(EmptyNode e)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(EnumNode e)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(FloatNode f)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(ForNode f)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(FunctionCallNode f)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(FunctionNode f)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(IfNode i)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(IntNode i)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(MathOpNode m)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(ModuleNode m)
    {
        throw new NotImplementedException();
    }

    // public LLVMValueRef Visit(ParameterNode p)
    // {
    //     throw new NotImplementedException();
    // }

    public LLVMValueRef Visit(ProgramNode p)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(RecordNode r)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(RepeatNode r)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(StatementNode s)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(StringNode s)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(TestNode t)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(VariableDeclarationNode v)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(VariableUsagePlainNode v)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Visit(WhileNode w)
    {
        throw new NotImplementedException();
    }
}
