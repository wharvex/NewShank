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

public class LLVMVisitor
{
    // Context _context;
    // LLVMBuilderRef _builder;
    // LLVMModuleRef _module;
    //
    // public LLVMVisitor(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    // {
    //     _context = context;
    //     _builder = builder;
    //     _module = module;
    // }
    //
    // private Types _types(LLVMTypeRef typeRef)
    // {
    //     if (
    //         typeRef == LLVMTypeRef.Int64
    //         || typeRef == LLVMTypeRef.Int1
    //         || typeRef == LLVMTypeRef.Int8
    //     )
    //         return Types.INTEGER;
    //     else if (typeRef == LLVMTypeRef.Double)
    //         return Types.FLOAT;
    //     else if (typeRef == _context.StringType)
    //         return Types.STRING;
    //     else
    //         throw new Exception("undefined type");
    // }
    //
    // public override LLVMValueRef Visit(IntNode node)
    // {
    //     return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)node.Value);
    // }
    //
    // public override LLVMValueRef Visit(FloatNode node)
    // {
    //     return LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, node.Value);
    // }
    //
    // public override LLVMValueRef Visit(VariableUsageNode node)
    // {
    //     LLVMValue value = _context.GetVariable(node.Name);
    //     return _builder.BuildLoad2(value.TypeRef, value.ValueRef);
    // }
    //
    // public override LLVMValueRef Visit(CharNode node)
    // {
    //     return LLVMValueRef.CreateConstInt(_module.Context.Int8Type, (ulong)(node.Value));
    // }
    //
    // public override LLVMValueRef Visit(BoolNode node)
    // {
    //     return LLVMValueRef.CreateConstInt(_module.Context.Int1Type, (ulong)(node.GetValueAsInt()));
    // }
    //
    // public override LLVMValueRef Visit(StringNode node)
    // {
    //     // a string in llvm is just length + content
    //     var stringLength = LLVMValueRef.CreateConstInt(
    //         _module.Context.Int32Type,
    //         (ulong)node.Value.Length
    //     );
    //
    //     var stringContent = _builder.BuildGlobalStringPtr(node.Value);
    //
    //     // if we never mutate the string part directly, meaning when we do assignment we assign it a new string struct, then we do not need to do this malloc,
    //     // and we could just insert, the string constant in the string struct, we could do this because we don't directly mutate the string,
    //     // and the way we currently define string constants, they must not be mutated
    //     // one problem is that constant llvm strings are null terminated
    //     var stringPointer = _builder.BuildMalloc(
    //         LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)node.Value.Length)
    //     );
    //     _builder.BuildCall2(
    //         _context.CFuntions.memcpy.TypeOf,
    //         _context.CFuntions.memcpy.Function,
    //         [stringPointer, stringContent, stringLength]
    //     );
    //     var String = LLVMValueRef.CreateConstStruct(
    //         [
    //             LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
    //             LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0))
    //         ],
    //         false
    //     );
    //     String = _builder.BuildInsertValue(String, stringLength, 0);
    //     String = _builder.BuildInsertValue(String, stringPointer, 1);
    //     return String;
    // }
    //
    // public override LLVMValueRef Visit(MathOpNode node)
    // {
    //     LLVMValueRef L = node.Left.Visit(this, _context, _builder, _module);
    //     LLVMValueRef R = node.Right.Visit(this, _context, _builder, _module);
    //     if (_types(L.TypeOf) == Types.INTEGER)
    //     {
    //         return node.Op switch
    //         {
    //             MathOpNode.MathOpType.Plus => _builder.BuildAdd(L, R, "addtmp"),
    //             MathOpNode.MathOpType.Minus => _builder.BuildSub(L, R, "subtmp"),
    //             MathOpNode.MathOpType.Times => _builder.BuildMul(L, R, "multmp"),
    //             MathOpNode.MathOpType.Divide => _builder.BuildSDiv(L, R, "divtmp"),
    //             MathOpNode.MathOpType.Modulo => _builder.BuildURem(L, R, "modtmp"),
    //             _ => throw new Exception("unsupported operation")
    //         };
    //     }
    //     else if (_types(L.TypeOf) == Types.FLOAT)
    //     {
    //         return node.Op switch
    //         {
    //             MathOpNode.MathOpType.Plus => _builder.BuildFAdd(L, R, "addtmp"),
    //             MathOpNode.MathOpType.Minus => _builder.BuildFSub(L, R, "subtmp"),
    //             MathOpNode.MathOpType.Times => _builder.BuildFMul(L, R, "multmp"),
    //             MathOpNode.MathOpType.Divide => _builder.BuildFDiv(L, R, "divtmp"),
    //             _ => throw new Exception("unsupported operation")
    //         };
    //     }
    //     else if (_types(L.TypeOf) == Types.STRING)
    //     {
    //         if (node.Op == MathOpNode.MathOpType.Plus)
    //         {
    //             LLVMValueRef r = node.Right.Visit(this, _context, _builder, _module);
    //             LLVMValueRef l = node.Left.Visit(this, _context, _builder, _module);
    //             var lSize = _builder.BuildExtractValue(l, 0);
    //             var rSize = _builder.BuildExtractValue(r, 0);
    //             var newSize = _builder.BuildAdd(lSize, rSize);
    //             // allocate enough (or perhaps in the future more than enough) space, for the concatenated string
    //             // I think this has to be heap allocated, because we can assign this to an out parameter of a function, and we would then lose it if it was stack allocated
    //             var newContent = _builder.BuildCall2(
    //                 _context.CFuntions.malloc.TypeOf,
    //                 _context.CFuntions.malloc.Function,
    //                 [newSize]
    //             );
    //             // fill the first part of the string
    //             _builder.BuildCall2(
    //                 _context.CFuntions.memcpy.TypeOf,
    //                 _context.CFuntions.memcpy.Function,
    //                 [newContent, _builder.BuildExtractValue(l, 1), lSize]
    //             );
    //             // fill the second part of the string
    //             // first "increment" the pointer of the string to be after the contents of the first part
    //             var secondPart = _builder.BuildInBoundsGEP2(LLVMTypeRef.Int8, newContent, [lSize]);
    //             _builder.BuildCall2(
    //                 _context.CFuntions.memcpy.TypeOf,
    //                 _context.CFuntions.memcpy.Function,
    //                 [secondPart, _builder.BuildExtractValue(r, 1), rSize]
    //             );
    //             var String = LLVMValueRef.CreateConstStruct(
    //                 [
    //                     LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
    //                     LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0))
    //                 ],
    //                 false
    //             );
    //             String = _builder.BuildInsertValue(String, newSize, 0);
    //             String = _builder.BuildInsertValue(String, newContent, 1);
    //             return String;
    //         }
    //         else
    //         {
    //             throw new Exception(
    //                 $"strings can only be concatenated, you tried to {node.Op}, with strings"
    //             );
    //         }
    //     }
    //
    //     throw new InvalidOperationException();
    // }
    //
    // public override LLVMValueRef Visit(BooleanExpressionNode node)
    // {
    //     LLVMValueRef L = node.Left.Visit(this, _context, _builder, _module);
    //     LLVMValueRef R = node.Right.Visit(this, _context, _builder, _module);
    //
    //     if (_types(L.TypeOf) == Types.INTEGER)
    //     {
    //         return node.Op switch
    //         {
    //             BooleanExpressionNode.BooleanExpressionOpType.eq
    //                 => _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, L, R, "cmp"),
    //             BooleanExpressionNode.BooleanExpressionOpType.lt
    //                 => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, L, R, "cmp"),
    //             BooleanExpressionNode.BooleanExpressionOpType.le
    //                 => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, L, R, "cmp"),
    //             BooleanExpressionNode.BooleanExpressionOpType.gt
    //                 => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, L, R, "cmp"),
    //             BooleanExpressionNode.BooleanExpressionOpType.ge
    //                 => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, L, R, "cmp"),
    //
    //             _ => throw new Exception("not accepted op")
    //         };
    //     }
    //     else if (_types(L.TypeOf) == Types.FLOAT)
    //     {
    //         return node.Op switch
    //         {
    //             BooleanExpressionNode.BooleanExpressionOpType.eq
    //                 => _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, L, R, "cmp"),
    //             BooleanExpressionNode.BooleanExpressionOpType.lt
    //                 => _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, L, R, "cmp"),
    //             BooleanExpressionNode.BooleanExpressionOpType.le
    //                 => _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLE, L, R, "cmp"),
    //             BooleanExpressionNode.BooleanExpressionOpType.gt
    //                 => _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGT, L, R, "cmp"),
    //             BooleanExpressionNode.BooleanExpressionOpType.ge
    //                 => _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGE, L, R, "cmp"),
    //
    //             _ => throw new Exception("")
    //         };
    //     }
    //
    //     throw new Exception("undefined bool");
    // }
    //
    // public override LLVMValueRef Visit(RecordNode node)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public override void Visit(IfNode node)
    // {
    //     if (node.Expression != null)
    //     // if the condition is null then it's an else statement, which can only happen after an if statement
    //     // so is it's an if statement, and since we compile if statements recursively, like how we parse them
    //     // we know that we already created the block for the else statement, when compiling the if part
    //     // so we just compile the statements in the else block
    //     // if the condition is not null we compile the condition, create two blocks one for if it's true, and for when the condition is false
    //     // we then just compile the statements for when the condition is true under the true block, followed by a goto to an after block
    //     // and we visit(compile) the IfNode for when the condition is false if needed, followed by a goto to the after branch
    //     // note we could make this a bit better by checking if next is null and then make the conditional branch to after block in the false cas
    //     {
    //         var condition = node.Expression.Visit(this, _context, _builder, _module);
    //         var ifBlock = _context.CurrentFunction.AppendBasicBlock("if block");
    //         var elseBlock = _context.CurrentFunction.AppendBasicBlock("else block");
    //         var afterBlock = _context.CurrentFunction.AppendBasicBlock("after if statement");
    //         _builder.BuildCondBr(condition, ifBlock, elseBlock);
    //
    //         _builder.PositionAtEnd(ifBlock);
    //         node.Children.ForEach(c => c.VisitStatement(this, _context, _builder, _module));
    //         _builder.BuildBr(afterBlock);
    //         _builder.PositionAtEnd(elseBlock);
    //         node.NextIfNode?.VisitStatement(this, _context, _builder, _module);
    //         _builder.BuildBr(afterBlock);
    //         _builder.PositionAtEnd(afterBlock);
    //     }
    //     else
    //     {
    //         node.Children.ForEach(c => c.VisitStatement(this, _context, _builder, _module));
    //     }
    // }
    //
    // public override LLVMValueRef Visit(FunctionNode node)
    // {
    //     // should be already created, because the visit prototype method should have been called first
    //     var function = (LLVMFunction)_context.GetFunction(node.Name);
    //     _context.CurrentFunction = function;
    //     _context.ResetLocal();
    //     foreach (
    //         var (param, index) in node.ParameterVariables.Select((param, index) => (param, index))
    //     )
    //     {
    //         var llvmParam = function.GetParam((uint)index);
    //         var name = param.GetNameSafe();
    //         var parameter = _context.NewVariable(param.Type)(llvmParam, !param.IsConstant);
    //
    //         _context.AddVariable(name, parameter, false);
    //     }
    //
    //     function.Linkage = LLVMLinkage.LLVMExternalLinkage;
    //
    //     var block = function.AppendBasicBlock("entry");
    //     _builder.PositionAtEnd(block);
    //     node.LocalVariables.ForEach(variable => variable.Visit(this, _context, _builder, _module));
    //     node.Statements.ForEach(s => s.VisitStatement(this, _context, _builder, _module));
    //     // return 0 to singify ok
    //     _builder.BuildRet(LLVMValueRef.CreateConstInt(_module.Context.Int32Type, (ulong)0));
    //     _context.ResetLocal();
    //     return function.Function;
    // }
    //
    // public override void Visit(FunctionCallNode node)
    // {
    //     var function =
    //         _context.GetFunction(node.Name)
    //         ?? throw new Exception($"function {node.Name} not found");
    //     // if any arguement is not mutable, but is required to be mutable
    //     if (
    //         function
    //             .ArguementMutability.Zip(node.Parameters.Select(p => p.IsVariable))
    //             .Any(a => a is { First: true, Second: false })
    //     )
    //     {
    //         throw new Exception($"call to {node.Name} has a mismatch of mutability");
    //     }
    //
    //     var parameters = node.Parameters.Select(p => p.Visit(this, _context, _builder, _module));
    //     _builder.BuildCall2(function.TypeOf, function.Function, parameters.ToArray());
    // }
    //
    // public override void Visit(WhileNode node)
    // {
    //     var whileCond = _context.CurrentFunction.AppendBasicBlock("while.cond");
    //     var whileBody = _context.CurrentFunction.AppendBasicBlock("while.body");
    //     var whileDone = _context.CurrentFunction.AppendBasicBlock("while.done");
    //     _builder.BuildBr(whileCond);
    //     _builder.PositionAtEnd(whileCond);
    //     var condition = node.Expression.Visit(this, _context, _builder, _module);
    //     _builder.BuildCondBr(condition, whileBody, whileDone);
    //     _builder.PositionAtEnd(whileBody);
    //     node.Children.ForEach(c => c.VisitStatement(this, _context, _builder, _module));
    //     _builder.BuildBr(whileCond);
    //     _builder.PositionAtEnd(whileDone);
    // }
    //
    // public override void Visit(AssignmentNode node)
    // {
    //     var llvmValue = _context.GetVariable(node.Target.Name);
    //     if (!llvmValue.IsMutable)
    //     {
    //         throw new Exception($"tried to mutate non mutable variable {node.Target.Name}");
    //     }
    //
    //     _builder.BuildStore(
    //         node.Expression.Visit(this, _context, _builder, _module),
    //         llvmValue.ValueRef
    //     );
    //     //do something with type information we could either utilize an enum or something along the lines
    // }
    //
    // public override LLVMValueRef Visit(EnumNode node)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public override void Visit(ForNode node)
    // {
    //     throw new NotImplementedException();
    // }
}
