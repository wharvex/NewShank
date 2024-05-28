using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class StringExprVisitor : Visitor
{
    public override LLVMValueRef Accept(
        VariableReferenceNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        LLVMValue variable = context.GetVaraible(node.Name);
        return builder.BuildLoad2(variable.TypeRef, variable.ValueRef);
    }

    public override LLVMValueRef Accept(
        StringNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
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

    public override LLVMValueRef Accept(
        MathOpNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        if (node.Op == ASTNode.MathOpType.plus)
        {
            LLVMValueRef r = node.Right.Visit(this, context, builder, module);
            LLVMValueRef l = node.Left.Visit(this, context, builder, module);
            var lSize = builder.BuildExtractValue(l, 0);
            var rSize = builder.BuildExtractValue(r, 0);
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
                [newContent, builder.BuildExtractValue(l, 1), lSize]
            );
            // fill the second part of the string
            // first "increment" the pointer of the string to be after the contents of the first part
            var secondPart = builder.BuildInBoundsGEP2(LLVMTypeRef.Int8, newContent, [lSize]);
            builder.BuildCall2(
                context.CFuntions.memcpy.TypeOf,
                context.CFuntions.memcpy.Function,
                [secondPart, builder.BuildExtractValue(r, 1), rSize]
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
}
