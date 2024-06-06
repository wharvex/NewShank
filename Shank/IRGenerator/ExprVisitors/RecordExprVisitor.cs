using LLVMSharp.Interop;

namespace Shank.ExprVisitors;

public class RecordExprVisitor : Visitor
{
    public override LLVMValueRef Accept(
        RecordNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public override LLVMValueRef Accept(
        VariableReferenceNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }
}
