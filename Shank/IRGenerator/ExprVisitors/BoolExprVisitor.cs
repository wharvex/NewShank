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
    private Types _types(ASTNode L, ASTNode R, Context context)
    {
        if (L is BoolNode || L is IntNode || L is CharNode)
        {
            return Types.INTEGER;
        }
        else if (R is BoolNode || R is IntNode || R is CharNode)
        {
            return Types.INTEGER;
        }
        else if (L is FloatNode || R is FloatNode)
        {
            return Types.FLOAT;
        }
        else if (L is VariableReferenceNode)
        {
            VariableReferenceNode v = (VariableReferenceNode)L;
            LlvmVaraible varaible = context.GetVaraible(v.Name);
            if (varaible.TypeRef == LLVMTypeRef.Float)
            {
                return Types.FLOAT;
            }
            else
            {
                return Types.INTEGER;
            }
        }
        else if (R is VariableReferenceNode)
        {
            VariableReferenceNode v = (VariableReferenceNode)R;
            LlvmVaraible varaible = context.GetVaraible(v.Name);
            if (varaible.TypeRef == LLVMTypeRef.Float)
            {
                return Types.FLOAT;
            }
            else
            {
                return Types.INTEGER;
            }
        }

        throw new Exception("unsupported type");
    }

    public override LLVMValueRef Accept(
        IntNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public override LLVMValueRef Accept(
        FloatNode node,
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

        if (_types(node.Left, node.Right, context) == Types.INTEGER)
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
        else if (_types(node.Left, node.Right, context) == Types.FLOAT)
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
}
