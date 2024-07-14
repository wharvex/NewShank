/*
using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.IRGenerator;

namespace Shank.ExprVisitors;

/// <summary>
/// Depreciated visitor classes
/// keeping around in case I forgot something or as reference
/// </summary>
/// <param name="context"></param>
/// <param name="builder"></param>
/// <param name="module"></param>
public class LLVMExpr(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    : ExpressionVisitor<LLVMValueRef>
{
    public void DebugRuntime(string format, LLVMValueRef value)
    {
        builder.BuildCall2(
            context.CFuntions.printf.TypeOf,
            context.CFuntions.printf.Function,
            [builder.BuildGlobalStringPtr(format), value]
        );
    }

    private Types _types(LLVMTypeRef typeRef)
    {
        if (
            typeRef == LLVMTypeRef.Int64
            || typeRef == LLVMTypeRef.Int1
            || typeRef == LLVMTypeRef.Int8
        )
            return Types.INTEGER;
        else if (typeRef == LLVMTypeRef.Double)
            return Types.FLOAT;
        else if (typeRef == context.StringType)
            return Types.STRING;
        else
            throw new Exception("undefined type");
    }

    public override LLVMValueRef Visit(IntNode node)
    {
        return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)node.Value);
    }

    public override LLVMValueRef Visit(FloatNode node)
    {
        return LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, node.Value);
    }

    public override LLVMValueRef Visit(VariableUsagePlainNode node)
    {
        LLVMValue value = context.GetVariable(node.MonomorphizedName());
        if (node.Extension != null)
        {
            var a = builder.BuildGEP2(
                value.TypeRef,
                value.ValueRef,
                new[] { node.Extension.Accept(new LLVMExpr(context, builder, module)) }
            );
            return builder.BuildLoad2(value.TypeRef, a);
        }
        return builder.BuildLoad2(value.TypeRef, value.ValueRef);
    }

    public override LLVMValueRef Visit(CharNode node)
    {
        return LLVMValueRef.CreateConstInt(module.Context.Int8Type, (ulong)(node.Value));
    }

    public override LLVMValueRef Visit(BoolNode node)
    {
        return LLVMValueRef.CreateConstInt(module.Context.Int1Type, (ulong)(node.GetValueAsInt()));
    }

    public override LLVMValueRef Visit(StringNode node)
    {
        // a string in llvm is just length + content
        var stringLength = LLVMValueRef.CreateConstInt(
            module.Context.Int32Type,
            (ulong)node.Value.Length
        );

        var stringContent = builder.BuildGlobalStringPtr(node.Value);

        // if we never mutate the string part directly, meaning when we do assignment we assign it a new string struct, then we do not need to do this malloc,
        // and we could just insert, the string constant in the string struct, we could do this because we don't directly mutate the string,
        // and the way we currently define string constants, they must not be mutated
        // one problem is that constant llvm strings are null terminated
        var stringPointer = builder.BuildMalloc(
            LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)node.Value.Length)
        );
        builder.BuildCall2(
            context.CFuntions.memcpy.TypeOf,
            context.CFuntions.memcpy.Function,
            [stringPointer, stringContent, stringLength]
        );
        var String = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
                LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0))
            ],
            false
        );
        String = builder.BuildInsertValue(String, stringLength, 0);
        String = builder.BuildInsertValue(String, stringPointer, 1);
        return String;
    }

    public override LLVMValueRef Visit(MathOpNode node)
    {
        LLVMValueRef L = node.Left.Accept(this);
        LLVMValueRef R = node.Right.Accept(this);
        if (_types(L.TypeOf) == Types.INTEGER)
        {
            return node.Op switch
            {
                MathOpNode.MathOpType.Plus => builder.BuildAdd(L, R, "addtmp"),
                MathOpNode.MathOpType.Minus => builder.BuildSub(L, R, "subtmp"),
                MathOpNode.MathOpType.Times => builder.BuildMul(L, R, "multmp"),
                MathOpNode.MathOpType.Divide => builder.BuildSDiv(L, R, "divtmp"),
                MathOpNode.MathOpType.Modulo => builder.BuildURem(L, R, "modtmp"),
                _ => throw new Exception("unsupported operation")
            };
        }
        else if (_types(L.TypeOf) == Types.FLOAT)
        {
            return node.Op switch
            {
                MathOpNode.MathOpType.Plus => builder.BuildFAdd(L, R, "addtmp"),
                MathOpNode.MathOpType.Minus => builder.BuildFSub(L, R, "subtmp"),
                MathOpNode.MathOpType.Times => builder.BuildFMul(L, R, "multmp"),
                MathOpNode.MathOpType.Divide => builder.BuildFDiv(L, R, "divtmp"),
                _ => throw new Exception("unsupported operation")
            };
        }
        else if (_types(L.TypeOf) == Types.STRING)
        {
            if (node.Op == MathOpNode.MathOpType.Plus)
            {
                // LLVMValueRef r = node.Right.Visit(this);
                // LLVMValueRef l = node.Left.Visit(this);
                var lSize = builder.BuildExtractValue(L, 0);
                var rSize = builder.BuildExtractValue(R, 0);
                var newSize = builder.BuildAdd(lSize, rSize);
                // allocate enough (or perhaps in the future more than enough) space, for the concatenated string
                // I think this has to be heap allocated, because we can assign this to an out parameter of a function, and we would then lose it if it was stack allocated
                var newContent = builder.BuildCall2(
                    context.CFuntions.malloc.TypeOf,
                    context.CFuntions.malloc.Function,
                    [newSize]
                );
                // fill the first part of the string
                builder.BuildCall2(
                    context.CFuntions.memcpy.TypeOf,
                    context.CFuntions.memcpy.Function,
                    [newContent, builder.BuildExtractValue(L, 1), lSize]
                );
                // fill the second part of the string
                // first "increment" the pointer of the string to be after the contents of the first part
                var secondPart = builder.BuildInBoundsGEP2(LLVMTypeRef.Int8, newContent, [lSize]);
                builder.BuildCall2(
                    context.CFuntions.memcpy.TypeOf,
                    context.CFuntions.memcpy.Function,
                    [secondPart, builder.BuildExtractValue(R, 1), rSize]
                );
                var String = LLVMValueRef.CreateConstStruct(
                    [
                        LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
                        LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0))
                    ],
                    false
                );
                String = builder.BuildInsertValue(String, newSize, 0);
                String = builder.BuildInsertValue(String, newContent, 1);
                return String;
            }
            else
            {
                throw new Exception(
                    $"strings can only be concatenated, you tried to {node.Op}, with strings"
                );
            }
        }
        else
        {
            throw new Exception("error");
        }
    }

    public override LLVMValueRef Visit(BooleanExpressionNode node)
    {
        LLVMValueRef L = node.Left.Accept(this);
        LLVMValueRef R = node.Right.Accept(this);

        if (_types(L.TypeOf) == Types.INTEGER)
        {
            return node.Op switch
            {
                BooleanExpressionNode.BooleanExpressionOpType.eq
                    => builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.ne
                    => builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.lt
                    => builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.le
                    => builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.gt
                    => builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.ge
                    => builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, L, R, "cmp"),
                _ => throw new Exception("not accepted op")
            };
        }
        else if (_types(L.TypeOf) == Types.FLOAT)
        {
            return node.Op switch
            {
                BooleanExpressionNode.BooleanExpressionOpType.eq
                    => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.ne
                    => builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.lt
                    => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.le
                    => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLE, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.gt
                    => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGT, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.ge
                    => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGE, L, R, "cmp"),

                _ => throw new Exception("")
            };
        }

        throw new Exception("undefined bool");
    }

    public override LLVMValueRef Visit(RecordNode node)
    {
        throw new NotImplementedException();
    }

    // public override LLVMValueRef Visit(RecordNode node)
    // {
    //     throw new NotImplementedException();
    // }

    // public override LLVMValueRef Visit(ParameterNode node)
    // {
    //     return node.IsVariable
    //         ? context.GetVariable(node.Variable?.Name).ValueRef
    //         : node.Constant.Accept(this);
    // }
}
*/
