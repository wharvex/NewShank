using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class FloatExprVisitor : Visitor
{
    
    public override LLVMValueRef Accept(
        FloatNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        return LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, node.Value);
    }

    public override LLVMValueRef Accept(
        VariableReferenceNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        LlvmVaraible varaible = context.GetVaraible(node.Name);
        return builder.BuildLoad2(varaible.TypeRef, varaible.ValueRef);
    }

    public override LLVMValueRef Accept(
        MathOpNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        LLVMValueRef R = node.Right.Visit(this, context, builder, module);
        LLVMValueRef L = node.Left.Visit(this, context, builder, module);
        return node.Op switch
        {
            ASTNode.MathOpType.plus => builder.BuildFAdd(L, R, "addtmp"),
            ASTNode.MathOpType.minus => builder.BuildFSub(L, R, "subtmp"),
            ASTNode.MathOpType.times => builder.BuildFMul(L, R, "multmp"),
            ASTNode.MathOpType.divide => builder.BuildFDiv(L, R, "divtmp"),
            _ => throw new Exception("unsupported operation")
        };
    }
}
