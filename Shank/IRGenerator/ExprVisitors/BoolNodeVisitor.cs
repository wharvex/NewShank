using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class BoolNodeVisitor : Visitor
{
    public LLVMValueRef Accept(
        IntNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Accept(
        FloatNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Accept(
        VariableReferenceNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Accept(
        CharNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Accept(
        BoolNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Accept(
        StringNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Accept(
        BooleanExpressionNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }
}
