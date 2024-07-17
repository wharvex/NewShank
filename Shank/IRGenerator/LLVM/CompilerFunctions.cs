using LLVMSharp.Interop;

namespace Shank.IRGenerator;

// a facade around a llvm value ref to make it more type safe and also to be able to get the actual type of the function as opposed to a function pointer type
public class LLVMFunction(LLVMValueRef function, string name, LLVMTypeRef type)
{
    public LLVMValueRef Function { get; private set; } = function;
    public string Name { get; } = name;

    public LLVMTypeRef ReturnType => TypeOf.ReturnType;
    public LLVMTypeRef TypeOf { get; } = type;

    public LLVMLinkage Linkage
    {
        get => Function.Linkage;
        set => Function = Function with { Linkage = value };
    }

    public LLVMValueRef GetParam(uint index)
    {
        return Function.GetParam(index);
    }

    public LLVMBasicBlockRef AppendBasicBlock(string entry)
    {
        return Function.AppendBasicBlock(entry);
    }
}

public record struct LLVMParameter(LLVMType Type, bool Mutable);

// represents a function accessible to a shank user
// we need to track each parameter mutability
public class LLVMShankFunction(
    LLVMValueRef function,
    string name,
    LLVMTypeRef type,
    List<LLVMParameter> parameters
) : LLVMFunction(function, name, type)
{
    public List<LLVMParameter> Parameters { get; } = parameters;
}

// this class adds extension methods to llvm modules to add functions, but return them to us as our llvm function facade
public static class LLVMAddFunctionExtension
{
    public static LLVMFunction addFunction(this LLVMModuleRef module, string name, LLVMTypeRef type)
    {
        var function = module.AddFunction(name, type);
        return new LLVMFunction(function, name, type);
    }

    public static LLVMShankFunction addFunction(
        this LLVMModuleRef module,
        string name,
        LLVMTypeRef type,
        List<LLVMParameter> parameters
    )
    {
        var function = module.AddFunction(name, type);
        return new LLVMShankFunction(function, name, type, parameters);
    }
}
