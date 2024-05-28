using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public enum Types
{
    STRUCT,
    FLOAT,
    INTEGER,
}

public class BoolExprVisitor : Visitor
{
    private Types _types(LLVMTypeRef typeRef, Context context)
    {
        if (
            typeRef == LLVMTypeRef.Int64
            || typeRef == LLVMTypeRef.Int1
            || typeRef == LLVMTypeRef.Int8
        )
            return Types.INTEGER;
        else if (typeRef == LLVMTypeRef.Double)
            return Types.FLOAT;
        else
            throw new Exception("undefined type");
    }

    public override LLVMValueRef Accept(
        IntNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)node.Value);
    }

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
        return LLVMValueRef.CreateConstInt(module.Context.Int8Type, (ulong)(node.Value));
    }

    public override LLVMValueRef Accept(
        BoolNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        return LLVMValueRef.CreateConstInt(module.Context.Int1Type, (ulong)(node.GetValueAsInt()));
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
        BooleanExpressionNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        LLVMValueRef L = node.Left.Visit(this, context, builder, module);
        LLVMValueRef R = node.Right.Visit(this, context, builder, module);

        if (_types(L.TypeOf, context) == Types.INTEGER)
        {
            return node.Op switch
            {
                ASTNode.BooleanExpressionOpType.eq
                    => builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, L, R, "cmp"),
                ASTNode.BooleanExpressionOpType.lt
                    => builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, L, R, "cmp"),
                ASTNode.BooleanExpressionOpType.le
                    => builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, L, R, "cmp"),
                ASTNode.BooleanExpressionOpType.gt
                    => builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, L, R, "cmp"),
                ASTNode.BooleanExpressionOpType.ge
                    => builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, L, R, "cmp"),

                _ => throw new Exception("not accepted op")
            };
        }
        else if (_types(L.TypeOf, context) == Types.FLOAT)
        {
            return node.Op switch
            {
                ASTNode.BooleanExpressionOpType.eq
                    => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, L, R, "cmp"),
                ASTNode.BooleanExpressionOpType.lt
                    => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, L, R, "cmp"),
                ASTNode.BooleanExpressionOpType.le
                    => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLE, L, R, "cmp"),
                ASTNode.BooleanExpressionOpType.gt
                    => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGT, L, R, "cmp"),
                ASTNode.BooleanExpressionOpType.ge
                    => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGE, L, R, "cmp"),

                _ => throw new Exception("")
            };
        }

        throw new Exception("undefined bool");
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
        if (_types(L.TypeOf, context) == Types.INTEGER)
        {
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
        else if (_types(L.TypeOf, context) == Types.FLOAT)
        {
            return node.Op switch
            {
                ASTNode.MathOpType.plus => builder.BuildFAdd(L, R, "addtmp"),
                ASTNode.MathOpType.minus => builder.BuildFSub(L, R, "subtmp"),
                ASTNode.MathOpType.times => builder.BuildFMul(L, R, "multmp"),
                ASTNode.MathOpType.divide => builder.BuildFDiv(L, R, "divtmp"),
                _ => throw new Exception("unsupported operation")
            };
        }

        throw new Exception("unsupported operation");
    }
}
