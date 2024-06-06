using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class CharExprVisitor : Visitor
{
    public override LLVMValueRef Accept(
        VariableUsageNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        LlvmVaraible varaible = context.GetVaraible(node.Name);
        return builder.BuildLoad2(varaible.TypeRef, varaible.ValueRef);
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
