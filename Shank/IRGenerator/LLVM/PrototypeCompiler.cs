using LLVMSharp.Interop;
using Shank.ASTNodes;

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
        var parameters = node.ParameterVariables.Select(
            s => new LLVMParameter(context.GetLLVMTypeFromShankType(s.Type), !s.IsConstant)
        )
            .ToList();
        node.Name = (node.Name.Equals("start") ? "main" : node.Name);
        var function = module.addFunction(
            node.Name,
            LLVMTypeRef.CreateFunction(
                fnRetTy,
                parameters
                    .Select(
                        p =>
                            p.Mutable
                                ? LLVMTypeRef.CreatePointer(p.Type.TypeRef, 0)
                                : p.Type.TypeRef
                    )
                    .ToArray()
            ),
            parameters
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
        var record = new LLVMStructType(node.Name, llvmRecord);
        context.Records.Add(node.Type.MonomorphizedIndex, record);
    }

    public void CompilePrototypeGlobalVariable(VariableDeclarationNode node)
    {
        var type = context.GetLLVMTypeFromShankType(node.Type);
        var a = module.AddGlobal(type.TypeRef, node.GetNameSafe());
        // a.Linkage = LLVMLinkage.LLVMExternalLinkage;
        // a.Linkage = LLVMLinkage.LLVMCommonLinkage;
        // a.SetAlignment(4);
        // a.Initializer = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0);
        var variable = context.NewVariable(node.Type);
        a.Initializer = LLVMValueRef.CreateConstNull(type.TypeRef);
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
        // var llvmRecord = module.Context.CreateNamedStruct(node.Name);
        var enumType = new LLVMEnumType(obj.TypeName, obj.EType.Variants);
        context.Enums.Add(obj.EType.MonomorphizedIndex, enumType);

        // var variable = context.NewVariable(obj.EType);

        // foreach (
        //     var (param, index) in obj.EnumElementsVariables.Select((param, index) => (param, index))
        // )
        // {
        //     var a = module.AddGlobal(LLVMTypeRef.Int64, param.GetNameSafe());
        //     // a.Linkage = LLVMLinkage.LLVMExternalLinkage;
        //     // a.Linkage = LLVMLinkage.LLVMCommonLinkage;
        //     // a.SetAlignment(4);
        //     // a.Initializer = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0);
        //     // var variable = context.NewVariable(node.Type);
        //     a.Initializer = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)index);
        //     context.AddVariable(param.MonomorphizedName(), variable(a, false));
        // }

        // context.AddVariable(
        //     obj.EType.MonomorphizedIndex(),
        //     variable(LLVMValueRef.CreateConstNull(LLVMTypeRef.Int64), false)
        // );

        // context.Records.Add(node.Type.MonomorphizedIndex, record);
    }

    private void CompileBuiltinFunctionPrototype(BuiltInFunctionNode node)
    {
        var fnRetTy = module.Context.Int32Type;

        var parameters = node.ParameterVariables.Select(
            s => new LLVMParameter(context.GetLLVMTypeFromShankType(s.Type), !s.IsConstant)
        )
            .ToList();

        node.Name = (node.Name.Equals("start") ? "main" : node.Name);

        var function = module.addFunction(
            node.Name,
            LLVMTypeRef.CreateFunction(
                fnRetTy,
                parameters
                    .Select(
                        p =>
                            p.Mutable
                                ? LLVMTypeRef.CreatePointer(p.Type.TypeRef, 0)
                                : p.Type.TypeRef
                    )
                    .ToArray()
            ),
            parameters
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
