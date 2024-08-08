using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.IRGenerator;

/// <summary>
/// this is the 1st pass. it generates the headers.
/// for functions, global vars, types
/// </summary>
/// <param name="context"></param>
/// <param name="builder"></param>
/// <param name="module"></param>
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

    private void CompileFunctionPrototype(FunctionNode node)
    {
        var functionReturnType = module.Context.Int32Type; //error code return
        var parameters = node.ParameterVariables.Select(
            s => new LLVMParameter(context.GetLLVMTypeFromShankType(s.Type), !s.IsConstant)
        )
            .ToList();
        node.Name = node.Name.Equals("start") ? "main" : node.Name; //main is not sttart :<
        var function = module.addFunction(
            node.Name,
            LLVMTypeRef.CreateFunction(
                functionReturnType,
                parameters
                    .Select(
                        p =>
                            p.Mutable || p.Type is LLVMStructType or LLVMArrayType
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

    private void CompileRecordPrototype(RecordNode node)
    {
        var llvmRecord = module.Context.CreateNamedStruct(node.Name);
        var record = new LLVMStructType(node.Name, llvmRecord);
        context.Records.Add(node.Type.MonomorphizedIndex, record);
    }

    private void CompilePrototypeGlobalVariable(VariableDeclarationNode node)
    {
        //global vars
        var type = context.GetLLVMTypeFromShankType(node.Type);
        var value = module.AddGlobal(type.TypeRef, node.GetNameSafe());
        var variable = context.NewVariable(node.Type);
        value.Initializer = LLVMValueRef.CreateConstNull(type.TypeRef);
        context.AddVariable(node.MonomorphizedName(), variable(value, !node.IsConstant));
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
        var enumType = new LLVMEnumType(obj.TypeName, obj.EType.Variants);
        context.Enums.Add(obj.EType.MonomorphizedIndex, enumType);
    }

    private void CompileBuiltinFunctionPrototype(BuiltInFunctionNode node)
    {
        var fnRetTy = module.Context.Int32Type;

        var parameters = node.ParameterVariables.Select(
            s => new LLVMParameter(context.GetLLVMTypeFromShankType(s.Type), !s.IsConstant)
        )
            .ToList();

        var function = module.addFunction(
            node.Name,
            LLVMTypeRef.CreateFunction(
                fnRetTy,
                parameters
                    .Select(
                        p =>
                            // p.Mutable
                            p.Mutable || p.Type is LLVMStructType or LLVMArrayType
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
