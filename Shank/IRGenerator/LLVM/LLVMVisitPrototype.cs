using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class LLVMVisitPrototype(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    : VisitPrototype
{
    public override void Accept(FunctionNode node)
    {
        var fnRetTy = module.Context.Int32Type;
        var args = node.ParameterVariables.Select(
            s =>
                context.GetLLVMTypeFromShankType(s.Type, !s.IsConstant, s.UnknownType)
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

        context.addFunction(node.Name, function);
    }

    public override void Accept(ModuleNode node)
    {
        context.SetCurrentModule(node.Name);
        node.Records.Values.ToList().ForEach(f => f.VisitProto(this));
        node.GetFunctionsAsList().ForEach(f => f.VisitProto(this));
    }

    public override void Accept(RecordNode node)
    {
        var args = node.Members2.Select(
            s =>
                context.GetLLVMTypeFromShankType(s.Type, !s.IsConstant, s.UnknownType)
                ?? throw new CompilerException($"type of parameter {s.Name} is not found", s.Line)
        );
        var a = LLVMTypeRef.CreateStruct(args.ToArray(), false);
        context.CurrentModule.CustomTypes.Add(node.Name, a);
    }
}
