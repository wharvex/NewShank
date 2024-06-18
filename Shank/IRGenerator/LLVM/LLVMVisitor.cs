using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.IRGenerator;

namespace Shank.ExprVisitors;

public enum Types
{
    STRUCT,
    FLOAT,
    INTEGER,
    STRING,
}

public class LLVMVisitor(Context context, LLVMBuilderRef builder, LLVMModuleRef module) : Visitor
{
    private Stack<LLVMValueRef> expr = new();

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

    public override void Visit(IntNode node)
    {
        expr.Push(
            LLVMValueRef.CreateConstInt(
                LLVMTypeRef.Int64, //
                (ulong)node.Value
            )
        ); //
    }

    public override void Visit(FloatNode node)
    {
        expr.Push(
            LLVMValueRef.CreateConstReal(
                LLVMTypeRef.Double, //
                (ulong)node.Value
            )
        ); //a
    }

    public override void Visit(VariableUsagePlainNode node)
    {
        LLVMValue value = context.GetVariable(node.Name);
        if (
            node.ExtensionType == VariableUsagePlainNode.VrnExtType.ArrayIndex
            || node.ExtensionType == VariableUsagePlainNode.VrnExtType.RecordMember
        )
        {
            node.Extension?.Accept(this);
            var a = builder.BuildGEP2(value.TypeRef, value.ValueRef, new[] { expr.Pop() });
            expr.Push(builder.BuildLoad2(value.TypeRef, a));
        }
        // else if (node.ExtensionType == VariableUsagePlainNode.VrnExtType.RecordMember)

        expr.Push(builder.BuildLoad2(value.TypeRef, value.ValueRef));
    }

    public override void Visit(CharNode node)
    {
        expr.Push(
            LLVMValueRef.CreateConstInt(
                module.Context.Int8Type, //a
                (ulong)(node.Value)
            )
        );
    }

    public override void Visit(BoolNode node)
    {
        expr.Push(
            LLVMValueRef.CreateConstInt(
                module.Context.Int1Type, //a
                (ulong)(
                    node.GetValueAsInt() //a
                )
            )
        );
    }

