using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.IRGenerator;

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
        else if (typeRef == LLVMTypeRef.Int1)
            return Types.BOOLEAN;
        else if (typeRef == LLVMTypeRef.Double)
            return Types.FLOAT;
        else if (typeRef == context.StringType)
            return Types.STRING;
        else
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
        // TODO: what does string concatination and ranges
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

    public LLVMValueRef CompileInteger(IntNode node)
    {
        return LLVMValueRef.CreateConstInt(
            LLVMTypeRef.Int64, //
            (ulong)node.Value
        );
    }

    public LLVMValueRef CompileReal(FloatNode node)
    {
        return LLVMValueRef.CreateConstReal(
            LLVMTypeRef.Double, //
            node.Value
        );
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

    public LLVMValueRef CompileVariableUsage(VariableUsagePlainNode node, bool load = true)
    {
        Console.WriteLine(node.MonomorphizedName());
        LLVMValue value = context.GetVariable(node.MonomorphizedName());

        if (node.ExtensionType == VariableUsagePlainNode.VrnExtType.ArrayIndex)
        {
            LLVMArray newValue = (LLVMArray)value;
            var elementType = (LLVMTypeRef)
                context.GetLLVMTypeFromShankType(((LLVMArray)value).Inner());
            var start = builder.BuildStructGEP2(newValue.TypeRef, value.ValueRef, 1);
            start = builder.BuildLoad2(LLVMTypeRef.Int32, start);

            var index = builder.BuildIntCast(CompileExpression(node.Extension), LLVMTypeRef.Int32);
            var size = builder.BuildStructGEP2(newValue.TypeRef, value.ValueRef, 2);
            size = builder.BuildLoad2(LLVMTypeRef.Int32, size);
            var indexOutOfBoundsBlock = module.Context.AppendBasicBlock(
                context.CurrentFunction.Function,
                "index out of bounnds"
            );
            var okIndexBlock = module.Context.AppendBasicBlock(
                context.CurrentFunction.Function,
                "ok"
            );
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
            var array = builder.BuildStructGEP2(newValue.TypeRef, value.ValueRef, 0);
            var a = builder.BuildLoad2(LLVMTypeRef.CreatePointer(elementType, 0), array);
            index = builder.BuildSub(index, start);
            a = builder.BuildInBoundsGEP2(elementType, a, [index]);
            // TODO: check for unitizialized access when doing this load (this applies also for member access and simple varaiable lookup)
            return (load ? builder.BuildLoad2(elementType, a) : a);
        }

        if (node.ExtensionType == VariableUsagePlainNode.VrnExtType.RecordMember)
        {
            if (value is LLVMReference r)
            {
                var inner = builder.BuildStructGEP2(r.TypeRef, r.ValueRef, 0);
                inner = builder.BuildLoad2(
                    LLVMTypeRef.CreatePointer(r.TypeOf.Inner.LlvmTypeRef, 0),
                    inner
                );
                value = new LLVMStruct(inner, r.IsMutable, r.TypeOf.Inner);
            }

            LLVMStruct varType = (LLVMStruct)value;
            var varField = (VariableUsagePlainNode)node.GetExtensionSafe();
            var fieldType = (LLVMTypeRef)
                context.GetLLVMTypeFromShankType(varType.GetTypeOf(varField.Name));
            var structField = builder.BuildStructGEP2(
                value.TypeRef,
                value.ValueRef,
                (uint)varType.Access(varField.Name)
            );
            return load ? builder.BuildLoad2(fieldType, structField) : structField;
        }
        // else if (node.ExtensionType == VariableUsagePlainNode.VrnExtType.Enum)
        // {
        //     var varType = (EnumType)context.GetCustomType(value.TypeRef.StructName).Type;
        //     Console.WriteLine(varType.Variants);
        //     Console.WriteLine(node.Extension);
        // }

        if (node.ExtensionType == VariableUsagePlainNode.VrnExtType.None)
        {
            if (value is LLVMEnum p)
            {
                return LLVMValueRef.CreateConstInt(
                    LLVMTypeRef.Int64,
                    (ulong)p.EnumType.GetElementNum(node.Name)
                );
            }

            // if(value.TypeRef == )
            // builder.BuildLoad2(value.TypeRef, )
            return load ? builder.BuildLoad2(value.TypeRef, value.ValueRef) : value.ValueRef;
        }

        throw new NotImplementedException();
    }

    public LLVMValueRef CompileCharacter(CharNode node)
    {
        return (
            LLVMValueRef.CreateConstInt(
                module.Context.Int8Type, //a
                (ulong)(node.Value)
            )
        );
    }

    public LLVMValueRef CompileBoolean(BoolNode node)
    {
        return (
            LLVMValueRef.CreateConstInt(
                module.Context.Int1Type, //a
                (ulong)(
                    node.GetValueAsInt() //a
                )
            )
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
        if (_types(L.TypeOf) == Types.INTEGER)
        {
            return (HandleIntOp(L, node.Op, R));
        }
        else if (_types(L.TypeOf) == Types.FLOAT)
        {
            return (HandleFloatOp(L, node.Op, R));
        }
        else if (_types(L.TypeOf) == Types.STRING)
        {
            return (HandleString(L, R));
        }

        throw new NotImplementedException();
    }

    public LLVMValueRef CompileBooleanExpression(BooleanExpressionNode node)
    {
        LLVMValueRef R = CompileExpression(node.Right);
        LLVMValueRef L = CompileExpression(node.Left);
        if (_types(L.TypeOf) == Types.INTEGER)
        {
            return HandleIntBoolOp(
                L,
                node.Op switch
                {
                    BooleanExpressionNode.BooleanExpressionOpType.eq => LLVMIntPredicate.LLVMIntEQ,
                    BooleanExpressionNode.BooleanExpressionOpType.ne => LLVMIntPredicate.LLVMIntNE,
                    BooleanExpressionNode.BooleanExpressionOpType.gt => LLVMIntPredicate.LLVMIntSGT,
                    BooleanExpressionNode.BooleanExpressionOpType.lt => LLVMIntPredicate.LLVMIntSLT,
                    BooleanExpressionNode.BooleanExpressionOpType.le => LLVMIntPredicate.LLVMIntSLE,
                    BooleanExpressionNode.BooleanExpressionOpType.ge => LLVMIntPredicate.LLVMIntSGE,
                },
                R
            );
        }
        else if (_types(L.TypeOf) == Types.FLOAT)
        {
            return HandleFloatBoolOp(
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
            );
        }

        throw new NotImplementedException();
    }

    public void CompileRecord(RecordNode node)
    {
        // this cannot be done from the prototype becuase we cannot attach llvm types to Type without putting dependency of llvm for the Types file
        // also we do not know the order by which types are added to the llvm module
        var record = context.GetCustomType(node.Type.MonomorphizedIndex);
        var args = node.Type.Fields.Select(
            s =>
                // for records (and eventually references) we do not hold the actual type of the record, but rather a pointer to it, because llvm does not like direct recursive types
                (
                    s.Key,
                    s.Value is RecordType
                        ? LLVMTypeRef.CreatePointer(
                            (LLVMTypeRef)context.GetLLVMTypeFromShankType(s.Value)!,
                            0
                        )
                        : (LLVMTypeRef)context.GetLLVMTypeFromShankType(s.Value)!
                )
        )
            .ToArray();
        record.LlvmTypeRef.StructSetBody(args.Select(s => s.Item2).ToArray(), false);
    }

    public void CompileFunctionCall(FunctionCallNode node)
    {
        {
            var function =
                context.GetFunction(node.MonomphorizedFunctionLocater)
                ?? throw new Exception($"function {node.Name} not found");
            // if any arguement is not mutable, but is required to be mutable
            if (
                function
                    .ArguementMutability.Zip( //mutable
                        node.Arguments //Parameters //multable
                        .Select(p => p is VariableUsagePlainNode { IsVariableFunctionCall: true }) //.IsVariable)
                    )
                    .Any(a => a is { First: true, Second: false })
            )
            {
                throw new Exception($"call to {node.Name} has a mismatch of mutability");
            }

            var parameters = node.Arguments.Zip(function.ArguementMutability)
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
        builder.BuildRet(LLVMValueRef.CreateConstInt(module.Context.Int32Type, (ulong)0));
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

    public void CompileAssignment(AssignmentNode node)
    {
        // Console.WriteLine("assignment nsmaE: " + node.Target.MonomorphizedName());
        var llvmValue = context.GetVariable(node.Target.MonomorphizedName());
        var expression = CompileExpression(node.Expression);
        var target = CompileExpression(node.Target, false);
        if (!llvmValue.IsMutable)
        {
            throw new Exception($"tried to mutate non mutable variable {node.Target.Name}");
        }

        builder.BuildStore(expression, target);
    }

    // public  void Visit(EnumNode node)
    // {
    //     throw new NotImplementedException();
    // }

    /*public  void Visit(ModuleNode node)
    {
        context.SetCurrentModule(node.Name);
        // then we add to our scope all our imports
        foreach (var (moduleName, imports) in node.ImportTargetNames)
        {
            var shankModule = context.Modules[moduleName];
            foreach (var import in imports)
            {
                // TODO: type imports
                if (shankModule.Functions.TryGetValue(import, out var function))
                {
                    context.AddFunction(import, function);
                }
                else if (shankModule.CustomTypes.TryGetValue(import, out var type))
                {
                    context.AddCustomType(import, type);
                }
            }
        }

        node //modnode
        .GetFunctionsAsList() //list
            .ForEach(f => f.Accept(this));
    }*/

    public void CompileIf(IfNode node)
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

    public void CompileRepeat(RepeatNode node)
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

    public void CompileVariableDeclaration(VariableDeclarationNode node)
    {
        var name = node.GetNameSafe();
        // TODO: only alloca when !isConstant

        var llvmTypeFromShankType =
            context.GetLLVMTypeFromShankType(node.Type) ?? throw new Exception("null type");
        LLVMValueRef v = builder.BuildAlloca(
            // isVar is false, because we are already creating it using alloca which makes it var
            llvmTypeFromShankType,
            name
        );
        // TODO: preallocate records to (might need to be recursive)
        if (node.Type is ArrayType a)
        {
            var arraySize = LLVMValueRef.CreateConstInt(
                LLVMTypeRef.Int32,
                (ulong)(a.Range.To - a.Range.From)
            );
            var arrayStart = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)a.Range.From);
            var arrayAllocation = builder.BuildArrayAlloca(
                (LLVMTypeRef)context.GetLLVMTypeFromShankType(a.Inner),
                arraySize
            );
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
        var variable = context.NewVariable(node.Type);
        context.AddVariable(node.MonomorphizedName(), variable(v, !node.IsConstant));
    }

    public void Compile(MonomorphizedProgramNode node)
    {
        var protoTypeCompiler = new PrototypeCompiler(context, builder, module);
        protoTypeCompiler.CompilePrototypes(node);

        node.Records.Values.ToList().ForEach(CompileRecord);
        node.Functions.Values.ToList().ForEach(CompileFunction);
        node.BuiltinFunctions.Values.ToList().ForEach(CompileBuiltinFunction);
    }

    public void CompileFor(ForNode node)
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
        // TODO: signed or unsigned comparison
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

    public void CompileBuiltinFunction(BuiltInFunctionNode node)
    {
        var function = context.BuiltinFunctions[(TypedBuiltinIndex)node.MonomorphizedName];
        var block = function.AppendBasicBlock("entry");
        builder.PositionAtEnd(block);
        switch (node.Name)
        {
            case "write":
                CompileBuiltinWrite(node, function);
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
                CompileBuiltinAllocateMemory(node, function);
                break;
            case "freeMemory":
                CompileBuiltinFreeMemory(node, function);
                break;
            case "isSet":
                CompileBuiltinIsSet(node, function);
                break;
            case "high":
                CompileBuiltinHigh(function);
                break;
            case "low":
                CompileBuiltinLow(function);
                break;
            case "size":
                CompileBuiltinSize(node, function);
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


    private void CompileBuiltinSize(BuiltInFunctionNode node, LLVMShankFunction function)
    {
        // we don't need the actual reference as llvm type of the reference can be determined based on the signature of the function (effectively this is computed at compile time)
        // var memory = function.Function.FirstParam;

        var size = function.Function.GetParam(1);

        var type = (ReferenceType)node.ParameterVariables.First().Type;
        var innerTypeOfReference = type.Inner;
        var llvmTypeOfReference = (LLVMTypeRef)
            context.GetLLVMTypeFromShankType(innerTypeOfReference);
        var referenceSize = llvmTypeOfReference.SizeOf;
        builder.BuildStore(referenceSize, size);
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
        builder.BuildStore(arrayEnd, end);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinLow(LLVMShankFunction function)
    {
        // we have to get the runtime array start as opposed to just looking at the signature of the function because we do not monomorphize over the type limits
        var array = function.Function.FirstParam;
        var start = function.Function.GetParam(1);
        var arrayStart = builder.BuildExtractValue(array, 1);
        builder.BuildStore(arrayStart, start);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinIsSet(BuiltInFunctionNode node, LLVMShankFunction function)
    {
        var memory = function.Function.FirstParam;
        var isSet = function.Function.GetParam(1);

        var set = builder.BuildExtractValue(memory, 2);
        builder.BuildStore(set, isSet);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinAllocateMemory(BuiltInFunctionNode node, LLVMShankFunction function)
    {
        // TODO: do refernces need to keep their size for `size` builtin?
        var param = function.Function.FirstParam;
        ReferenceType type = (ReferenceType)node.ParameterVariables.First().Type;
        var innerType = type.Inner;
        var llvmTypeFromShankType = (LLVMTypeRef)context.GetLLVMTypeFromShankType(innerType);
        var size = llvmTypeFromShankType.SizeOf;
        size = builder.BuildIntCast(size, LLVMTypeRef.Int32);
        var memory = builder.BuildMalloc(llvmTypeFromShankType);
        var newReference = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(llvmTypeFromShankType, 0)),
                size,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1)
            ],
            false
        );
        newReference = builder.BuildInsertValue(newReference, memory, 0);
        builder.BuildStore(newReference, param);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinFreeMemory(BuiltInFunctionNode node, LLVMShankFunction function)
    {
        var param = function.Function.FirstParam;
        ReferenceType type = (ReferenceType)node.ParameterVariables.First().Type;
        var innerType = type.Inner;
        var typeFromShankType = (LLVMTypeRef)context.GetLLVMTypeFromShankType(type);
        var memory = builder.BuildLoad2(
            LLVMTypeRef.CreatePointer(typeFromShankType, 0),
            builder.BuildStructGEP2(typeFromShankType, param, 0)
        );
        builder.BuildFree(memory);

        var llvmTypeFromShankType = (LLVMTypeRef)context.GetLLVMTypeFromShankType(innerType);
        var newReference = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(llvmTypeFromShankType, 0)),
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)
            ],
            false
        );
        builder.BuildStore(newReference, param);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinWrite(BuiltInFunctionNode node, LLVMShankFunction function)
    {
        var formatList = node.ParameterVariables.Select(param => param.Type)
            .Select(
                type =>
                    type switch
                    {
                        BooleanType booleanType => "%s",
                        CharacterType characterType => "%c",
                        IntegerType integerType => "%d",
                        RealType realType => "%.2f",
                        StringType stringType => "%.*s",
                        _ => throw new NotImplementedException(nameof(type))
                    }
            );
        var format = $"{string.Join(" ", formatList)}\n";
        var parameters = function
            .Function.Params.SelectMany<LLVMValueRef, LLVMValueRef>(
                p =>
                    _types(p.TypeOf) switch
                    {
                        // TODO: do not dispatch based (only) on llvm type b/c especially for records, it will be hard to obtain the record type via llvm rather use the parameter list of the function from the ast
                        Types.FLOAT
                            => [p],
                        Types.INTEGER => [p],
                        Types.BOOLEAN
                            =>
                            [
                                builder.BuildSelect(
                                    p,
                                    builder.BuildGlobalStringPtr("true"),
                                    builder.BuildGlobalStringPtr("false")
                                )
                            ],
                        Types.STRING
                            => [builder.BuildExtractValue(p, 1), builder.BuildExtractValue(p, 0)],
                    }
            )
            .Prepend(builder.BuildGlobalStringPtr(format, "printf-format"));
        builder.BuildCall2(
            context.CFuntions.printf.TypeOf,
            context.CFuntions.printf.Function,
            parameters.ToArray()
        );
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinSubString(LLVMShankFunction function)
    {
        // SubString someString, index, length, var resultString

        // resultString = length characters from someString, starting at index
        var someString = function.Function.GetParam(0);
        var index = function.Function.GetParam(1);
        var length = function.Function.GetParam(2);
        var resultString = function.Function.GetParam(3);

        SubString(someString, index, length, resultString);

        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinRight(LLVMShankFunction function)
    {
        var someString = function.Function.GetParam(0);
        var length = function.Function.GetParam(1);
        var resultString = function.Function.GetParam(2);
        var stringLength = builder.BuildExtractValue(someString, 1);
        // substring starting string.lenth - length
        var index = builder.BuildSub(stringLength, builder.BuildIntCast(length, LLVMTypeRef.Int32));
        SubString(someString, index, length, resultString);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }

    private void CompileBuiltinLeft(LLVMShankFunction function)
    {
        var someString = function.Function.GetParam(0);
        var length = function.Function.GetParam(1);
        var resultString = function.Function.GetParam(2);
        SubString(someString, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0), length, resultString);
        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
    }
    // index passed in are zero based
    private void SubString(LLVMValueRef someString, LLVMValueRef index, LLVMValueRef length, LLVMValueRef resultString)
    {
        // TODO: bounds checking
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
