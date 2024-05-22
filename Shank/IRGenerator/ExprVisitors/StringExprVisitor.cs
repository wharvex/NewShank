using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class StringExprVisitor : Visitor
{
    public LLVMValueRef Accept(VariableReferenceNode node, Context context, LLVMBuilderRef builder,
        LLVMModuleRef module)
    {
        throw new NotImplementedException();
    }


    public LLVMValueRef Accept(StringNode node, Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        throw new NotImplementedException();
    }

    public LLVMValueRef Accept(MathOpNode node, Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        throw new NotImplementedException();
    }
}