using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public abstract class Visitor
{
    public LLVMValueRef Accept(
        IntNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public LLVMValueRef Accept(
        FloatNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public LLVMValueRef Accept(
        VariableReferenceNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public LLVMValueRef Accept(
        CharNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public LLVMValueRef Accept(
        BoolNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public LLVMValueRef Accept(
        StringNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public LLVMValueRef Accept(
        MathOpNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public LLVMValueRef Accept(
        BooleanExpressionNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }
}
