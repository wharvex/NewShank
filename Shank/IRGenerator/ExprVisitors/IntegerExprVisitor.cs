using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class IntegerExprVisitor : Visitor
{
    public override LLVMValueRef Accept(
        IntNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        return LLVMValueRef.CreateConstInt(module.Context.Int64Type, (ulong)node.Value);
    }

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
        MathOpNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        LLVMValueRef L = node.Left.Visit(this, context, builder, module);
        LLVMValueRef R = node.Right.Visit(this, context, builder, module);

        return node.Op switch
        {
            ASTNode.MathOpType.plus => builder.BuildAdd(L, R, "addtmp"),
            ASTNode.MathOpType.minus => builder.BuildSub(L, R, "subtmp"),
            ASTNode.MathOpType.times => builder.BuildMul(L, R, "multmp"),
            ASTNode.MathOpType.divide => builder.BuildSDiv(L, R, "divtmp"),
            ASTNode.MathOpType.modulo => builder.BuildURem(L, R, "modtmp"),
            _ => throw new Exception("unsupported operation")
        };
    }
}
