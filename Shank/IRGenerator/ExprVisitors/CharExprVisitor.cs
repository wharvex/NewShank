using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class CharExprVisitor : Visitor
{
    public override LLVMValueRef Accept(
        VariableReferenceNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public override LLVMValueRef Accept(
        CharNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public override LLVMValueRef Accept(
        MathOpNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }
}
