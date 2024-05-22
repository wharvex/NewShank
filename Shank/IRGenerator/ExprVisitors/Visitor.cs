using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public abstract class Visitor
{
    public virtual LLVMValueRef Accept(
        IntNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        FloatNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        VariableReferenceNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        CharNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        BoolNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        StringNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        MathOpNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        BooleanExpressionNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }
}
