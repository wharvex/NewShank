using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class StringExprVisitor : Visitor
{
    public override LLVMValueRef Accept(
        VariableUsageNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public override LLVMValueRef Accept(
        StringNode node,
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
