using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class LLVMStatement : StatementVisitor
{
    public Context _context { get; set; }
    public LLVMBuilderRef _builder { get; set; }
    public LLVMModuleRef _module { get; set; }

    public LLVMStatement(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        _context = context;
        _builder = builder;
        _module = module;
    }

    public override void Accept(FunctionCallNode node)
    {
        var function =
            _context.GetFunction(node.Name)
            ?? throw new Exception($"function {node.Name} not found");
        // if any arguement is not mutable, but is required to be mutable
        if (
            function
                .ArguementMutability.Zip(node.Parameters.Select(p => p.IsVariable))
                .Any(a => a is { First: true, Second: false })
        )
        {
            throw new Exception($"call to {node.Name} has a mismatch of mutability");
        }

        var parameters = node.Parameters.Select(
            p => p.Visit(new LLVMExpr(_context, _builder, _module))
        );
        _builder.BuildCall2(function.TypeOf, function.Function, parameters.ToArray());
    }

    public override void Accept(FunctionNode node)
    {
        var function = (LLVMFunction)_context.GetFunction(node.Name);
        _context.CurrentFunction = function;
        _context.ResetLocal();
        var block = function.AppendBasicBlock("entry");
        _builder.PositionAtEnd(block);
        foreach (
            var (param, index) in node.ParameterVariables.Select((param, index) => (param, index))
        )
        {
            var llvmParam = function.GetParam((uint)index);
            var name = param.GetNameSafe();
            LLVMValueRef paramAllocation = _builder.BuildAlloca(llvmParam.TypeOf, name);
            var parameter = _context.newVariable(param.Type, param.UnknownType)(
                paramAllocation,
                !param.IsConstant
            );

            _builder.BuildStore(llvmParam, paramAllocation);
            _context.AddVaraible(name, parameter, false);
        }

        function.Linkage = LLVMLinkage.LLVMExternalLinkage;

        node.LocalVariables.ForEach(variable => variable.Visit(this));
        node.Statements.ForEach(s => s.Visit(this));
        // return 0 to singify ok
        _builder.BuildRet(LLVMValueRef.CreateConstInt(_module.Context.Int32Type, (ulong)0));
        _context.ResetLocal();
    }

    public override void Accept(WhileNode node)
    {
        var whileCond = _context.CurrentFunction.AppendBasicBlock("while.cond");
        var whileBody = _context.CurrentFunction.AppendBasicBlock("while.body");
        var whileDone = _context.CurrentFunction.AppendBasicBlock("while.done");
        _builder.BuildBr(whileCond);
        _builder.PositionAtEnd(whileCond);
        var condition = node.Expression.Visit(new LLVMExpr(_context, _builder, _module));
        _builder.BuildCondBr(condition, whileBody, whileDone);
        _builder.PositionAtEnd(whileBody);
        node.Children.ForEach(c => c.Visit(this));
        _builder.BuildBr(whileCond);
        _builder.PositionAtEnd(whileDone);
    }

    public override void Accept(AssignmentNode node)
    {
        var llvmValue = _context.GetVaraible(node.Target.Name);
        if (!llvmValue.IsMutable)
        {
            throw new Exception($"tried to mutate non mutable variable {node.Target.Name}");
        }
        // node.Expression<LLVMValueRef>.Visit(new LLVM)

        _builder.BuildStore(
            node.Expression.Visit(new LLVMExpr(_context, _builder, _module)),
            llvmValue.ValueRef
        );
    }

    public override void Accept(EnumNode node)
    {
        throw new NotImplementedException();
    }

    public override void Accept(ModuleNode node)
    {
        _context.SetCurrentModule(node.Name);
        // then we add to our scope all our imports
        foreach (var (moduleName, imports) in node.ImportTargetNames)
        {
            var shankModule = _context.Modules[moduleName];
            foreach (var import in imports)
            {
                // TODO: type imports
                if (shankModule.Functions.TryGetValue(import, out var function))
                {
                    _context.addFunction(import, function);
                }
            }
        }

        node //modnode
        .GetFunctionsAsList() //list
            .ForEach(f => f.Visit(this));
    }

    public override void Accept(IfNode node)
    {
        if (node.Expression != null)
        // if the condition is null then it's an else statement, which can only happen after an if statement
        // so is it's an if statement, and since we compile if statements recursively, like how we parse them
        // we know that we already created the block for the else statement, when compiling the if part
        // so we just compile the statements in the else block
        // if the condition is not null we compile the condition, create two blocks one for if it's true, and for when the condition is false
        // we then just compile the statements for when the condition is true under the true block, followed by a goto to an after block
        // and we visit(compile) the IfNode for when the condition is false if needed, followed by a goto to the after branch
        // note we could make this a bit better by checking if next is null and then make the conditional branch to after block in the false cas
        {
            var condition = node.Expression.Visit(new LLVMExpr(_context, _builder, _module));
            var ifBlock = _context.CurrentFunction.AppendBasicBlock("if block");
            var elseBlock = _context.CurrentFunction.AppendBasicBlock("else block");
            var afterBlock = _context.CurrentFunction.AppendBasicBlock("after if statement");
            _builder.BuildCondBr(condition, ifBlock, elseBlock);

            _builder.PositionAtEnd(ifBlock);
            node.Children.ForEach(c => c.Visit(this));
            _builder.BuildBr(afterBlock);
            _builder.PositionAtEnd(elseBlock);
            node.NextIfNode?.Visit(this);
            _builder.BuildBr(afterBlock);
            _builder.PositionAtEnd(afterBlock);
        }
        else
        {
            node.Children.ForEach(c => c.Visit(this));
        }
    }

    public override void Accept(RepeatNode node)
    {
        var whileBody = _context.CurrentFunction.AppendBasicBlock("while.body");
        var whileDone = _context.CurrentFunction.AppendBasicBlock("while.done");
        // first execute the body
        _builder.BuildBr(whileBody);
        _builder.PositionAtEnd(whileBody);
        node.Children.ForEach(c => c.Visit(this));
        // and then test the condition
        var condition = node.Expression.Visit(new LLVMExpr(_context, _builder, _module));
        _builder.BuildCondBr(condition, whileBody, whileDone);
        _builder.PositionAtEnd(whileDone);
    }

    public override void Accept(RecordNode node)
    {
        Console.WriteLine("hello world");
        throw new Exception("error");

        // _builder.BuildAlloca(_context.CurrentModule.CustomTypes[node.Name]);
        // _builder.BuildStructGEP2(
        // _context.CurrentModule.CustomTypes[node.Name],
        // _builder.BuildAlloca(_context.CurrentModule.CustomTypes[node.Name]),
        // 1
        // );
        // _context.CurrentModule.CustomTypes
    }

    public override void Accept(VariableNode node)
    {
        var name = node.GetNameSafe();
        // TODO: only alloca when !isConstant

        LLVMValueRef v = _builder.BuildAlloca(
            // isVar is false, because we are already creating it using alloca which makes it var
            _context.GetLLVMTypeFromShankType(node.Type, node.UnknownType)
                ?? throw new Exception("null type"),
            name
        );
        var variable = _context.newVariable(node.Type, node.UnknownType);
        _context.AddVaraible(name, variable(v, !node.IsConstant), false);
    }

    public override void Accept(ProgramNode node)
    {
        _context.setModules(node.Modules.Keys);

        foreach (var keyValuePair in node.Modules)
        {
            keyValuePair.Value.VisitProto(new LLVMVisitPrototype(_context, _builder, _module));
        }

        foreach (var keyValuePair in node.Modules)
        {
            keyValuePair.Value.Visit(this);
        }
    }

    public override void Accept(ForNode node)
    {
        var forStart = _context.CurrentFunction.AppendBasicBlock("for.start");
        var afterFor = _context.CurrentFunction.AppendBasicBlock("for.after");
        var forBody = _context.CurrentFunction.AppendBasicBlock("for.body");
        var forIncremnet = _context.CurrentFunction.AppendBasicBlock("for.inc");
        // TODO: assign loop variable initial from value
        _builder.BuildBr(forStart);
        _builder.PositionAtEnd(forStart);
        // we have to compile the to and from in the loop so that the get run each time, we go through the loop
        // in case we modify them in the loop


        var fromValue = node.From.Visit(new LLVMExpr(_context, _builder, _module));
        var toValue = node.To.Visit(new LLVMExpr(_context, _builder, _module));
        var currentIterable = node.Variable.Visit(new LLVMExpr(_context, _builder, _module));

        // right now we assume, from, to, and the variable are all integers
        // in the future we should check and give some error at runtime/compile time if not
        // TODO: signed or unsigned comparison
        var condition = _builder.BuildAnd(
            _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, currentIterable, fromValue),
            _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, currentIterable, toValue)
        );
        _builder.BuildCondBr(condition, forBody, afterFor);
        _builder.PositionAtEnd(forBody);
        node.Children.ForEach(c => c.Visit(this));
        _builder.BuildBr(forIncremnet);
        _builder.PositionAtEnd(forIncremnet);
        // TODO: increment
        _builder.BuildBr(forStart);
        _builder.PositionAtEnd(afterFor);
    }
}
