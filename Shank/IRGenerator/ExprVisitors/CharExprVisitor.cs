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
        LLVMValue value = context.GetVaraible(node.Name);
        return builder.BuildLoad2(value.TypeRef, value.ValueRef);
    }

    public override LLVMValueRef Accept(
        CharNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        return LLVMValueRef.CreateConstInt(module.Context.Int8Type, (ulong)node.Value);
    }
}
