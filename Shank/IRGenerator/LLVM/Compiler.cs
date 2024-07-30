using System.Collections;
using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.IRGenerator;

// TODO: check all mallocs (and any other thing returned from calling a c function)

public enum Types
{
    Struct,
    Float,
    Integer,
    Boolean,
    String,
}

/// <summary>
/// statement pass. this adds the Values.
/// </summary>
/// <param name="context"></param>
/// <param name="builder"></param>
/// <param name="module"></param>
/// <param name="options"></param>
public class Compiler(
    Context context,
    LLVMBuilderRef builder,
    LLVMModuleRef module,
    CompileOptions options
)
{
    private readonly LLVMTypeRef _charStar = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);

    public void DebugRuntime(string format, LLVMValueRef value)
    {
        builder.BuildCall2(
            context.CFuntions.printf.TypeOf,
            context.CFuntions.printf.Function,
            [builder.BuildGlobalStringPtr(format), value]
        );
    }

    private LLVMValueRef CompileExpression(ExpressionNode expression, bool load = true)
    {
        return expression switch
        {
            BooleanExpressionNode booleanExpressionNode
                => CompileBooleanExpression(booleanExpressionNode),
            BoolNode boolNode => CompileBoolean(boolNode),
            CharNode charNode => CompileCharacter(charNode),
            FloatNode floatNode => CompileReal(floatNode),
            IntNode intNode => CompileInteger(intNode),
            MathOpNode mathOpNode => CompileMathExpression(mathOpNode),
            StringNode stringNode => CompileString(stringNode),
            VariableUsageNodeTemp variableUsageNodeTemp when options.VuOpTest
                => CompileVariableUsageNew(variableUsageNodeTemp, load),
            VariableUsagePlainNode variableUsagePlainNode
                => CompileVariableUsage(variableUsagePlainNode, load),
            _ => throw new ArgumentOutOfRangeException(nameof(expression))
        };
    }

    private static Types _types(LLVMTypeRef typeRef)
    {
        if (typeRef == LLVMTypeRef.Int64 || typeRef == LLVMTypeRef.Int8)
            return Types.Integer;
        if (typeRef == LLVMTypeRef.Int1)
            return Types.Boolean;
        if (typeRef == LLVMTypeRef.Double)
            return Types.Float;
        if (typeRef == LLVMType.StringType)
            return Types.String;
        throw new Exception("undefined type");
    }

    private LLVMValueRef HandleIntOp(
        LLVMValueRef left, //L
        MathOpNode.MathOpType operation, // OP
        LLVMValueRef right //R
    )
    {
        return operation switch
        {
            MathOpNode.MathOpType.Plus => builder.BuildAdd(left, right, "addtmp"),
            MathOpNode.MathOpType.Minus => builder.BuildSub(left, right, "subtmp"),
            MathOpNode.MathOpType.Times => builder.BuildMul(left, right, "multmp"),
            MathOpNode.MathOpType.Divide => builder.BuildSDiv(left, right, "divtmp"),
            MathOpNode.MathOpType.Modulo => builder.BuildURem(left, right, "modtmp"),
            _ => throw new Exception("unsupported operation")
        };
    }

    private LLVMValueRef HandleFloatOp(
        LLVMValueRef left, //L
        MathOpNode.MathOpType operation, // OP
        LLVMValueRef right //R
    )
    {
        return operation switch
        {
            MathOpNode.MathOpType.Plus => builder.BuildFAdd(left, right, "addtmp"),
            MathOpNode.MathOpType.Minus => builder.BuildFSub(left, right, "subtmp"),
            MathOpNode.MathOpType.Times => builder.BuildFMul(left, right, "multmp"),
            MathOpNode.MathOpType.Divide => builder.BuildFDiv(left, right, "divtmp"),
            _ => throw new Exception("unsupported operation")
        };
    }

    private LLVMValueRef HandleString(LLVMValueRef leftString, LLVMValueRef rightString)
    {
        var lSize = builder.BuildExtractValue(leftString, 1);
        var rSize = builder.BuildExtractValue(rightString, 1);
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
            [newContent, builder.BuildExtractValue(leftString, 0), lSize]
        );
        // fill the second part of the string
        // first "increment" the pointer of the string to be after the contents of the first part
        var secondPart = builder.BuildInBoundsGEP2(LLVMTypeRef.Int8, newContent, [lSize]);
        builder.BuildCall2(
            context.CFuntions.memcpy.TypeOf,
            context.CFuntions.memcpy.Function,
            [secondPart, builder.BuildExtractValue(rightString, 0), rSize]
        );
        var String = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(_charStar),
                LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
            ],
            false
        );
        String = builder.BuildInsertValue(String, newContent, 0);
        String = builder.BuildInsertValue(String, newSize, 1);
        return String;
    }

    private LLVMValueRef HandleIntBoolOp(
        LLVMValueRef left, //L
        LLVMIntPredicate comparer, // OP
        LLVMValueRef right //R
    )
    {
        return builder.BuildICmp(comparer, left, right);
    }

    private LLVMValueRef HandleFloatBoolOp(
        LLVMValueRef left, //L
        LLVMRealPredicate comparer, // OP
        LLVMValueRef right //R
    )
    {
        return builder.BuildFCmp(comparer, left, right);
    }

    private LLVMValueRef CompileInteger(IntNode node)
    {
        return LLVMValueRef.CreateConstInt(
            LLVMTypeRef.Int64, //
            (ulong)node.Value
        );
    }

    private LLVMValueRef CompileReal(FloatNode node)
    {
        return LLVMValueRef.CreateConstReal(
            LLVMTypeRef.Double, //
            node.Value
        );
    }

    private void Error(string message, LLVMValueRef[] values)
    {
        builder.BuildCall2(
            context.CFuntions.printf.TypeOf,
            context.CFuntions.printf.Function,
            values.Prepend(builder.BuildGlobalStringPtr($"Error: {message}\n")).ToArray()
        );
        builder.BuildCall2(
            context.CFuntions.exit.TypeOf,
            context.CFuntions.exit.Function,
            [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1)]
        );
        builder.BuildUnreachable();
    }

    private void Error(string message, LLVMValueRef[] values, ASTNode node)
    {
        builder.BuildCall2(
            context.CFuntions.printf.TypeOf,
            context.CFuntions.printf.Function,
            values
                .Prepend(builder.BuildGlobalStringPtr($"Error: {message}, on line {node.Line}\n"))
                .ToArray()
        );
        builder.BuildCall2(
            context.CFuntions.exit.TypeOf,
            context.CFuntions.exit.Function,
            [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1)]
        );
        builder.BuildUnreachable();
    }

    private LLVMValueRef CompileEnum(VariableUsagePlainNode node)
    {
        return CompileExpression(node.Extension);
    }

    private LLVMValueRef CompileVariableUsage(VariableUsagePlainNode node, bool load = true)
    {
        if (node.ExtensionType == VariableUsagePlainNode.VrnExtType.Enum)
        {
            return CompileEnum(node);
        }

        LLVMValue value = context.GetVariable(node.MonomorphizedName());

        var variable = node.ExtensionType switch
        {
            VariableUsagePlainNode.VrnExtType.ArrayIndex => CompileArrayUsage(node, value),

            VariableUsagePlainNode.VrnExtType.RecordMember => CompileRecordUsage(node, value),

            VariableUsagePlainNode.VrnExtType.None => value,
        };
        return load ? CopyVariable(variable) : variable.ValueRef;
    }

    private LLVMValueRef CompileVariableUsageNew(VariableUsageNodeTemp node, bool load)
    {
        if (
            node is VariableUsagePlainNode
            {
                ExtensionType: VariableUsagePlainNode.VrnExtType.Enum
            } enumUsageNode
        )
        {
            return CompileEnum(enumUsageNode);
        }

        var variable = CompileVariableUsageNew(node);
        // TODO: remove load (and espicially) copy variable as they are only used for assignments (not for function calls)
        // this would make it that copy variable (which would only be used for assignments) would not have to build up a new value, but could just store in the current one (current variable the programmer was assigning to)
        // so essentially we delegate the reponsiblility of actually loading/copying to the caller (this would also mean making CompileExpression return LLVMValue)
        return load ? CopyVariable(variable) : variable.ValueRef;
    }

    private LLVMValue CompileVariableUsageNew(VariableUsageNodeTemp node)
    {
        // TODO:
        //this doesnt work for enums
        // how enums wokr with out this is by constant folding
        // store the Value of the enum which is an integer in the Extension type

        return node switch
        {
            VariableUsageIndexNode variableUsageIndexNode
                => CompileVariableUsageNew(variableUsageIndexNode),
            VariableUsageMemberNode variableUsageMemberNode
                => CompileVariableUsageNew(variableUsageMemberNode),
            VariableUsagePlainNode variableUsagePlainNode
                => context.GetVariable(variableUsagePlainNode.MonomorphizedName())
        };
    }

    private LLVMValue CompileVariableUsageNew(VariableUsageMemberNode node)
    {
        // Problem with big array (is not using pointer for non mutable aggregate parameters, we could just always pass load as true when doing function calls)
        // and then check if its aggregate type like array or struct if its not then load it after (and then there no need to copy)
        // or maybe (and/or) the fact that llvm doesn't like massive constants (initialization/copying - could be paratially elvated by making the constant global?)/or function calls with massive parameter counts
        // for initializtion look into memset - split things up into loops if we go past certain size?
        // memset I think would work in all cases (but you cannot memset a struct directly) (looking at what rustc does you basically have to do a loop when arrays are involved)
        var value = CompileVariableUsageNew(node.Left);
        if (value is LLVMReference r)
        {
            value = GetReferenceInner(r);
        }

        var varType = (LLVMStruct)value;
        var varField = node.Right;
        var dataType = varType.GetTypeOf(varField.Name);
        var structField = builder.BuildStructGEP2(
            value.TypeRef.TypeRef,
            value.ValueRef,
            (uint)varType.Access(varField.Name)
        );

        return dataType.IntoValue(structField, value.IsMutable);
    }

    private LLVMValue CompileVariableUsageNew(VariableUsageIndexNode node)
    {
        var value = CompileVariableUsageNew(node.Left);
        if (value is LLVMReference r)
        {
            value = GetReferenceInner(r);
        }

        var newValue = (LLVMArray)value;
        var arrayType = (LLVMArrayType)newValue.TypeRef;
        var length = newValue.TypeRef.TypeRef.ArrayLength;
        var arrayInnerType = ((LLVMArray)value).Inner();
        var elementType = arrayInnerType.TypeRef;
        var start = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (uint)arrayType.Range.From);

        var index = builder.BuildIntCast(CompileExpression(node.Right), LLVMTypeRef.Int32);

        var size = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, length);
        var indexOutOfBoundsBlock = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "index out of bounds"
        );
        var okIndexBlock = module.Context.AppendBasicBlock(context.CurrentFunction.Function, "ok");
        var smaller = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, index, start);
        var bigger = builder.BuildICmp(
            LLVMIntPredicate.LLVMIntSGT,
            index,
            builder.BuildAdd(start, size)
        );
        var goodIndex = builder.BuildOr(smaller, bigger);
        builder.BuildCondBr(goodIndex, indexOutOfBoundsBlock, okIndexBlock);
        builder.PositionAtEnd(indexOutOfBoundsBlock);
        // TODO: in the error say which side the index is out of bounds for
        Error(
            $"Index %d is out of bounds, for array {node.GetPlain().Name} of type {newValue.Inner()} of length {length} with the first index at {(int)arrayType.Range.From}",
            [index, size],
            node
        );
        builder.PositionAtEnd(okIndexBlock);
        var array = value.ValueRef;
        index = builder.BuildSub(index, start);
        array = builder.BuildInBoundsGEP2(
            arrayType.TypeRef,
            array,
            [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0), index]
        );
        return arrayInnerType.IntoValue(array, value.IsMutable);
    }

    // copy variable ensures value semantics for everything besides refernces (so basically if you assign one string variable to another string variable changing the second one does not change the first one)
    // we couldn't just do a memcpy because then string would not be copied becasue they are pointers
    // it also ensures that stack allocated memory does not escape the function it was allocated in
    // for large values this may segault (but we just ignore this currently)
    private LLVMValueRef CopyVariable(LLVMValue variable)
    {
        // not happening (should happen during semantic analysis) check for uninitialized access when doing this load
        var value = builder.BuildLoad2(variable.TypeRef.TypeRef, variable.ValueRef);
        return CopyVariable(variable.TypeRef, value);
    }

    private LLVMValueRef CopyVariable(LLVMType type, LLVMValueRef value) =>
        type switch
        {
            LLVMArrayType llvmArrayType => CopyArray(llvmArrayType, value),
            LLVMEnumType
            or LLVMCharacterType
            or LLVMBooleanType
            or LLVMIntegerType
            or LLVMRealType
            or LLVMReferenceType
                => value,
            LLVMStringType => CopyString(value),
            LLVMStructType llvmStructType => CopyStruct(llvmStructType, value),
        };

    private LLVMValueRef CopyArray(LLVMArrayType llvmArrayType, LLVMValueRef value)
    {
        // TODO: perhaps turn this into a loop instead of doing this as a "unrolled loop"
        return Enumerable
            .Range(0, (int)llvmArrayType.Range.Length)
            .Select(
                i =>
                    (
                        i,
                        CopyVariable(llvmArrayType.Inner, builder.BuildExtractValue(value, (uint)i))
                    )
            )
            .Aggregate(
                llvmArrayType.TypeRef.Undef,
                (array, element) => builder.BuildInsertValue(array, element.Item2, (uint)element.i)
            );
    }

    private LLVMValueRef CopyStruct(LLVMStructType llvmStructType, LLVMValueRef value)
    {
        return llvmStructType
            .Members.Values.Select(
                (type, index) =>
                    (index, CopyVariable(type, builder.BuildExtractValue(value, (uint)index)))
            )
            .Aggregate(
                llvmStructType.TypeRef.Undef,
                (record, current) =>
                    builder.BuildInsertValue(record, current.Item2, (uint)current.index)
            );
    }

    private LLVMValueRef CopyString(LLVMValueRef value)
    {
        var stringContent = builder.BuildExtractValue(value, 0);
        var stringLength = builder.BuildExtractValue(value, 1);
        var newStringContent = builder.BuildCall2(
            context.CFuntions.malloc.TypeOf,
            context.CFuntions.malloc.Function,
            [stringLength]
        );

        builder.BuildCall2(
            context.CFuntions.memcpy.TypeOf,
            context.CFuntions.memcpy.Function,
            [newStringContent, stringContent, stringLength]
        );
        var newString = builder.BuildInsertValue(LLVMType.StringType.Undef, newStringContent, 0);
        return builder.BuildInsertValue(newString, stringLength, 1);
    }

    private LLVMValue CompileRecordUsage(VariableUsagePlainNode node, LLVMValue value)
    {
        if (value is LLVMReference r)
        {
            value = GetReferenceInner(r);
        }

        var varType = (LLVMStruct)value;
        var varField = (VariableUsagePlainNode)node.GetExtensionSafe();
        var dataType = varType.GetTypeOf(varField.Name);
        var structField = builder.BuildStructGEP2(
            value.TypeRef.TypeRef,
            value.ValueRef,
            (uint)varType.Access(varField.Name)
        );

        return dataType.IntoValue(structField, value.IsMutable);
    }

    private LLVMValue GetReferenceInner(LLVMReference r)
    {
        var inner = builder.BuildStructGEP2(r.TypeRef.TypeRef, r.ValueRef, 0);
        inner = builder.BuildLoad2(LLVMTypeRef.CreatePointer(r.TypeOf.Inner.TypeRef, 0), inner);
        return r.TypeOf.Inner.IntoValue(inner, r.IsMutable);
    }

    private LLVMValue CompileArrayUsage(VariableUsagePlainNode node, LLVMValue value)
    {
        if (value is LLVMReference r)
        {
            value = GetReferenceInner(r);
        }

        var newValue = (LLVMArray)value;
        var arrayInnerType = ((LLVMArray)value).Inner();
        var elementType = arrayInnerType.TypeRef;
        var arrayType = (LLVMArrayType)newValue.TypeRef;
        var length = newValue.TypeRef.TypeRef.ArrayLength;
        var start = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (uint)arrayType.Range.From);

        var index = builder.BuildIntCast(CompileExpression(node.Extension), LLVMTypeRef.Int32);
        var size = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, length);
        var indexOutOfBoundsBlock = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "index out of bounds"
        );
        var okIndexBlock = module.Context.AppendBasicBlock(context.CurrentFunction.Function, "ok");
        var smaller = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, index, start);
        var bigger = builder.BuildICmp(
            LLVMIntPredicate.LLVMIntSGT,
            index,
            builder.BuildAdd(start, size)
        );
        var goodIndex = builder.BuildOr(smaller, bigger);
        builder.BuildCondBr(goodIndex, indexOutOfBoundsBlock, okIndexBlock);
        builder.PositionAtEnd(indexOutOfBoundsBlock);
        // TODO: in the error say which side the index is out of bounds for
        Error(
            $"Index %d is out of bounds, for array {node.Name} of type {newValue.Inner()} of length %d",
            [index, start],
            node
        );
        builder.PositionAtEnd(okIndexBlock);
        var array = value.ValueRef;
        index = builder.BuildSub(index, start);
        array = builder.BuildInBoundsGEP2(elementType, array, [index]);
        return arrayInnerType.IntoValue(array, value.IsMutable);
    }

    private LLVMValueRef CompileCharacter(CharNode node)
    {
        return LLVMValueRef.CreateConstInt(
            module.Context.Int8Type, //a
            node.Value
        );
    }

    private LLVMValueRef CompileBoolean(BoolNode node)
    {
        return LLVMValueRef.CreateConstInt(
            module.Context.Int1Type, //a
            (ulong)node.GetValueAsInt() //a
        );
    }

    private LLVMValueRef CompileString(StringNode node)
    {
        var stringLength = LLVMValueRef.CreateConstInt(
            module.Context.Int32Type,
            (ulong)node.Value.Length
        );

        var stringContent = builder.BuildGlobalStringPtr(node.Value);

        // if we never mutate the string part directly, meaning when we do assignment we assign it a new string struct, then we do not need to do this malloc,
        // and we could just insert, the string constant in the string struct, we could do this because we don't directly mutate the string,
        // and the way we currently define string constants, they must not be mutated
        // one problem is that constant llvm strings are null terminated
        var stringPointer = builder.BuildArrayMalloc(
            LLVMTypeRef.Int8,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)node.Value.Length)
        );
        builder.BuildCall2(
            context.CFuntions.memcpy.TypeOf,
            context.CFuntions.memcpy.Function,
            [stringPointer, stringContent, stringLength]
        );
        var String = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(_charStar),
                LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
            ],
            false
        );
        String = builder.BuildInsertValue(String, stringPointer, 0);
        String = builder.BuildInsertValue(String, stringLength, 1);
        return String;
    }

    private LLVMValueRef CompileMathExpression(MathOpNode node)
    {
        var left = CompileExpression(node.Left);
        var right = CompileExpression(node.Right);
        // TODO: don't use _types, once we add types to ExpressionNode
        var types = _types(left.TypeOf);
        return types switch
        {
            Types.Integer => HandleIntOp(left, node.Op, right),
            Types.Float => HandleFloatOp(left, node.Op, right),
            Types.String => HandleString(left, right),
            _ => throw new NotImplementedException()
        };
    }

    private LLVMValueRef CompileBooleanExpression(BooleanExpressionNode node)
    {
        var left = CompileExpression(node.Left);
        var right = CompileExpression(node.Right);
        return CompileBooleanExpression(node.Op, left, right);
    }

    private LLVMValueRef CompileBooleanExpression(
        BooleanExpressionNode.BooleanExpressionOpType op,
        LLVMValueRef left,
        LLVMValueRef right
    )
    {
        // TODO: enum equality (should arrays, structs, and references have equality defined for them)
        // for enums if we want more general comparison we cannot use pointers
        // TODO: don't use _types, once we add types to ExpressionNode
        var types = _types(left.TypeOf);
        return types switch
        {
            Types.Integer
                => HandleIntBoolOp(
                    left,
                    op switch
                    {
                        BooleanExpressionNode.BooleanExpressionOpType.eq
                            => LLVMIntPredicate.LLVMIntEQ,
                        BooleanExpressionNode.BooleanExpressionOpType.ne
                            => LLVMIntPredicate.LLVMIntNE,
                        BooleanExpressionNode.BooleanExpressionOpType.gt
                            => LLVMIntPredicate.LLVMIntSGT,
                        BooleanExpressionNode.BooleanExpressionOpType.lt
                            => LLVMIntPredicate.LLVMIntSLT,
                        BooleanExpressionNode.BooleanExpressionOpType.le
                            => LLVMIntPredicate.LLVMIntSLE,
                        BooleanExpressionNode.BooleanExpressionOpType.ge
                            => LLVMIntPredicate.LLVMIntSGE,
                    },
                    right
                ),
            Types.Float
                => HandleFloatBoolOp(
                    left,
                    op switch
                    {
                        BooleanExpressionNode.BooleanExpressionOpType.eq
                            => LLVMRealPredicate.LLVMRealOEQ,
                        BooleanExpressionNode.BooleanExpressionOpType.ne
                            => LLVMRealPredicate.LLVMRealONE,
                        BooleanExpressionNode.BooleanExpressionOpType.gt
                            => LLVMRealPredicate.LLVMRealOLT,
                        BooleanExpressionNode.BooleanExpressionOpType.lt
                            => LLVMRealPredicate.LLVMRealOLT,
                        BooleanExpressionNode.BooleanExpressionOpType.le
                            => LLVMRealPredicate.LLVMRealOLE,
                        BooleanExpressionNode.BooleanExpressionOpType.ge
                            => LLVMRealPredicate.LLVMRealOGE,
                    },
                    right
                ),
            // booleans can only be compared using equality
            Types.Boolean
                => builder.BuildICmp(
                    op is BooleanExpressionNode.BooleanExpressionOpType.eq
                        ? LLVMIntPredicate.LLVMIntEQ
                        : LLVMIntPredicate.LLVMIntNE,
                    left,
                    right
                ),
            Types.String => HandleStringComparison(left, op, right),
            _ => throw new NotImplementedException()
        };
    }

    private LLVMValueRef HandleStringComparison(
        LLVMValueRef left,
        BooleanExpressionNode.BooleanExpressionOpType operation,
        LLVMValueRef right
    )
    {
        // string comparison is lexicographical unless the strings differ in size (in that case the bigger one is the one with a bigger size)
        var leftLength = builder.BuildExtractValue(left, 1);
        var rightLength = builder.BuildExtractValue(right, 1);
        var sizeDifference = builder.BuildSub(leftLength, rightLength);
        var sameLength = builder.BuildICmp(
            LLVMIntPredicate.LLVMIntEQ,
            sizeDifference,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0)
        );
        var lexicographicDifference = builder.BuildCall2(
            context.CFuntions.memcmp.TypeOf,
            context.CFuntions.memcmp.Function,
            [
                builder.BuildExtractValue(left, 0),
                builder.BuildExtractValue(right, 0),
                builder.BuildSelect(
                    builder.BuildICmp(
                        LLVMIntPredicate.LLVMIntSLT,
                        sizeDifference,
                        LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0)
                    ),
                    leftLength,
                    rightLength
                )
            ]
        );
        var stringComparison = builder.BuildSelect(
            sameLength,
            lexicographicDifference,
            sizeDifference
        );
        return builder.BuildICmp(
            operation switch
            {
                BooleanExpressionNode.BooleanExpressionOpType.lt => LLVMIntPredicate.LLVMIntSLT,
                BooleanExpressionNode.BooleanExpressionOpType.le => LLVMIntPredicate.LLVMIntSLE,
                BooleanExpressionNode.BooleanExpressionOpType.gt => LLVMIntPredicate.LLVMIntSGT,
                BooleanExpressionNode.BooleanExpressionOpType.ge => LLVMIntPredicate.LLVMIntSGE,
                BooleanExpressionNode.BooleanExpressionOpType.eq => LLVMIntPredicate.LLVMIntEQ,
                BooleanExpressionNode.BooleanExpressionOpType.ne => LLVMIntPredicate.LLVMIntNE,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            },
            stringComparison,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0)
        );
    }

    private void CompileRecord(RecordNode node)
    {
        // this cannot be done from the prototype because we cannot attach llvm types to Type without putting dependency of llvm for the Types file
        // also we do not know the order by which types are added to the llvm module
        var record = context.Records[node.Type.MonomorphizedIndex];
        var members = node.Type.Fields.Select(
            s =>
                (
                    s.Key,
                    // we are assuming that (mutually) recursive records are behind a refersTo (which already has a level of indirection, so we don't have to worry about llvm being cranky about recursive structs)
                    context.GetLLVMTypeFromShankType(s.Value)
                )
        )
            .ToDictionary();
        record.Members = members;
        record.TypeRef.StructSetBody(members.Select(s => s.Value.TypeRef).ToArray(), false);
    }

    private void CompileFunctionCall(FunctionCallNode node)
    {
        {
            var function = context.GetFunction(node.MonomphorizedFunctionLocater);

            var parameters = node.Arguments.Zip(function.Parameters)
                .Select(
                    argumentAndMutability =>
                        CompileExpression(
                            argumentAndMutability.First,
                            // for struct and arrays and really anything else we do not have to copy anything for function call because if they are marked var they need to be mutated, if they are not then the function cannot mutate them - and any time it assign part of this to a different value that will get copied
                            !argumentAndMutability.Second.Mutable
                                && argumentAndMutability.Second.Type
                                    is not (LLVMArrayType or LLVMStructType)
                        )
                );
            // function.
            builder.BuildCall2(function.TypeOf, function.Function, parameters.ToArray());
        }
    }

    private void CompileFunction(FunctionNode node)
    {
        var function = context.GetFunction(node.MonomorphizedName);
        context.CurrentFunction = function;
        context.ResetLocal();
        var block = function.AppendBasicBlock("entry");
        builder.PositionAtEnd(block);
        foreach (
            var (param, index) in node.ParameterVariables.Select((param, index) => (param, index))
        )
        {
            if (param.IsConstant && param.Type is not (ArrayType or RecordType))
            {
                var llvmParam = function.GetParam((uint)index);
                var name = param.GetNameSafe();
                LLVMValueRef paramAllocation = builder.BuildAlloca(llvmParam.TypeOf, name);
                var parameter = context.NewVariable(param.Type)(
                    paramAllocation, //a
                    !param.IsConstant
                );
                builder.BuildStore(llvmParam, paramAllocation);
                context.AddVariable(param.MonomorphizedName(), parameter);
            }
            else
            {
                var llvmParam = function.GetParam((uint)index);
                var parameter = context.NewVariable(param.Type)(
                    llvmParam, //a
                    !param.IsConstant
                );
                context.AddVariable(param.MonomorphizedName(), parameter);
            }
        }

        function.Linkage = LLVMLinkage.LLVMExternalLinkage;

        node.LocalVariables.ForEach(CompileVariableDeclaration);
        if (function.Name == "main")
        {
            var seed = builder.BuildCall2(
                context.CFuntions.time.TypeOf,
                context.CFuntions.time.Function,
                [LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int64, 0))]
            );
            builder.BuildStore(
                builder.BuildIntCast(seed, LLVMTypeRef.Int64),
                context.RandomVariables.S1
            );
        }

        node.Statements.ForEach(CompileStatement);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
        context.ResetLocal();
    }

    private void CompileWhile(WhileNode node)
    {
        var whileCond = context.CurrentFunction.AppendBasicBlock("while.cond");
        var whileBody = context.CurrentFunction.AppendBasicBlock("while.body");
        var whileDone = context.CurrentFunction.AppendBasicBlock("while.done");
        builder.BuildBr(whileCond);
        builder.PositionAtEnd(whileCond);
        var condition = CompileExpression(node.Expression);
        builder.BuildCondBr(condition, whileBody, whileDone);
        builder.PositionAtEnd(whileBody);
        node.Children.ForEach(CompileStatement);
        builder.BuildBr(whileCond);
        builder.PositionAtEnd(whileDone);
    }

    private void CompileStatement(StatementNode node)
    {
        switch (node)
        {
            case AssignmentNode assignmentNode:
                CompileAssignment(assignmentNode);
                break;
            case ForNode forNode:
                CompileFor(forNode);
                break;
            case FunctionCallNode functionCallNode:
                CompileFunctionCall(functionCallNode);
                break;
            case IfNode ifNode:
                CompileIf(ifNode);
                break;
            case RepeatNode repeatNode:
                CompileRepeat(repeatNode);
                break;
            case WhileNode whileNode:
                CompileWhile(whileNode);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(node));
        }
    }

    private void CompileAssignment(AssignmentNode node)
    {
        var llvmValue = options.VuOpTest
            ? context.GetVariable(node.NewTarget.GetPlain().MonomorphizedName())
            : context.GetVariable(node.Target.MonomorphizedName());
        var expression = CompileExpression(node.Expression);
        var target = CompileExpression(options.VuOpTest ? node.NewTarget : node.Target, false);
        if (!llvmValue.IsMutable)
        {
            throw new Exception($"tried to mutate non mutable variable {node.Target.Name}");
        }

        builder.BuildStore(expression, target);
    }

    private void CompileIf(IfNode node)
    {
        if (node.Expression != null)
        // if the condition is null then it's an else statement, which can
        // only happen after an if statement
        // so is it's an if statement, and since we compile
        // if statements recursively, like how we parse them
        // we know that we already created the block for the
        // else statement, when compiling the if part,
        // so we just compile the statements in the else block
        // if the condition is not null we compile the condition,
        // create two blocks one for if it's true, and for when the condition is false
        // we then just compile the statements for when the condition
        // is true under the true block, followed by a goto to an after block
        // and we visit(compile) the IfNode for when the condition is false
        // if needed, followed by a goto to the after branch
        // note we could make this a bit better by checking if next is null and then make the conditional branch to after block in the false cas
        {
            var condition = CompileExpression(node.Expression);
            var ifBlock = context.CurrentFunction.AppendBasicBlock("if block");
            var elseBlock = context.CurrentFunction.AppendBasicBlock("else block");
            var afterBlock = context.CurrentFunction.AppendBasicBlock("after if statement");
            builder.BuildCondBr(condition, ifBlock, elseBlock);

            builder.PositionAtEnd(ifBlock);
            node.Children.ForEach(CompileStatement);
            builder.BuildBr(afterBlock);
            builder.PositionAtEnd(elseBlock);
            if (node.NextIfNode is { } nonnull)
            {
                CompileIf(nonnull);
            }

            builder.BuildBr(afterBlock);
            builder.PositionAtEnd(afterBlock);
        }
        else
        {
            node.Children.ForEach(CompileStatement);
        }
    }

    private void CompileRepeat(RepeatNode node)
    {
        var whileBody = context.CurrentFunction.AppendBasicBlock("while.body");
        var whileDone = context.CurrentFunction.AppendBasicBlock("while.done");
        // first execute the body
        builder.BuildBr(whileBody);
        builder.PositionAtEnd(whileBody);
        node.Children.ForEach(CompileStatement);
        // and then test the condition
        var condition = CompileExpression(node.Expression);
        builder.BuildCondBr(condition, whileBody, whileDone);
        builder.PositionAtEnd(whileDone);
    }

    private void CompileVariableDeclaration(VariableDeclarationNode node)
    {
        var name = node.GetNameSafe();
        // only alloca when !isConstant (is somewhat problematic with structs)

        var llvmTypeFromShankType = context.GetLLVMTypeFromShankType(node.Type);
        LLVMValueRef v = builder.BuildAlloca(
            // isVar is false, because we are already creating it using alloca which makes it var
            llvmTypeFromShankType.TypeRef,
            name
        );
        if (node.InitialValue is { } init)
        {
            var initLLVM = CompileExpression(node.InitialValue);
            builder.BuildStore(initLLVM, v);
        }
        else
        {
            // TODO: if we used memset for enum we are assuming the first enum variant is zero
            builder.BuildCall2(
                context.CFuntions.memset.TypeOf,
                context.CFuntions.memset.Function,
                [
                    builder.BuildBitCast(v, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)),
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                    builder.BuildIntCast(llvmTypeFromShankType.TypeRef.SizeOf, LLVMTypeRef.Int32)
                ]
            );
            // builder.BuildStore(CompileDefaultExpression(llvmTypeFromShankType), v);
            // CompileDefaultExpression(llvmTypeFromShankType, v);
        }

        // TODO: preallocate arrays in records to (might need to be recursive)
        // if (node.Type is ArrayType a && llvmTypeFromShankType is LLVMArrayType shankType)
        // {
        //     var arraySize = LLVMValueRef.CreateConstInt(
        //         LLVMTypeRef.Int32,
        //         (ulong)(a.Range.To - a.Range.From)
        //     );
        //     // var arrayStart = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)a.Range.From);
        //     var arrayAllocation = builder.BuildArrayAlloca(shankType.Inner.TypeRef, arraySize);
        //     // var array = builder.BuildInsertValue(
        //     //     LLVMValueRef.CreateConstStruct(
        //             // [LLVMValueRef.CreateConstNull(arrayAllocation.TypeOf), arrayStart, arraySize],
        //             // false
        //     //     ),
        //     //     arrayAllocation,
        //     //     0
        //     // );
        //     builder.BuildStore(arrayAllocation, v);
        // }
        //
        // TODO: NewVariableCalls GetLLVMTypeForShankType even though in order to allocate we need to already know the type so we call the aforementioned function,
        // maybe the aforementioned function should take a function that takes a type and gives back an LLVMValueRef (so it would work here, but also for parameters)
        var variable = context.NewVariable(node.Type);
        context.AddVariable(node.MonomorphizedName(), variable(v, !node.IsConstant));
    }

    private void CompileDefaultExpression(LLVMType llvmTypeFromShankType, LLVMValueRef variable)
    {
        switch (llvmTypeFromShankType)
        {
            case LLVMArrayType arrayType:
                CompileDefaultExpression(arrayType, variable);
                break;
            case LLVMReferenceType referenceType:
                builder.BuildStore(
                    LLVMValueRef.CreateConstStruct(
                        [
                            LLVMValueRef.CreateConstPointerNull(referenceType.Inner.TypeRef),
                            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)
                        ],
                        false
                    ),
                    variable
                );
                break;
            case LLVMStringType:
                builder.BuildStore(
                    LLVMValueRef.CreateConstStruct(
                        [
                            LLVMValueRef.CreateConstArray(LLVMTypeRef.Int8, []),
                            // TODO: should this be a malloc
                            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                        ],
                        false
                    ),
                    variable
                );
                break;
            case LLVMStructType structType:
                CompileDefaultExpression(structType, variable);
                break;
            case LLVMRealType type:
                builder.BuildStore(LLVMValueRef.CreateConstReal(type.TypeRef, 0.0), variable);
                break;
            // TODO: first declared variant
            case LLVMEnumType type:
                builder.BuildStore(LLVMValueRef.CreateConstInt(type.TypeRef, 0), variable);
                break;
            case LLVMIntegerType type:
                builder.BuildStore(LLVMValueRef.CreateConstInt(type.TypeRef, 0), variable);
                break;
            case LLVMCharacterType type:
                builder.BuildStore(LLVMValueRef.CreateConstInt(type.TypeRef, 0), variable);
                break;
            case LLVMBooleanType type:
                builder.BuildStore(LLVMValueRef.CreateConstInt(type.TypeRef, 0), variable);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(llvmTypeFromShankType));
        }
        ;
    }

    private void CompileDefaultExpression(LLVMStructType type, LLVMValueRef variable)
    {
        foreach (
            var (llvmType, index) in type.Members.Values.Select((llvmType, i) => (llvmType, i))
        )
        {
            var field = builder.BuildStructGEP2(type.TypeRef, variable, (uint)index);
            CompileDefaultExpression(llvmType, field);
        }
    }

    private LLVMValueRef CreateEntryBlockAlloca(LLVMTypeRef type)
    {
        var currentInsertionPoint = builder.InsertBlock;
        var entry = context.CurrentFunction.Function.EntryBasicBlock;
        if (entry.FirstInstruction is { } inst)
        {
            builder.PositionBefore(inst);
        }
        else
        {
            builder.PositionAtEnd(entry);
        }

        LLVMValueRef lLVMValueRef = builder.BuildAlloca(type);
        builder.PositionAtEnd(currentInsertionPoint);
        return lLVMValueRef;
    }

    private void CompileDefaultExpression(LLVMArrayType type, LLVMValueRef variable)
    {
        var inner = CreateEntryBlockAlloca(type.Inner.TypeRef);
        CompileDefaultExpression(type.Inner, inner);
        var inneri8 = builder.BuildBitCast(inner, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));
        var lastElement = builder.BuildInBoundsGEP2(
            type.TypeRef,
            variable,
            [
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0),
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)type.Range.Length)
            ]
        );
        var firstElement = builder.BuildInBoundsGEP2(
            type.TypeRef,
            variable,
            [
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0),
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0)
            ]
        );
        var repeatLoopHeader = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "repeat_loop_header"
        );
        var repeatLoopBody = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "repeat_loop_body"
        );
        var repeatLoopNext = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "repeat_loop_next"
        );
        var startBlock = builder.InsertBlock;
        builder.BuildBr(repeatLoopHeader);
        builder.PositionAtEnd(repeatLoopHeader);
        var currentElementPhi = builder.BuildPhi(LLVMTypeRef.CreatePointer(type.Inner.TypeRef, 0));
        var done = builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, currentElementPhi, lastElement);
        builder.BuildCondBr(done, repeatLoopBody, repeatLoopNext);
        builder.PositionAtEnd(repeatLoopBody);

        // should this be i8*
        var elementi8 = builder.BuildBitCast(
            currentElementPhi,
            LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)
        );
        builder.BuildCall2(
            context.CFuntions.memcpy.TypeOf,
            context.CFuntions.memcpy.Function,
            [elementi8, inneri8, builder.BuildIntCast(type.TypeRef.SizeOf, LLVMTypeRef.Int32)]
        );
        var next = builder.BuildInBoundsGEP2(
            type.Inner.TypeRef,
            currentElementPhi,
            [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1)]
        );
        currentElementPhi.AddIncoming([firstElement, next], [startBlock, repeatLoopBody], 2);
        builder.BuildBr(repeatLoopHeader);
        builder.PositionAtEnd(repeatLoopNext);
    }

    public void Compile(MonomorphizedProgramNode node)
    {
        var protoTypeCompiler = new PrototypeCompiler(context, builder, module);
        protoTypeCompiler.CompilePrototypes(node);

        node.Records.Values.ToList().ForEach(CompileRecord);
        node.GlobalVariables.Values.ToList().ForEach(CompileGlobalVariables);
        node.Functions.Values.ToList().ForEach(CompileFunction);
        node.BuiltinFunctions.Values.ToList().ForEach(CompileBuiltinFunction);
    }

    private void CompileGlobalVariables(VariableDeclarationNode node)
    {
        var variable = context.GetVariable((ModuleIndex)node.MonomorphizedName());
        if (node.InitialValue is not null)
        {
            var content = node.InitialValue! switch
            {
                IntNode n => CompileInteger(n),
                FloatNode f => CompileReal(f),
                BoolNode b => CompileBoolean(b),
                StringNode s => CompileConstantString(s),
                CharNode c => CompileCharacter(c),
            };
            variable.ValueRef = variable.ValueRef with { Initializer = content };
        }
    }

    private LLVMValueRef CompileConstantString(StringNode s)
    {
        // this is basically what builder.BuildGlobalStringPtr does, just without the builder - as It's for constants
        // and builders need a basic block which requires a function
        // see https://llvm.org/doxygen/IRBuilder_8cpp_source.html line 44 and line 1997 (definition of BuildGlobalStringPtr)

        // create the actual constant array of bytes corresponding to the string
        var stringContent = module.Context.GetConstString(s.Value, false);
        // hold the array in separate global, so that we can gep it as a pointer (you can't gep a raw array)
        var stringVariable = module.AddGlobal(stringContent.TypeOf, s.Value + "Content");
        stringVariable.Initializer = stringContent;
        // now gep the array which is the global variable, this is done to convert the array to an i8*
        var int32 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
        stringContent = LLVMValueRef.CreateConstInBoundsGEP2(
            stringContent.TypeOf,
            stringVariable,
            [int32, int32]
        );

        var size = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)s.Value.Length);
        return LLVMValueRef.CreateConstStruct([stringContent, size], false);
    }

    private void CompileFor(ForNode node)
    {
        var forStart = context.CurrentFunction.AppendBasicBlock("for.start");
        var afterFor = context.CurrentFunction.AppendBasicBlock("for.after");
        var forBody = context.CurrentFunction.AppendBasicBlock("for.body");
        var forIncrement = context.CurrentFunction.AppendBasicBlock("for.inc");
        var variable = options.VuOpTest ? node.NewVariable : node.Variable;
        var mutableCurrentIterable = CompileExpression(variable, false);
        var fromValue = CompileExpression(node.From);
        builder.BuildStore(fromValue, mutableCurrentIterable);

        builder.BuildBr(forStart);
        builder.PositionAtEnd(forStart);
        // we have to compile the to and from in the loop so that the get run each time, we go through the loop
        // in case we modify them in the loop


        var toValue = CompileExpression(node.To);

        var currentIterable = CompileExpression(variable);
        // right now we assume, from, to, and the variable are all integers
        // in the future we should check and give some error at runtime/compile time if not
        var condition = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, currentIterable, toValue);
        builder.BuildCondBr(condition, forBody, afterFor);
        builder.PositionAtEnd(forBody);
        node.Children.ForEach(CompileStatement);
        builder.BuildBr(forIncrement);
        builder.PositionAtEnd(forIncrement);

        var incremented = builder.BuildAdd(
            currentIterable,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1)
        );
        builder.BuildStore(incremented, mutableCurrentIterable);
        builder.BuildBr(forStart);
        builder.PositionAtEnd(afterFor);
    }

    private void CompileBuiltinFunction(BuiltInFunctionNode node)
    {
        var function = context.BuiltinFunctions[(TypedBuiltinIndex)node.MonomorphizedName];
        context.CurrentFunction = function;
        var block = function.AppendBasicBlock("entry");
        builder.PositionAtEnd(block);

        switch (node.GetBuiltIn())
        {
            case BuiltInFunction.Write:
                CompileBuiltinWrite(function);
                break;
            case BuiltInFunction.Substring:
                CompileBuiltinSubString(function);
                break;
            case BuiltInFunction.RealToInt:
                CompileBuiltinRealToInteger(function);
                break;
            case BuiltInFunction.IntToReal:
                CompileBuiltinIntegerToReal(function);
                break;
            case BuiltInFunction.Read:
                CompileBuiltinRead(function);
                break;
            case BuiltInFunction.AllocateMem:
                CompileBuiltinAllocateMemory(function);
                break;
            case BuiltInFunction.FreeMem:
                CompileBuiltinFreeMemory(function);
                break;
            case BuiltInFunction.High:
                CompileBuiltinHigh(function);
                break;
            case BuiltInFunction.Low:
                CompileBuiltinLow(function);
                break;
            case BuiltInFunction.IsSet:
                CompileBuiltinIsSet(function);
                break;
            case BuiltInFunction.Left:
                CompileBuiltinLeft(function);
                break;
            case BuiltInFunction.Right:
                CompileBuiltinRight(function);
                break;
            case BuiltInFunction.Size:
                CompileBuiltinSize(function);
                break;
            case BuiltInFunction.AssertIsEqual:
                CompileBuiltinAssertIsEqual(function);
                break;
            case BuiltInFunction.GetRandom:
                CompileBuiltinGetRandom(function);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void CompileBuiltinGetRandom(LLVMShankFunction function)
    {
        var randomVariables = context.RandomVariables;
        var s1 = builder.BuildLoad2(LLVMTypeRef.Int64, randomVariables.S1);
        // rotl(s1 * 5, 7)
        var rotl0 = rotl(builder.BuildMul(s1, CreateConstInt(5)), CreateConstInt(7));
        // result = rotl * 9
        var result = builder.BuildMul(rotl0, CreateConstInt(9));
        // t = s1 << 17
        var t = builder.BuildShl(s1, CreateConstInt(17));
        // s2 ^= s0 (s2 = s2 ^ s0)
        builder.BuildStore(
            builder.BuildXor(
                builder.BuildLoad2(LLVMTypeRef.Int64, randomVariables.S2),
                builder.BuildLoad2(LLVMTypeRef.Int64, randomVariables.S0)
            ),
            randomVariables.S2
        );
        // s3 ^= s1
        builder.BuildStore(
            builder.BuildXor(
                builder.BuildLoad2(LLVMTypeRef.Int64, randomVariables.S3),
                builder.BuildLoad2(LLVMTypeRef.Int64, randomVariables.S1)
            ),
            randomVariables.S3
        );
        // s1 ^= s2
        builder.BuildStore(
            builder.BuildXor(
                builder.BuildLoad2(LLVMTypeRef.Int64, randomVariables.S1),
                builder.BuildLoad2(LLVMTypeRef.Int64, randomVariables.S2)
            ),
            randomVariables.S1
        );
        // s0 ^= s3
        builder.BuildStore(
            builder.BuildXor(
                builder.BuildLoad2(LLVMTypeRef.Int64, randomVariables.S0),
                builder.BuildLoad2(LLVMTypeRef.Int64, randomVariables.S3)
            ),
            randomVariables.S0
        );
        // s2 ^= t
        builder.BuildStore(
            builder.BuildXor(builder.BuildLoad2(LLVMTypeRef.Int64, randomVariables.S2), t),
            randomVariables.S2
        );
        // rotl(s3, 45)
        var rotl1 = rotl(
            builder.BuildLoad2(LLVMTypeRef.Int64, randomVariables.S3),
            CreateConstInt(45)
        );
        // s3 = rotl
        builder.BuildStore(rotl1, randomVariables.S3);
        // return result
        builder.BuildStore(result, function.Function.FirstParam);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));

        LLVMValueRef CreateConstInt(ulong n)
        {
            return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, n);
        }

        // rotl(x, k) = (x << k) | (x >> (64 - k))
        LLVMValueRef rotl(LLVMValueRef x, LLVMValueRef k)
        {
            return builder.BuildOr(
                builder.BuildShl(x, k),
                builder.BuildAShr(x, builder.BuildSub(CreateConstInt(64), k))
            );
        }
    }

    private void CompileBuiltinRead(LLVMShankFunction function)
    {
        // TODO: should read keep the \n
        var output = function.Function.FirstParam;
        var readBb = module.Context.AppendBasicBlock(function.Function, "read char");
        var reallocateBb = module.Context.AppendBasicBlock(function.Function, "reallocate");
        var checkDoneBb = module.Context.AppendBasicBlock(function.Function, "check done");
        var doneBb = module.Context.AppendBasicBlock(function.Function, "done");
        // init part
        var size = builder.BuildAlloca(LLVMTypeRef.Int32);
        builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 128), size);
        var content = builder.BuildAlloca(_charStar);
        builder.BuildStore(
            builder.BuildCall2(
                context.CFuntions.malloc.TypeOf,
                context.CFuntions.malloc.Function,
                [builder.BuildLoad2(LLVMTypeRef.Int32, size)]
            ),
            content
        );
        var index = builder.BuildAlloca(LLVMTypeRef.Int32);
        var int32 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
        builder.BuildStore(int32, index);
        // read part
        builder.BuildBr(readBb);
        builder.PositionAtEnd(readBb);
        var read = builder.BuildCall2(
            context.CFuntions.getchar.TypeOf,
            context.CFuntions.getchar.Function,
            []
        );
        var loadedIndex = builder.BuildLoad2(LLVMTypeRef.Int32, index);
        var reallocate = builder.BuildICmp(
            LLVMIntPredicate.LLVMIntSGT,
            loadedIndex,
            builder.BuildLoad2(LLVMTypeRef.Int32, size)
        );
        builder.BuildCondBr(reallocate, reallocateBb, checkDoneBb);
        // reallocation
        builder.PositionAtEnd(reallocateBb);
        var int33 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1);
        var newSize = builder.BuildLShr(builder.BuildLoad2(LLVMTypeRef.Int32, size), int33);
        var newContent = builder.BuildCall2(
            context.CFuntions.realloc.TypeOf,
            context.CFuntions.realloc.Function,
            [builder.BuildLoad2(_charStar, content), newSize]
        );
        builder.BuildStore(newContent, content);
        builder.BuildStore(newSize, size);
        builder.BuildBr(checkDoneBb);
        // updating string and checking if we are done
        builder.PositionAtEnd(checkDoneBb);
        var current = builder.BuildInBoundsGEP2(
            LLVMTypeRef.Int8,
            builder.BuildLoad2(_charStar, content),
            [loadedIndex]
        );
        builder.BuildStore(read, current);
        builder.BuildStore(builder.BuildAdd(loadedIndex, int33), index);
        var isNewline = builder.BuildICmp(
            LLVMIntPredicate.LLVMIntEQ,
            read,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, '\n')
        );
        builder.BuildCondBr(isNewline, doneBb, readBb);
        builder.PositionAtEnd(doneBb);
        var newString = LLVMType.StringType.Undef;
        newString = builder.BuildInsertValue(newString, builder.BuildLoad2(_charStar, content), 0);
        newString = builder.BuildInsertValue(
            newString,
            builder.BuildLoad2(LLVMTypeRef.Int32, size),
            1
        );
        builder.BuildStore(newString, output);
        builder.BuildRet(int32);
    }

    private void CompileBuiltinHigh(LLVMShankFunction function)
    {
        // we have to get the runtime array start and size as opposed to just looking at the signature of the function because we do not monomorphize over the type limits
        var array = function.Function.FirstParam;
        var end = function.Function.GetParam(1);
        var arrayStart = builder.BuildExtractValue(array, 1);
        var arraySize = builder.BuildExtractValue(array, 2);
        var arrayEnd = builder.BuildAdd(arrayStart, arraySize);
        arrayEnd = builder.BuildIntCast(arrayEnd, LLVMTypeRef.Int64);
        builder.BuildStore(arrayEnd, end);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinLow(LLVMShankFunction function)
    {
        // we have to get the runtime array start as opposed to just looking at the signature of the function because we do not monomorphize over the type limits
        var array = function.Function.FirstParam;
        var start = function.Function.GetParam(1);
        var arrayStart = builder.BuildExtractValue(array, 1);
        arrayStart = builder.BuildIntCast(arrayStart, LLVMTypeRef.Int64);
        builder.BuildStore(arrayStart, start);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinSize(LLVMShankFunction function)
    {
        // we don't need the actual reference as llvm type of the reference can be determined based on the signature of the function (effectively this is computed at compile time)
        // var memory = function.Function.FirstParam;

        var size = function.Function.GetParam(1);
        var type = (LLVMReferenceType)function.Parameters.First().Type;
        var llvmTypeOfReference = type.Inner.TypeRef;
        var referenceSize = llvmTypeOfReference.SizeOf;
        builder.BuildStore(referenceSize, size);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinIsSet(LLVMShankFunction function)
    {
        var memory = function.Function.FirstParam;
        var isSet = function.Function.GetParam(1);
        var set = builder.BuildExtractValue(memory, 2);
        builder.BuildStore(set, isSet);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinAllocateMemory(LLVMShankFunction function)
    {
        // TODO: do references need to keep their size for `size` builtin?
        var param = function.Function.FirstParam;

        var type = (LLVMReferenceType)function.Parameters.First().Type;
        var llvmTypeOfReference = type.Inner.TypeRef;

        var size = llvmTypeOfReference.SizeOf;
        size = builder.BuildIntCast(size, LLVMTypeRef.Int32);
        var memory = builder.BuildMalloc(llvmTypeOfReference);

        // TODO: if we used memset for enum we are assuming the first enum variant is zero
        builder.BuildCall2(
            context.CFuntions.memset.TypeOf,
            context.CFuntions.memset.Function,
            [
                builder.BuildBitCast(memory, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)),
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                builder.BuildIntCast(llvmTypeOfReference.SizeOf, LLVMTypeRef.Int32)
            ]
        );

        var newReference = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(llvmTypeOfReference, 0)),
                size,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1)
            ],
            false
        );
        newReference = builder.BuildInsertValue(newReference, memory, 0);
        builder.BuildStore(newReference, param);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinFreeMemory(LLVMShankFunction function)
    {
        var param = function.Function.FirstParam;

        var type = (LLVMReferenceType)function.Parameters.First().Type;
        var llvmTypeOfReference = type.TypeRef;
        var llvmTypeOfReferenceInner = type.Inner.TypeRef;

        var memory = builder.BuildLoad2(
            LLVMTypeRef.CreatePointer(llvmTypeOfReference, 0),
            builder.BuildStructGEP2(llvmTypeOfReference, param, 0)
        );
        builder.BuildFree(memory);

        var int32 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
        var newReference = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(
                    LLVMTypeRef.CreatePointer(llvmTypeOfReferenceInner, 0)
                ),
                int32,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)
            ],
            false
        );
        builder.BuildStore(newReference, param);
        builder.BuildRet(int32);
    }

    private LLVMValueRef HandleEnum(LLVMValueRef valueRef, List<string> list, int index = 0)
    {
        if (index >= list.Count)
        {
            return builder.BuildSelect(
                builder.BuildICmp(
                    LLVMIntPredicate.LLVMIntEQ,
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)list.Count),
                    valueRef
                ),
                builder.BuildGlobalStringPtr(list[0]),
                builder.BuildGlobalStringPtr("error")
            );
        }

        return builder.BuildSelect(
            builder.BuildICmp(
                LLVMIntPredicate.LLVMIntEQ,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)index),
                valueRef
            ),
            builder.BuildGlobalStringPtr(list[index]),
            HandleEnum(valueRef, list, index + 1)
        );
    }

    private void CompileBuiltinWrite(LLVMShankFunction function)
    {
        var formatList = function.Parameters.Select(param => param.Type).Select(GetFormatCode);

        var format = $"{string.Join(" ", formatList)}\n";
        var paramList = function
            .Parameters.Select(param => param.Type)
            .Zip(function.Function.Params)
            .Select(
                p =>
                    (
                        p.First,
                        p.First is LLVMArrayType or LLVMStructType
                            ? builder.BuildLoad2(p.First.TypeRef, p.Second)
                            : p.Second
                    )
            )
            .SelectMany(GetValues)
            .Prepend(builder.BuildGlobalStringPtr(format, "printf-format"));
        builder.BuildCall2(
            context.CFuntions.printf.TypeOf,
            context.CFuntions.printf.Function,
            paramList.ToArray()
        );
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private IEnumerable<LLVMValueRef> GetValues((LLVMType First, LLVMValueRef Second) varValue) =>
        varValue.First switch
        {
            LLVMEnumType Enum => [HandleEnum(varValue.Second, Enum.Variants)],
            LLVMIntegerType or LLVMRealType or LLVMCharacterType => [varValue.Second],
            LLVMBooleanType
                =>
                [
                    builder.BuildSelect(
                        varValue.Second,
                        builder.BuildGlobalStringPtr("true"),
                        builder.BuildGlobalStringPtr("false")
                    )
                ],
            LLVMStringType
                =>
                [
                    builder.BuildExtractValue(varValue.Second, 1),
                    builder.BuildExtractValue(varValue.Second, 0)
                ],
            LLVMStructType record
                => record.Members.Values.SelectMany(
                    (member, index) =>
                        GetValues((member, builder.BuildExtractValue(varValue.Second, (uint)index)))
                ),
            LLVMReferenceType => [],
            LLVMArrayType array
                => Enumerable
                    .Range(0, (int)array.Range.Length)
                    .Take(15)
                    .SelectMany(
                        i =>
                            GetValues(
                                (array.Inner, builder.BuildExtractValue(varValue.Second, (uint)i))
                            )
                    ),
            _ => throw new NotImplementedException(varValue.First.ToString())
        };

    private static string GetFormatCode(LLVMType type) =>
        type switch
        {
            LLVMBooleanType => "%s",
            LLVMCharacterType => "%c",
            LLVMIntegerType => "%d",
            LLVMEnumType => "%s",
            LLVMRealType => "%.2f",
            LLVMStringType => "%.*s",
            LLVMArrayType Array
                => $"[ {string.Join(", ", Enumerable.Repeat(GetFormatCode(Array.Inner), (int)Array.Range.Length).Take(15))} ] ",
            LLVMStructType record
                => $"{record.Name}: {{ {string.Join(", ", record
                    .Members.Select(member => $"{member.Key} = {GetFormatCode(member.Value)}"))} }}",
            // TODO: print only one level
            LLVMReferenceType reference => reference.ToString(),
            _ => throw new CompilerException($"type is undefinedv {type} in function write", 0)
        };

    private void CompileBuiltinSubString(LLVMShankFunction function)
    {
        // SubString someString, index, length, var resultString

        // resultString = length characters from someString, starting at index
        var someString = function.Function.GetParam(0);
        var index = function.Function.GetParam(1);
        var length = function.Function.GetParam(2);
        var resultString = function.Function.GetParam(3);
        index = builder.BuildIntCast(index, LLVMTypeRef.Int32);
        var int32 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1);
        var indexZero = builder.BuildSub(index, int32);
        var indexOutOfBoundsBlock = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "out of bounds"
        );
        var indexInBoundsBlock = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "in bounds"
        );
        var stringLength = builder.BuildExtractValue(someString, 1);
        var outOfBounds = builder.BuildOr(
            builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, index, int32),
            builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, index, stringLength)
        );

        builder.BuildCondBr(outOfBounds, indexOutOfBoundsBlock, indexInBoundsBlock);
        builder.PositionAtEnd(indexOutOfBoundsBlock);
        Error(
            $"Index %d is out of bounds, for string with length %d in call to substring",
            [index, stringLength]
        );
        builder.PositionAtEnd(indexInBoundsBlock);
        var indexAndLengthOutOfBoundsBlock = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "out of bounds"
        );
        var indexAndLengthInBoundsBlock = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "in bounds"
        );
        // we use index zero because when we substring, we start at index and go for length characters (start...=start+length) so the first character we take is the start, and we will have length - 1 characters left
        var outOfBoundsWithLength = builder.BuildICmp(
            LLVMIntPredicate.LLVMIntSGT,
            builder.BuildAdd(indexZero, builder.BuildIntCast(length, LLVMTypeRef.Int32)),
            stringLength
        );
        builder.BuildCondBr(
            outOfBoundsWithLength,
            indexAndLengthOutOfBoundsBlock,
            indexAndLengthInBoundsBlock
        );
        builder.PositionAtEnd(indexAndLengthOutOfBoundsBlock);
        Error(
            $"Index %d with length %d is out of bounds, for string with length %d in call to substring",
            [index, length, stringLength]
        );
        builder.PositionAtEnd(indexAndLengthInBoundsBlock);

        // make index zero based
        SubString(someString, indexZero, length, resultString);

        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinRight(LLVMShankFunction function)
    {
        var someString = function.Function.GetParam(0);
        var length = function.Function.GetParam(1);
        var length32 = builder.BuildIntCast(length, LLVMTypeRef.Int32);
        var resultString = function.Function.GetParam(2);
        var stringLength = builder.BuildExtractValue(someString, 1);
        var int32 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
        var toBigLength = builder.BuildOr(
            builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, length32, int32),
            builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, length32, stringLength)
        );
        var toBigLengthBlock = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "bad length"
        );
        var goodLengthBlock = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "good length"
        );
        builder.BuildCondBr(toBigLength, toBigLengthBlock, goodLengthBlock);
        builder.PositionAtEnd(toBigLengthBlock);
        Error(
            $"Length %d is to big for string with length %d in call to right",
            [length, stringLength]
        );
        builder.PositionAtEnd(goodLengthBlock);
        // substring starting string.length - length
        var index = builder.BuildIntCast(length, LLVMTypeRef.Int32);
        index = builder.BuildSub(stringLength, index);
        SubString(someString, index, length, resultString);
        builder.BuildRet(int32);
    }

    private void CompileBuiltinLeft(LLVMShankFunction function)
    {
        var someString = function.Function.GetParam(0);
        var length = function.Function.GetParam(1);
        var length32 = builder.BuildIntCast(length, LLVMTypeRef.Int32);
        var resultString = function.Function.GetParam(2);
        var stringLength = builder.BuildExtractValue(someString, 1);
        var int32 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
        var toBigLength = builder.BuildOr(
            builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, length32, int32),
            builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, length32, stringLength)
        );
        var toBigLengthBlock = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function, //
            "bad length"
        );
        var goodLengthBlock = module.Context.AppendBasicBlock(
            context.CurrentFunction.Function,
            "good length"
        );
        builder.BuildCondBr(toBigLength, toBigLengthBlock, goodLengthBlock);
        builder.PositionAtEnd(toBigLengthBlock);
        Error(
            $"Length %d is to big for string with length %d in call to left",
            [length, stringLength]
        );
        builder.PositionAtEnd(goodLengthBlock);

        SubString(someString, int32, length, resultString);
        builder.BuildRet(int32);
    }

    // index passed in are zero based (must be 32-bit integers)
    // bounds must be checked beforehand (so each string manipulation method can give its own specific errors)
    private void SubString(
        LLVMValueRef someString,
        LLVMValueRef index,
        LLVMValueRef length,
        LLVMValueRef resultString
    )
    {
        var someStringContents = builder.BuildExtractValue(someString, 0);
        length = builder.BuildIntCast(length, LLVMTypeRef.Int32);
        var newContent = builder.BuildCall2(
            context.CFuntions.malloc.TypeOf,
            context.CFuntions.malloc.Function,
            [length]
        );
        var subString = builder.BuildInBoundsGEP2(LLVMTypeRef.Int8, someStringContents, [index]);
        builder.BuildCall2(
            context.CFuntions.memcpy.TypeOf,
            context.CFuntions.memcpy.Function,
            [newContent, subString, length]
        );
        var newString = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(_charStar),
                LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
            ],
            false
        );
        newString = builder.BuildInsertValue(newString, newContent, 0);
        newString = builder.BuildInsertValue(
            newString,
            builder.BuildIntCast(length, LLVMTypeRef.Int32),
            1
        );
        builder.BuildStore(newString, resultString);
    }

    private void CompileBuiltinRealToInteger(LLVMShankFunction function)
    {
        var value = function.Function.GetParam(0);
        var result = function.Function.GetParam(1);
        var converted = builder.BuildFPToSI(value, LLVMTypeRef.Int64);
        builder.BuildStore(converted, result);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinAssertIsEqual(LLVMShankFunction function)
    {
        var expected = function.Function.GetParam(0);
        var actual = function.Function.GetParam(1);
        var comparison = CompileBooleanExpression(
            BooleanExpressionNode.BooleanExpressionOpType.eq,
            expected,
            actual
        );
        var sameBlock = function.AppendBasicBlock("same");
        var notEqual = function.AppendBasicBlock("different");
        builder.BuildCondBr(comparison, sameBlock, notEqual);
        builder.PositionAtEnd(sameBlock);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
        builder.PositionAtEnd(notEqual);
        builder.BuildCall2(
            context.CFuntions.printf.TypeOf,
            context.CFuntions.printf.Function,
            [
                builder.BuildGlobalStringPtr(
                    $"expected {GetFormatCode(function.Parameters.First().Type)}, found {GetFormatCode(function.Parameters[1].Type)}\n"
                ),
                ..GetValues((function.Parameters.First().Type, expected)),
                ..GetValues((function.Parameters[1].Type, actual))
            ]
        );
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1));
    }

    private void CompileBuiltinIntegerToReal(LLVMShankFunction function)
    {
        var value = function.Function.GetParam(0);
        var result = function.Function.GetParam(1);
        var converted = builder.BuildSIToFP(value, LLVMTypeRef.Double);
        builder.BuildStore(converted, result);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }
}
