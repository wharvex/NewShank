using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.IRGenerator;

namespace Shank.IRGenerator;

public class PrototypeCompiler(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
{
    public void DebugRuntime(string format, LLVMValueRef value)
    {
        builder.BuildCall2(
            context.CFuntions.printf.TypeOf,
            context.CFuntions.printf.Function,
            [builder.BuildGlobalStringPtr(format), value]
        );
    }

    public void CompileFunctionPrototype(FunctionNode node)
    {
        var fnRetTy = module.Context.Int32Type;
        var args = node.ParameterVariables.Select(
            s =>
                context.GetLLVMTypeFromShankType(s.Type, !s.IsConstant)
                ?? throw new CompilerException(
                    $"" + $"type of parameter {s.Name} is not found",
                    s.Line
                )
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

        context.AddFunction((TypedModuleIndex)node.MonomorphizedName, function);
    }


    public void CompileRecordPrototype(RecordNode node)
    {
        var llvmRecord = module.Context.CreateNamedStruct(node.Name);
        var record = new LLVMStructType(
            node.Type,
            llvmRecord,
            node.Type.Fields.Select(s => s.Key).ToList()
        );
        context.Records.Add(node.Type.MonomorphizedIndex, record);
    }

    public void CompilePrototypeGlobalVariable(VariableDeclarationNode node)
    {
        var a = module.AddGlobal(
            context.GetLLVMTypeFromShankType(node.Type) ?? throw new Exception("null type"),
            node.GetNameSafe()
        );
        // a.Linkage = LLVMLinkage.LLVMExternalLinkage;
        // a.Linkage = LLVMLinkage.LLVMCommonLinkage;
        // a.SetAlignment(4);
        // a.Initializer = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0);
        var variable = context.NewVariable(node.Type);
        a.Initializer = LLVMValueRef.CreateConstNull(
            context.GetLLVMTypeFromShankType(node.Type) ?? throw new Exception("null type")
        );
        context.AddVariable(node.MonomorphizedName(), variable(a, !node.IsConstant));
    }

    public void CompilePrototypes(MonomorphizedProgramNode programNode)
    {
        programNode.Records.Values.ToList().ForEach(CompileRecordPrototype);
        programNode.Enums.Values.ToList().ForEach(CompileEnumPrototype);
        programNode.Functions.Values.ToList().ForEach(CompileFunctionPrototype);
        programNode.BuiltinFunctions.Values.ToList().ForEach(CompileBuiltinFunctionPrototype);
        programNode.GlobalVariables.Values.ToList().ForEach(CompilePrototypeGlobalVariable);
    }

    private void CompileEnumPrototype(EnumNode obj)
    {
        throw new NotImplementedException();
    }

    private void CompileBuiltinFunctionPrototype(BuiltInFunctionNode node)
    {
        var fnRetTy = module.Context.Int32Type;

        var args = node.ParameterVariables.Select(
            s =>
                context.GetLLVMTypeFromShankType(s.Type, !s.IsConstant)
                ?? throw new CompilerException(
                    $"" + $"type of parameter {s.Name} is not found",
                    s.Line
                )
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

        context.AddBuiltinFunction((TypedBuiltinIndex)node.MonomorphizedName, function);
    }
}