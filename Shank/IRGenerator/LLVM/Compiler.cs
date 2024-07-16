using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.IRGenerator;

// TODO: check all mallocs (and any other thing returned from calling a c function)

public enum Types
{
    STRUCT,
    FLOAT,
    INTEGER,
    BOOLEAN,
    STRING,
}

public class Compiler(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
{
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
            // VariableUsageIndexNode variableUsageIndexNode => Visit(),
            // VariableUsageMemberNode variableUsageMemberNode => Visit(),
            VariableUsagePlainNode variableUsagePlainNode
                => CompileVariableUsage(variableUsagePlainNode, load),
            // VariableUsageNodeTemp variableUsageNodeTemp => Visit(),
            _ => throw new ArgumentOutOfRangeException(nameof(expression))
        };
    }

    private Types _types(LLVMTypeRef typeRef)
    {
        if (typeRef == LLVMTypeRef.Int64 || typeRef == LLVMTypeRef.Int8)
            return Types.INTEGER;
        if (typeRef == LLVMTypeRef.Int1)
            return Types.BOOLEAN;
        if (typeRef == LLVMTypeRef.Double)
            return Types.FLOAT;
        if (typeRef == LLVMType.StringType)
            return Types.STRING;
        throw new Exception("undefined type");
    }

    private LLVMValueRef HandleIntOp(
        LLVMValueRef L, //L
        MathOpNode.MathOpType Op, // OP
        LLVMValueRef R //R
    )
    {
        return Op switch
        {
            MathOpNode.MathOpType.Plus => builder.BuildAdd(L, R, "addtmp"),
            MathOpNode.MathOpType.Minus => builder.BuildSub(L, R, "subtmp"),
            MathOpNode.MathOpType.Times => builder.BuildMul(L, R, "multmp"),
            MathOpNode.MathOpType.Divide => builder.BuildSDiv(L, R, "divtmp"),
            MathOpNode.MathOpType.Modulo => builder.BuildURem(L, R, "modtmp"),
            _ => throw new Exception("unsupported operation")
        };
    }

    private LLVMValueRef HandleFloatOp(
        LLVMValueRef L, //L
        MathOpNode.MathOpType Op, // OP
        LLVMValueRef R //R
    )
    {
        return Op switch
        {
            MathOpNode.MathOpType.Plus => builder.BuildFAdd(L, R, "addtmp"),
            MathOpNode.MathOpType.Minus => builder.BuildFSub(L, R, "subtmp"),
            MathOpNode.MathOpType.Times => builder.BuildFMul(L, R, "multmp"),
            MathOpNode.MathOpType.Divide => builder.BuildFDiv(L, R, "divtmp"),
            _ => throw new Exception("unsupported operation")
        };
    }

    private LLVMValueRef HandleString(LLVMValueRef L, LLVMValueRef R)
    {
        var lSize = builder.BuildExtractValue(L, 1);
        var rSize = builder.BuildExtractValue(R, 1);
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
            [newContent, builder.BuildExtractValue(L, 0), lSize]
        );
        // fill the second part of the string
        // first "increment" the pointer of the string to be after the contents of the first part
        var secondPart = builder.BuildInBoundsGEP2(LLVMTypeRef.Int8, newContent, [lSize]);
        builder.BuildCall2(
            context.CFuntions.memcpy.TypeOf,
            context.CFuntions.memcpy.Function,
            [secondPart, builder.BuildExtractValue(R, 0), rSize]
        );
        var String = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)),
                LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
            ],
            false
        );
        String = builder.BuildInsertValue(String, newContent, 0);
        String = builder.BuildInsertValue(String, newSize, 1);
        return String;
    }

    private LLVMValueRef HandleIntBoolOp(
        LLVMValueRef L, //L
        LLVMIntPredicate Op, // OP
        LLVMValueRef R //R
    )
    {
        return builder.BuildICmp(Op, L, R);
    }

    private LLVMValueRef HandleFloatBoolOp(
        LLVMValueRef L, //L
        LLVMRealPredicate Op, // OP
        LLVMValueRef R //R
    )
    {
        return builder.BuildFCmp(Op, L, R);
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

    public LLVMValueRef CompileEnum(VariableUsagePlainNode node)
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

        var varaiable = node.ExtensionType switch
        {
            VariableUsagePlainNode.VrnExtType.ArrayIndex => CompileArrayUsage(node, value),

            VariableUsagePlainNode.VrnExtType.RecordMember => CompileRecordUsage(node, value),
            // VariableUsagePlainNode.VrnExtType.Enum => CompileEnum(node, value),

            VariableUsagePlainNode.VrnExtType.None
                => value,
        };
        return load ? CopyVariable(varaiable) : varaiable.ValueRef;
    }

    private LLVMValueRef CopyVariable(LLVMValue varaiable)
    {
        // not happening (should happen during semantic analysis) check for unitizialized access when doing this load
        // TODO: copy everything recursivly
        var value = builder.BuildLoad2(varaiable.TypeRef.TypeRef, varaiable.ValueRef);
        return CopyVariable(varaiable.TypeRef, value);
    }

    private LLVMValueRef CopyVariable(LLVMType type, LLVMValueRef value) =>
        type switch
        {
            // TODO: arrays might need to be copied just need a better way to do it
            LLVMArrayType => value,
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
            var inner = builder.BuildStructGEP2(r.TypeRef.TypeRef, r.ValueRef, 0);
            inner = builder.BuildLoad2(LLVMTypeRef.CreatePointer(r.TypeOf.Inner.TypeRef, 0), inner);
            value = new LLVMStruct(inner, r.IsMutable, r.TypeOf.Inner);
        }

        LLVMStruct varType = (LLVMStruct)value;
        var varField = (VariableUsagePlainNode)node.GetExtensionSafe();
        var dataType = varType.GetTypeOf(varField.Name);
        // var fieldType = (LLVMTypeRef)context.GetLLVMTypeFromShankType(dataType);
        var structField = builder.BuildStructGEP2(
            value.TypeRef.TypeRef,
            value.ValueRef,
            (uint)varType.Access(varField.Name)
        );

        return dataType.IntoValue(structField, value.IsMutable);
    }

    private LLVMValue CompileArrayUsage(VariableUsagePlainNode node, LLVMValue value)
    {
        var newValue = (LLVMArray)value;
        var arrayInnerType = ((LLVMArray)value).Inner();
        var elementType = arrayInnerType.TypeRef;
        var start = builder.BuildStructGEP2(newValue.TypeRef.TypeRef, value.ValueRef, 1);
        start = builder.BuildLoad2(LLVMTypeRef.Int32, start);

        var index = builder.BuildIntCast(CompileExpression(node.Extension), LLVMTypeRef.Int32);
        var size = builder.BuildStructGEP2(newValue.TypeRef.TypeRef, value.ValueRef, 2);
        size = builder.BuildLoad2(LLVMTypeRef.Int32, size);
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
            $"Index %d is out of bounds, for array {node.Name} of type {newValue.Inner()}",
            [index],
            node
        );
        builder.PositionAtEnd(okIndexBlock);
        var array = builder.BuildStructGEP2(newValue.TypeRef.TypeRef, value.ValueRef, 0);
        var a = builder.BuildLoad2(LLVMTypeRef.CreatePointer(elementType, 0), array);
        index = builder.BuildSub(index, start);
        a = builder.BuildInBoundsGEP2(elementType, a, [index]);
        return arrayInnerType.IntoValue(a, value.IsMutable);
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

    public LLVMValueRef CompileString(StringNode node)
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
                LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)),
                LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
            ],
            false
        );
        String = builder.BuildInsertValue(String, stringPointer, 0);
        String = builder.BuildInsertValue(String, stringLength, 1);
        return String;
    }

    public LLVMValueRef CompileMathExpression(MathOpNode node)
    {
        LLVMValueRef R = CompileExpression(node.Right);
        LLVMValueRef L = CompileExpression(node.Left);
        // TODO: don't use _types, once we add types to ExpressionNode
        var types = _types(L.TypeOf);
        return types switch
        {
            Types.INTEGER => HandleIntOp(L, node.Op, R),
            Types.FLOAT => HandleFloatOp(L, node.Op, R),
            Types.STRING => HandleString(L, R),
            _ => throw new NotImplementedException()
        };
    }

    public LLVMValueRef CompileBooleanExpression(BooleanExpressionNode node)
    {
        LLVMValueRef R = CompileExpression(node.Right);
        LLVMValueRef L = CompileExpression(node.Left);
        // TODO: don't use _types, once we add types to ExpressionNode
        var types = _types(L.TypeOf);
        return types switch
        {
            Types.INTEGER
                => HandleIntBoolOp(
                    L,
                    node.Op switch
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
                    R
                ),
            Types.FLOAT
                => HandleFloatBoolOp(
                    L,
                    node.Op switch
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
                    R
                ),
            _ => throw new NotImplementedException()
        };
    }

    public void CompileRecord(RecordNode node)
    {
        // this cannot be done from the prototype becuase we cannot attach llvm types to Type without putting dependency of llvm for the Types file
        // also we do not know the order by which types are added to the llvm module
        var record = context.Records[node.Type.MonomorphizedIndex];
        var members = node.Type.Fields.Select(
            s =>
                (
                    s.Key,
                    // This is now longer case: we are assuming that (mutaully) recursive records are behind a refersTo
                    // for records (and eventually references) we do not hold the actual type of the record, but rather a pointer to it, because llvm does not like direct recursive types
                    // s.Value is RecordType
                    //     ? LLVMTypeRef.CreatePointer(
                    //         (LLVMTypeRef)context.GetLLVMTypeFromShankType(s.Value)!,
                    //         0
                    //     )
                    //     :
                    context.GetLLVMTypeFromShankType(s.Value)
                )
        )
            .ToDictionary();
        record.Members = members;
        record.TypeRef.StructSetBody(members.Select(s => s.Value.TypeRef).ToArray(), false);
    }

    public void CompileFunctionCall(FunctionCallNode node)
    {
        {
            var function = context.GetFunction(node.MonomphorizedFunctionLocater);

            var parameters = node.Arguments.Zip(function.Parameters.Select(p => p.Mutable))
                .Select(
                    (arguementAndMutability) =>
                        CompileExpression(
                            arguementAndMutability.First,
                            !arguementAndMutability.Second
                        )
                );
            // function.
            builder.BuildCall2(function.TypeOf, function.Function, parameters.ToArray());
        }
    }

    public void CompileFunction(FunctionNode node)
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
            if (param.IsConstant)
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
                var name = param.GetNameSafe();
                var parameter = context.NewVariable(param.Type)(
                    llvmParam, //a
                    !param.IsConstant
                );
                context.AddVariable(param.MonomorphizedName(), parameter);
            }
        }

        function.Linkage = LLVMLinkage.LLVMExternalLinkage;

        node.LocalVariables.ForEach(CompileVariableDeclaration);
        node.Statements.ForEach(CompileStatement);
        builder.BuildRet(LLVMValueRef.CreateConstInt(module.Context.Int32Type, 0));
        context.ResetLocal();
    }

    public void CompileWhile(WhileNode node)
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
        var llvmValue = context.GetVariable(node.Target.MonomorphizedName());
        var expression = CompileExpression(node.Expression);
        var target = CompileExpression(node.Target, false);
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
        // else statement, when compiling the if part
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
        // only alloca when !isConstant (is somewhat problametic with structs)

        var llvmTypeFromShankType = context.GetLLVMTypeFromShankType(node.Type);
        LLVMValueRef v = builder.BuildAlloca(
            // isVar is false, because we are already creating it using alloca which makes it var
            llvmTypeFromShankType.TypeRef,
            name
        );
        if (node.IsDefaultValue)
        {
            var init = CompileExpression(node.InitialValue);
            builder.BuildStore(init, v);
        }

        // TODO: preallocate arrays in records to (might need to be recursive)
        if (node.Type is ArrayType a && llvmTypeFromShankType is LLVMArrayType shankType)
        {
            var arraySize = LLVMValueRef.CreateConstInt(
                LLVMTypeRef.Int32,
                (ulong)(a.Range.To - a.Range.From)
            );
            var arrayStart = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)a.Range.From);
            var arrayAllocation = builder.BuildArrayAlloca(shankType.Inner.TypeRef, arraySize);
            var array = builder.BuildInsertValue(
                LLVMValueRef.CreateConstStruct(
                    [LLVMValueRef.CreateConstNull(arrayAllocation.TypeOf), arrayStart, arraySize],
                    false
                ),
                arrayAllocation,
                0
            );
            builder.BuildStore(array, v);
        }

        // TODO: NewVariableCalls GetLLVMTypeForShankType even though in order to allocate we need to already know the type so we call the aforementioned function,
        // maybe the aforementioned function should take a function that takes a type and gives back an LLVMValueRef (so it would work here, but also for parameters)
        var variable = context.NewVariable(node.Type);
        context.AddVariable(node.MonomorphizedName(), variable(v, !node.IsConstant));
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
        // hold the array in seperate global, so that we can gep it as a pointer (you can't gep a raw array)
        var stringVariable = module.AddGlobal(stringContent.TypeOf, s.Value + "Content");
        stringVariable.Initializer = stringContent;
        // now gep the array which is the global variable, this is done to convert the array to an i8*
        stringContent = LLVMValueRef.CreateConstInBoundsGEP2(
            stringContent.TypeOf,
            stringVariable,
            [
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0)
            ]
        );

        var size = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)s.Value.Length);
        return LLVMValueRef.CreateConstStruct([stringContent, size], false);
    }

    private void CompileFor(ForNode node)
    {
        var forStart = context.CurrentFunction.AppendBasicBlock("for.start");
        var afterFor = context.CurrentFunction.AppendBasicBlock("for.after");
        var forBody = context.CurrentFunction.AppendBasicBlock("for.body");
        var forIncremnet = context.CurrentFunction.AppendBasicBlock("for.inc");
        var mutableCurrentIterable = CompileExpression(node.Variable, false);
        var fromValue = CompileExpression(node.From);
        builder.BuildStore(fromValue, mutableCurrentIterable);

        // TODO: assign loop variable initial from value
        builder.BuildBr(forStart);
        builder.PositionAtEnd(forStart);
        // we have to compile the to and from in the loop so that the get run each time, we go through the loop
        // in case we modify them in the loop


        var toValue = CompileExpression(node.To);

        var currentIterable = CompileExpression(node.Variable);
        // right now we assume, from, to, and the variable are all integers
        // in the future we should check and give some error at runtime/compile time if not
        var condition = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, currentIterable, toValue);
        builder.BuildCondBr(condition, forBody, afterFor);
        builder.PositionAtEnd(forBody);
        node.Children.ForEach(CompileStatement);
        builder.BuildBr(forIncremnet);
        builder.PositionAtEnd(forIncremnet);

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
        switch (node.Name)
        {
            case "write":
                CompileBuiltinWrite(function);
                break;
            case "read":
                CompileBuiltinRead(function);
                break;
            case "substring":
                CompileBuiltinSubString(function);
                break;
            case "realToInteger":
                CompileBuiltinRealToInteger(function);
                break;
            case "integerToReal":
                CompileBuiltinIntegerToReal(function);
                break;
            case "allocateMemory":
                CompileBuiltinAllocateMemory(function);
                break;
            case "freeMemory":
                CompileBuiltinFreeMemory(function);
                break;
            case "isSet":
                CompileBuiltinIsSet(function);
                break;
            case "high":
                CompileBuiltinHigh(function);
                break;
            case "low":
                CompileBuiltinLow(function);
                break;
            case "size":
                CompileBuiltinSize(function);
                break;
            case "left":
                CompileBuiltinLeft(function);
                break;
            case "right":
                CompileBuiltinRight(function);
                break;
            default:
                throw new CompilerException("Undefined builtin", 0);
        }
    }

    private void CompileBuiltinRead(LLVMShankFunction function)
    {
        // TODO: should read keep the \n
        var output = function.Function.FirstParam;
        var read_bb = module.Context.AppendBasicBlock(function.Function, "read char");
        var reallocate_bb = module.Context.AppendBasicBlock(function.Function, "reallocate");
        var check_done_bb = module.Context.AppendBasicBlock(function.Function, "check done");
        var done_bb = module.Context.AppendBasicBlock(function.Function, "done");
        // init part
        var size = builder.BuildAlloca(LLVMTypeRef.Int32);
        builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 128), size);
        var content = builder.BuildAlloca(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));
        builder.BuildStore(
            builder.BuildCall2(
                context.CFuntions.malloc.TypeOf,
                context.CFuntions.malloc.Function,
                [builder.BuildLoad2(LLVMTypeRef.Int32, size)]
            ),
            content
        );
        var index = builder.BuildAlloca(LLVMTypeRef.Int32);
        builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0), index);
        // read part
        builder.BuildBr(read_bb);
        builder.PositionAtEnd(read_bb);
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
        builder.BuildCondBr(reallocate, reallocate_bb, check_done_bb);
        // reallocation
        builder.PositionAtEnd(reallocate_bb);
        var newSize = builder.BuildLShr(
            builder.BuildLoad2(LLVMTypeRef.Int32, size),
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1)
        );
        var newContent = builder.BuildCall2(
            context.CFuntions.realloc.TypeOf,
            context.CFuntions.realloc.Function,
            [builder.BuildLoad2(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), content), newSize]
        );
        builder.BuildStore(newContent, content);
        builder.BuildStore(newSize, size);
        builder.BuildBr(check_done_bb);
        // updating string and checking if we are done
        builder.PositionAtEnd(check_done_bb);
        var current = builder.BuildInBoundsGEP2(
            LLVMTypeRef.Int8,
            builder.BuildLoad2(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), content),
            [loadedIndex]
        );
        builder.BuildStore(read, current);
        builder.BuildStore(
            builder.BuildAdd(loadedIndex, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1)),
            index
        );
        var isNewline = builder.BuildICmp(
            LLVMIntPredicate.LLVMIntEQ,
            read,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, '\n')
        );
        builder.BuildCondBr(isNewline, done_bb, read_bb);
        builder.PositionAtEnd(done_bb);
        var newString = LLVMType.StringType.Undef;
        newString = builder.BuildInsertValue(
            newString,
            builder.BuildLoad2(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), content),
            0
        );
        newString = builder.BuildInsertValue(
            newString,
            builder.BuildLoad2(LLVMTypeRef.Int32, size),
            1
        );
        builder.BuildStore(newString, output);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
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
        // TODO: do refernces need to keep their size for `size` builtin?
        var param = function.Function.FirstParam;

        var type = (LLVMReferenceType)function.Parameters.First().Type;
        var llvmTypeOfReference = type.Inner.TypeRef;

        var size = llvmTypeOfReference.SizeOf;
        size = builder.BuildIntCast(size, LLVMTypeRef.Int32);
        var memory = builder.BuildMalloc(llvmTypeOfReference);
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

        var newReference = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(
                    LLVMTypeRef.CreatePointer(llvmTypeOfReferenceInner, 0)
                ),
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)
            ],
            false
        );
        builder.BuildStore(newReference, param);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private LLVMValueRef HandleEnum(LLVMValueRef valueRef, List<string> list, int Index = 0)
    {
        if (Index >= list.Count)
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
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)Index),
                valueRef
            ),
            builder.BuildGlobalStringPtr(list[Index]),
            HandleEnum(valueRef, list, Index + 1)
        );
    }

    private void CompileBuiltinWrite(LLVMShankFunction function)
    {
        var formatList = function.Parameters.Select(param => param.Type).Select(GetFormatCode);

        var format = $"{string.Join(" ", formatList)}\n";
        var paramList = function
            .Parameters.Select(param => param.Type)
            .Zip(function.Function.Params)
            .SelectMany(GetValues)
            .Prepend(builder.BuildGlobalStringPtr(format, "printf-format"));
        builder.BuildCall2(
            context.CFuntions.printf.TypeOf,
            context.CFuntions.printf.Function,
            paramList.ToArray()
        );
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));

        static string GetFormatCode(LLVMType type) =>
            type switch
            {
                LLVMBooleanType => "%s",
                LLVMCharacterType => "%c",
                LLVMIntegerType => "%d",
                LLVMEnumType => "%s",
                LLVMRealType => "%.2f",
                LLVMStringType => "%.*s",
                LLVMStructType record
                    => $"{record.Name}: [ {string.Join(", ", record.Members.Select(member => $"{member.Key}: {GetFormatCode(member.Value)}"))} ]",
                // TODO: print only one level
                LLVMReferenceType reference => $"refersTo {reference.Inner.Name}",

                _ => throw new NotImplementedException(type.ToString())
            };

        IEnumerable<LLVMValueRef> GetValues((LLVMType First, LLVMValueRef Second) n)
        {
            {
                return n.First switch
                {
                    LLVMEnumType e => [HandleEnum(n.Second, e.Variants)],
                    LLVMIntegerType or LLVMRealType or LLVMCharacterType => [n.Second],
                    LLVMBooleanType
                        =>
                        [
                            builder.BuildSelect(
                                n.Second,
                                builder.BuildGlobalStringPtr("true"),
                                builder.BuildGlobalStringPtr("false")
                            )
                        ],
                    LLVMStringType
                        =>
                        [
                            builder.BuildExtractValue(n.Second, 1),
                            builder.BuildExtractValue(n.Second, 0)
                        ],
                    LLVMStructType record
                        => record.Members.Values.SelectMany(
                            (member, index) =>
                                GetValues(
                                    (member, builder.BuildExtractValue(n.Second, (uint)index))
                                )
                        ),
                    LLVMReferenceType => [],
                    _ => throw new NotImplementedException(n.First.ToString())
                };
            }
            ;
        }
    }

    private void CompileBuiltinSubString(LLVMShankFunction function)
    {
        // SubString someString, index, length, var resultString

        // resultString = length characters from someString, starting at index
        var someString = function.Function.GetParam(0);
        var index = function.Function.GetParam(1);
        var length = function.Function.GetParam(2);
        var resultString = function.Function.GetParam(3);
        index = builder.BuildIntCast(index, LLVMTypeRef.Int32);
        var indexZero = builder.BuildSub(index, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1));
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
            builder.BuildICmp(
                LLVMIntPredicate.LLVMIntSLT,
                index,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1)
            ),
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
        // we use index zero because when we substring we start at index and go for length characters (start..=start+length) so the first character we take is the start and we will have length - 1 characters left
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
        var toBigLength = builder.BuildOr(
            builder.BuildICmp(
                LLVMIntPredicate.LLVMIntSLT,
                length32,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0)
            ),
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
        // substring starting string.lenth - length
        var index = builder.BuildIntCast(length, LLVMTypeRef.Int32);
        index = builder.BuildSub(stringLength, index);
        SubString(someString, index, length, resultString);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinLeft(LLVMShankFunction function)
    {
        var someString = function.Function.GetParam(0);
        var length = function.Function.GetParam(1);
        var length32 = builder.BuildIntCast(length, LLVMTypeRef.Int32);
        var resultString = function.Function.GetParam(2);
        var stringLength = builder.BuildExtractValue(someString, 1);
        var toBigLength = builder.BuildOr(
            builder.BuildICmp(
                LLVMIntPredicate.LLVMIntSLT,
                length32,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0)
            ),
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
            $"Length %d is to big for string with length %d in call to left",
            [length, stringLength]
        );
        builder.PositionAtEnd(goodLengthBlock);

        SubString(
            someString,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
            length,
            resultString
        );
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    // index passed in are zero based (must be 32 bit integers)
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
        var String = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)),
                LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
            ],
            false
        );
        String = builder.BuildInsertValue(String, newContent, 0);
        String = builder.BuildInsertValue(
            String,
            builder.BuildIntCast(length, LLVMTypeRef.Int32),
            1
        );
        builder.BuildStore(String, resultString);
    }

    private void CompileBuiltinRealToInteger(LLVMShankFunction function)
    {
        var value = function.Function.GetParam(0);
        var result = function.Function.GetParam(1);
        var converted = builder.BuildFPToSI(value, LLVMTypeRef.Int64);
        builder.BuildStore(converted, result);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
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
