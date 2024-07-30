// using LLVMSharp.Interop;
// using Shank.ASTNodes;
// using Shank.IRGenerator;
//
// namespace Shank.ExprVisitors;
//
// /// <summary>
// /// Depreciated visitor classes
// /// keeping around in case I forgot something or as reference
// /// </summary>
// /// <param name="context"></param>
// /// <param name="builder"></param>
// /// <param name="module"></param>
// public class LLVMStatement(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
// {
//     // public override void Accept(FunctionCallNode node)
//     // {
//     //     var function =
//     //         context.GetFunction(node.Name)
//     //         ?? throw new Exception($"function {node.Name} not found");
//     //     // if any arguement is not mutable, but is required to be mutable
//     //     if (
//     //         function
//     //             .ArguementMutability.Zip(node.Parameters.Select(p => p.IsVariable))
//     //             .Any(a => a is { First: true, Second: false })
//     //     )
//     //     {
//     //         throw new Exception($"call to {node.Name} has a mismatch of mutability");
//     //     }
//     //
//     //     var parameters = node.Parameters.Select(
//     //         p => p.Accept(new LLVMExpr(context, builder, module))
//     //     );
//     //     builder.BuildCall2(function.TypeOf, function.Function, parameters.ToArray());
//     // }
//     //
//     // public void DebugRuntime(string format, LLVMValueRef value)
//     // {
//     //     builder.BuildCall2(
//     //         context.CFuntions.printf.TypeOf,
//     //         context.CFuntions.printf.Function,
//     //         [builder.BuildGlobalStringPtr(format), value]
//     //     );
//     // }
//     //
//     // public override void Accept(FunctionNode node)
//     // {
//     //     var function = (LLVMFunction)context.GetFunction(node.Name);
//     //     context.CurrentFunction = function;
//     //     context.ResetLocal();
//     //     var block = function.AppendBasicBlock("entry");
//     //     builder.PositionAtEnd(block);
//     //     foreach (
//     //         var (param, index) in node.ParameterVariables.Select((param, index) => (param, index))
//     //     )
//     //     {
//     //         var llvmParam = function.GetParam((uint)index);
//     //         var name = param.GetNameSafe();
//     //         LLVMValueRef paramAllocation = builder.BuildAlloca(llvmParam.TypeOf, name);
//     //         var parameter = context.NewVariable(param.Type)(
//     //             paramAllocation, //a
//     //             !param.IsConstant
//     //         );
//     //
//     //         builder.BuildStore(llvmParam, paramAllocation);
//     //         context.AddVariable(name, parameter, false);
//     //     }
//     //
//     //     function.Linkage = LLVMLinkage.LLVMExternalLinkage;
//     //
//     //     node.LocalVariables.ForEach(variable => variable.Visit(this));
//     //     node.Statements.ForEach(s => s.Visit(this));
//     //     // return 0 to singify ok
//     //     builder.BuildRet(LLVMValueRef.CreateConstInt(module.Context.Int32Type, (ulong)0));
//     //     context.ResetLocal();
//     // }
//     //
//     // public override void Accept(AssignmentNode node)
//     // {
//     //     var llvmValue = context.GetVariable(node.Target.Name);
//     //     Console.WriteLine(node.ToString());
//     //
//     //     var expr = node.Expression.Accept(new LLVMExpr(context, builder, module));
//     //     if (!llvmValue.IsMutable) // :')
//     //     {
//     //         throw new Exception($"tried to mutate non mutable variable {node.Target.Name}");
//     //     }
//     //
//     //     if (node.Target.Extension != null)
//     //     {
//     //         var a = builder.BuildGEP2(
//     //             llvmValue.TypeRef,
//     //             llvmValue.ValueRef,
//     //             new[] { node.Target.Extension.Accept(new LLVMExpr(context, builder, module)) }
//     //         );
//     //         builder.BuildStore(expr, a);
//     //     }
//     //     else
//     //     {
//     //         builder.BuildStore(expr, llvmValue.ValueRef);
//     //     }
//     // }
//     //
//     // public override void Accept(EnumNode node) { }
//     //
//     // public override void Accept(ModuleNode node)
//     // {
//     //     context.SetCurrentModule(node.Name);
//     //     // then we add to our scope all our imports
//     //     foreach (var (moduleName, imports) in node.ImportTargetNames)
//     //     {
//     //         var shankModule = context.Modules[moduleName];
//     //         foreach (var import in imports)
//     //         {
//     //             // TODO: type imports
//     //             if (shankModule.Functions.TryGetValue(import, out var function))
//     //             {
//     //                 context.AddFunction(import, function);
//     //             }
//     //             else if (shankModule.CustomTypes.TryGetValue(import, out var type))
//     //             {
//     //                 context.AddCustomType(import, type);
//     //             }
//     //         }
//     //     }
//     //
//     //     node //modnode
//     //     .GetFunctionsAsList() //list
//     //         .ForEach(f => f.Visit(this));
//     // }
//     //
//     // public override void Accept(IfNode node)
//     // {
//     //     if (node.Expression != null)
//     //     // if the condition is null then it's an else statement, which can only happen after an if statement
//     //     // so is it's an if statement, and since we compile if statements recursively, like how we parse them
//     //     // we know that we already created the block for the else statement, when compiling the if part
//     //     // so we just compile the statements in the else block
//     //     // if the condition is not null we compile the condition, create two blocks one for if it's true, and for when the condition is false
//     //     // we then just compile the statements for when the condition is true under the true block, followed by a goto to an after block
//     //     // and we visit(compile) the IfNode for when the condition is false if needed, followed by a goto to the after branch
//     //     // note we could make this a bit better by checking if next is null and then make the conditional branch to after block in the false cas
//     //     {
//     //         var condition = node.Expression.Accept(new LLVMExpr(context, builder, module));
//     //         var ifBlock = context.CurrentFunction.AppendBasicBlock("if block");
//     //         var elseBlock = context.CurrentFunction.AppendBasicBlock("else block");
//     //         var afterBlock = context.CurrentFunction.AppendBasicBlock("after if statement");
//     //         builder.BuildCondBr(condition, ifBlock, elseBlock);
//     //
//     //         builder.PositionAtEnd(ifBlock);
//     //         node.Children.ForEach(c => c.Visit(this));
//     //         builder.BuildBr(afterBlock);
//     //         builder.PositionAtEnd(elseBlock);
//     //         node.NextIfNode?.Visit(this);
//     //         builder.BuildBr(afterBlock);
//     //         builder.PositionAtEnd(afterBlock);
//     //     }
//     //     else
//     //     {
//     //         node.Children.ForEach(c => c.Visit(this));
//     //     }
//     // }
//     //
//     // public override void Accept(WhileNode node)
//     // {
//     //     var whileCond = context.CurrentFunction.AppendBasicBlock("while.cond");
//     //     var whileBody = context.CurrentFunction.AppendBasicBlock("while.body");
//     //     var whileDone = context.CurrentFunction.AppendBasicBlock("while.done");
//     //     builder.BuildBr(whileCond);
//     //     builder.PositionAtEnd(whileCond);
//     //     var condition = node.Expression.Accept(new LLVMExpr(context, builder, module));
//     //     builder.BuildCondBr(condition, whileBody, whileDone);
//     //     builder.PositionAtEnd(whileBody);
//     //     node.Children.ForEach(c => c.Visit(this));
//     //     builder.BuildBr(whileCond);
//     //     builder.PositionAtEnd(whileDone);
//     // }
//     //
//     // public override void Accept(RepeatNode node)
//     // {
//     //     var whileBody = context.CurrentFunction.AppendBasicBlock("while.body");
//     //     var whileDone = context.CurrentFunction.AppendBasicBlock("while.done");
//     //     // first execute the body
//     //     builder.BuildBr(whileBody);
//     //     builder.PositionAtEnd(whileBody);
//     //     node.Children.ForEach(c => c.Visit(this));
//     //     // and then test the condition
//     //     var condition = node.Expression.Accept(new LLVMExpr(context, builder, module));
//     //     builder.BuildCondBr(condition, whileBody, whileDone);
//     //     builder.PositionAtEnd(whileDone);
//     // }
//     //
//     // public override void Accept(RecordNode node)
//     // {
//     //     // this cannot be done from the prototype becuase we cannot attach llvm types to Type without putting dependency of llvm for the Types file
//     //     // also we do not know the order by which types are added to the llvm module
//     //     var record = context.GetCustomType(node.Name);
//     //     var args = node.Type.Fields.Select(
//     //         s =>
//     //             // for records (and eventually references) we do not hold the actual type of the record, but rather a pointer to it, because llvm does not like direct recursive types
//     //             (
//     //                 s.Key,
//     //                 s.Value is RecordType
//     //                     ? LLVMTypeRef.CreatePointer(
//     //                         (LLVMTypeRef)context.GetLLVMTypeFromShankType(s.Value)!,
//     //                         0
//     //                     )
//     //                     : (LLVMTypeRef)context.GetLLVMTypeFromShankType(s.Value)!
//     //             )
//     //     )
//     //         .ToArray();
//     //     record.LlvmTypeRef.StructSetBody(args.Select(s => s.Item2).ToArray(), false);
//     // }
//     //
//     // public override void Accept(VariableDeclarationNode node)
//     // {
//     //     var name = node.GetNameSafe();
//     //     // TODO: only alloca when !isConstant
//     //
//     //     LLVMValueRef v = builder.BuildAlloca(
//     //         // isVar is false, because we are already creating it using alloca which makes it var
//     //         context.GetLLVMTypeFromShankType(node.Type) ?? throw new Exception("null type"),
//     //         name
//     //     );
//     //     var variable = context.NewVariable(node.Type);
//     //     context.AddVariable(name, variable(v, !node.IsConstant), false);
//     // }
//     //
//     // public override void Accept(ProgramNode node)
//     // {
//     //     context.SetModules(node.Modules.Keys);
//     //
//     //     foreach (var keyValuePair in node.Modules)
//     //     {
//     //         keyValuePair.Value.VisitProto(new LLVMVisitPrototype(context, builder, module));
//     //     }
//     //
//     //     foreach (var keyValuePair in node.Modules)
//     //     {
//     //         keyValuePair.Value.Visit(this);
//     //     }
//     // }
//     //
//     // public override void Accept(ForNode node)
//     // {
//     //     var forStart = context.CurrentFunction.AppendBasicBlock("for.start");
//     //     var afterFor = context.CurrentFunction.AppendBasicBlock("for.after");
//     //     var forBody = context.CurrentFunction.AppendBasicBlock("for.body");
//     //     var forIncremnet = context.CurrentFunction.AppendBasicBlock("for.inc");
//     //     // TODO: assign loop variable initial from value
//     //     builder.BuildBr(forStart);
//     //     builder.PositionAtEnd(forStart);
//     //     // we have to compile the to and from in the loop so that the get run each time, we go through the loop
//     //     // in case we modify them in the loop
//     //
//     //
//     //     var fromValue = node.From.Accept(new LLVMExpr(context, builder, module));
//     //     var toValue = node.To.Accept(new LLVMExpr(context, builder, module));
//     //     var currentIterable = node.Variable.Accept(new LLVMExpr(context, builder, module));
//     //
//     //     // right now we assume, from, to, and the variable are all integers
//     //     // in the future we should check and give some error at runtime/compile time if not
//     //     // TODO: signed or unsigned comparison
//     //     var condition = builder.BuildAnd(
//     //         builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, currentIterable, fromValue),
//     //         builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, currentIterable, toValue)
//     //     );
//     //     builder.BuildCondBr(condition, forBody, afterFor);
//     //     builder.PositionAtEnd(forBody);
//     //     node.Children.ForEach(c => c.Visit(this));
//     //     builder.BuildBr(forIncremnet);
//     //     builder.PositionAtEnd(forIncremnet);
//     //     // TODO: incrementf
//     //     builder.BuildBr(forStart);
//     //     builder.PositionAtEnd(afterFor);
//     // }
// }
