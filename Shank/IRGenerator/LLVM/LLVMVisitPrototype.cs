using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.IRGenerator;

namespace Shank.ExprVisitors;

public class LLVMVisitPrototype(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    : VisitPrototype
{
    public void DebugRuntime(string format, LLVMValueRef value)
    {
        builder.BuildCall2(
            context.CFuntions.printf.TypeOf,
            context.CFuntions.printf.Function,
            [builder.BuildGlobalStringPtr(format), value]
        );
    }

    public override void Accept(FunctionNode node)
    {
        var fnRetTy = module.Context.Int32Type;
        var args = node.ParameterVariables.Select(
            s =>
                context.GetLLVMTypeFromShankType(s.Type)
                ?? throw new CompilerException($"type of parameter {s.Name} is not found", s.Line)
        );
        var arguementMutability = node.ParameterVariables.Select(p => !p.IsConstant);
        node.Name = (node.Name.Equals("start") ? "main" : node.Name);
        var function = module.addFunction(
            node.Name,
            LLVMTypeRef.CreateFunction(fnRetTy, args.ToArray()),
            arguementMutability
        );
        foreach (
            var (param, index) in node.ParameterVariables.Select((param, index) => (param, index))
        )
        {
            var llvmParam = function.GetParam((uint)index);
            var name = param.GetNameSafe();
            llvmParam.Name = name;
        }

        context.AddFunction(node.Name, function);
    }

    public override void Accept(ModuleNode node)
    {
        context.SetCurrentModule(node.Name);
        node.Records.Values.ToList().ForEach(f => f.VisitProto(this));
        node.GetFunctionsAsList().ForEach(f => f.VisitProto(this));
        node.GlobalVariables.Values.ToList().ForEach(f => f.VisitProto(this));
    }

    public override void Accept(RecordNode node)
    {
        var llvmRecord = module.Context.CreateNamedStruct(node.Name);
        var record = new LLVMStructType(node.NewType, llvmRecord, node.NewType.Fields.Select(s => s.Key).ToList());
        context.CurrentModule.CustomTypes.Add(node.Name, record);
    }

    public override void Accept(VariableNode node)
    {
        var a = module.AddGlobal(
            context.GetLLVMTypeFromShankType(node.Type) ?? throw new Exception("null type"),
            node.GetNameSafe()
        );
        var variable = context.NewVariable(node.Type);
        context.AddVariable(node.GetNameSafe(), variable(a, !node.IsConstant), true);
    }
}
