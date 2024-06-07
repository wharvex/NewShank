using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class LLVMVisitPrototype : VisitPrototype
{
    public Context _context { get; set; }
    public LLVMBuilderRef _builder { get; set; }
    public LLVMModuleRef _module { get; set; }

    public LLVMVisitPrototype(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        _context = context;
        _builder = builder;
        _module = module;
    }

    public override void Accept(FunctionNode node)
    {
        var fnRetTy = _module.Context.Int32Type;
        var args = node.ParameterVariables.Select(
            s =>
                _context.GetLLVMTypeFromShankType(s.NewType)
                ?? throw new CompilerException($"type of parameter {s.Name} is not found", s.Line)
        );
        var arguementMutability = node.ParameterVariables.Select(p => !p.IsConstant);
        node.Name = (node.Name.Equals("start") ? "main" : node.Name);
        var function = _module.addFunction(
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

        _context.addFunction(node.Name, function);
    }

    public override void Accept(ModuleNode node)
    {
        _context.SetCurrentModule(node.Name);
        node.Records.Values.ToList().ForEach(f => f.VisitProto(this));
        node.GetFunctionsAsList().ForEach(f => f.VisitProto(this));
    }

    public override void Accept(RecordNode node)
    {
        var args = node.NewType.Fields.Select(
            s =>
                _context.GetLLVMTypeFromShankType(s.Value)
                ?? throw new CompilerException($"type of parameter {s.Key} is not found", node.Line)
        );
        var a = LLVMTypeRef.CreateStruct(args.ToArray(), false);
        _context.CurrentModule.CustomTypes.Add(node.Name, a);
    }
}