    public override void Visit(StringNode node)
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
        expr.Push(String);
    }

    public override void Visit(MathOpNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);

        LLVMValueRef R = expr.Pop();
        LLVMValueRef L = expr.Pop();
        if (_types(L.TypeOf) == Types.INTEGER)
        {
            expr.Push(HandleIntOp(L, node.Op, R));
        }
        else if (_types(L.TypeOf) == Types.FLOAT)
        {
            expr.Push(HandleFloatOp(L, node.Op, R));
        }
        else if (_types(L.TypeOf) == Types.STRING)
        {
            expr.Push(HandleString(L, R));
        }
    }

    public override void Visit(BooleanExpressionNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);

        LLVMValueRef R = expr.Pop();
        LLVMValueRef L = expr.Pop();
        if (_types(L.TypeOf) == Types.INTEGER)
        {
            expr.Push(
                HandleIntBoolOp(
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
                )
            );
        }
        else if (_types(L.TypeOf) == Types.FLOAT)
        {
            expr.Push(
                HandleFloatBoolOp(
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
                )
            );
        }
    }

    public override void Visit(RecordNode node)
    {
        // this cannot be done from the prototype becuase we cannot attach llvm types to Type without putting dependency of llvm for the Types file
        // also we do not know the order by which types are added to the llvm module
        var record = context.GetCustomType(node.Name);
        var args = node.NewType.Fields.Select(
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

    public override void Visit(ParameterNode node)
    {
        if (node.IsVariable)
        {
            var vars = context.GetVariable(node.Variable?.Name);
        }
        else
        {
            node.Constant.Accept(this);
        }
    }

    public override void Visit(FunctionCallNode node)
    {
        var function =
            context.GetFunction(node.Name)
            ?? throw new Exception($"function {node.Name} not found");
        // if any arguement is not mutable, but is required to be mutable
        if (
            function
                .ArguementMutability.Zip( //mutable
                    node.Parameters //multable
                    .Select(p => p.IsVariable)
                )
                .Any(a => a is { First: true, Second: false })
        )
        {
            throw new Exception($"call to {node.Name} has a mismatch of mutability");
        }

        var parameters = node.Parameters.Select(p =>
        {
            p.Accept(this);
            return expr.Pop();
        });
        builder.BuildCall2(function.TypeOf, function.Function, parameters.ToArray());
    }

    public override void Visit(FunctionNode node)
    {
        var function = (LLVMFunction)context.GetFunction(node.Name);
        context.CurrentFunction = function;
        context.ResetLocal();
        var block = function.AppendBasicBlock("entry");
        builder.PositionAtEnd(block);
        foreach (
            var (param, index) in node.ParameterVariables.Select((param, index) => (param, index))
        )
        {
            var llvmParam = function.GetParam((uint)index);
            var name = param.GetNameSafe();
            LLVMValueRef paramAllocation = builder.BuildAlloca(llvmParam.TypeOf, name);
            var parameter = context.NewVariable(param.Type)(
                paramAllocation, //a
                !param.IsConstant
            );

            builder.BuildStore(llvmParam, paramAllocation);
            context.AddVariable(name, parameter, false);
        }

        function.Linkage = LLVMLinkage.LLVMExternalLinkage;

        node.LocalVariables.ForEach(variable => variable.Accept(this));
        node.Statements.ForEach(s => s.Accept(this));
        // return 0 to singify ok
        builder.BuildRet(LLVMValueRef.CreateConstInt(module.Context.Int32Type, (ulong)0));
        context.ResetLocal();
    }

    public override void Visit(WhileNode node)
    {
        var whileCond = context.CurrentFunction.AppendBasicBlock("while.cond");
        var whileBody = context.CurrentFunction.AppendBasicBlock("while.body");
        var whileDone = context.CurrentFunction.AppendBasicBlock("while.done");
        builder.BuildBr(whileCond);
        builder.PositionAtEnd(whileCond);
        node.Expression.Accept(this);
        var condition = expr.Pop();
        builder.BuildCondBr(condition, whileBody, whileDone);
        builder.PositionAtEnd(whileBody);
        node.Children.ForEach(c => c.Accept(this));
        builder.BuildBr(whileCond);
        builder.PositionAtEnd(whileDone);
    }

    public override void Visit(AssignmentNode node)
    {
        var llvmValue = context.GetVariable(node.Target.Name);
        Console.WriteLine(node.ToString());

        node.Expression.Accept(this);
        var expression = expr.Pop();
        if (!llvmValue.IsMutable) // :')
        {
            throw new Exception($"tried to mutate non mutable variable {node.Target.Name}");
        }

        if (
            node.Target.ExtensionType == VariableUsagePlainNode.VrnExtType.ArrayIndex
            || node.Target.ExtensionType == VariableUsagePlainNode.VrnExtType.RecordMember
        )
        {
            node.Target.Extension?.Accept(this);
            var a = builder.BuildGEP2(llvmValue.TypeRef, llvmValue.ValueRef, new[] { expr.Pop() });
            builder.BuildStore(expression, a);
        }
        else
        {
            builder.BuildStore(expression, llvmValue.ValueRef);
        }
    }

    public override void Visit(EnumNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(ModuleNode node)
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
    }

    public override void Visit(IfNode node)
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
            node.Expression.Accept(this);
            var condition = expr.Pop();
            var ifBlock = context.CurrentFunction.AppendBasicBlock("if block");
            var elseBlock = context.CurrentFunction.AppendBasicBlock("else block");
            var afterBlock = context.CurrentFunction.AppendBasicBlock("after if statement");
            builder.BuildCondBr(condition, ifBlock, elseBlock);

            builder.PositionAtEnd(ifBlock);
            node.Children.ForEach(c => c.Accept(this));
            builder.BuildBr(afterBlock);
            builder.PositionAtEnd(elseBlock);
            node.NextIfNode?.Accept(this);
            builder.BuildBr(afterBlock);
            builder.PositionAtEnd(afterBlock);
        }
        else
        {
            node.Children.ForEach(c => c.Accept(this));
        }
    }

    public override void Visit(RepeatNode node)
    {
        var whileBody = context.CurrentFunction.AppendBasicBlock("while.body");
        var whileDone = context.CurrentFunction.AppendBasicBlock("while.done");
        // first execute the body
        builder.BuildBr(whileBody);
        builder.PositionAtEnd(whileBody);
        node.Children.ForEach(c => c.Accept(this));
        // and then test the condition
        node.Expression.Accept(this);
        var condition = expr.Pop();
        builder.BuildCondBr(condition, whileBody, whileDone);
        builder.PositionAtEnd(whileDone);
    }

    public override void Visit(VariableDeclarationNode node)
    {
        var name = node.GetNameSafe();
        // TODO: only alloca when !isConstant

        LLVMValueRef v = builder.BuildAlloca(
            // isVar is false, because we are already creating it using alloca which makes it var
            context.GetLLVMTypeFromShankType(node.Type) ?? throw new Exception("null type"),
            name
        );
        var variable = context.NewVariable(node.Type);
        context.AddVariable(name, variable(v, !node.IsConstant), false);
    }

    public override void Visit(ProgramNode node)
    {
        context.SetModules(node.Modules.Keys);

        foreach (var keyValuePair in node.Modules)
        {
            keyValuePair.Value.VisitProto(new LLVMVisitPrototype(context, builder, module));
        }

        foreach (var keyValuePair in node.Modules)
        {
            keyValuePair.Value.Accept(this);
        }
    }

    public override void Visit(ForNode node)
    {
        var forStart = context.CurrentFunction.AppendBasicBlock("for.start");
        var afterFor = context.CurrentFunction.AppendBasicBlock("for.after");
        var forBody = context.CurrentFunction.AppendBasicBlock("for.body");
        var forIncremnet = context.CurrentFunction.AppendBasicBlock("for.inc");
        // TODO: assign loop variable initial from value
        builder.BuildBr(forStart);
        builder.PositionAtEnd(forStart);
        // we have to compile the to and from in the loop so that the get run each time, we go through the loop
        // in case we modify them in the loop

        node.From.Accept(this);
        var fromValue = expr.Pop();
        node.To.Accept(this);
        var toValue = expr.Pop();
        node.Variable.Accept(new LLVMExpr(context, builder, module));
        var currentIterable = expr.Pop();
        // right now we assume, from, to, and the variable are all integers
        // in the future we should check and give some error at runtime/compile time if not
        // TODO: signed or unsigned comparison
        var condition = builder.BuildAnd(
            builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, currentIterable, fromValue),
            builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, currentIterable, toValue)
        );
        builder.BuildCondBr(condition, forBody, afterFor);
        builder.PositionAtEnd(forBody);
        node.Children.ForEach(c => c.Accept(this));
        builder.BuildBr(forIncremnet);
        builder.PositionAtEnd(forIncremnet);
        // TODO: increment
        builder.BuildBr(forStart);
        builder.PositionAtEnd(afterFor);
    }
}
