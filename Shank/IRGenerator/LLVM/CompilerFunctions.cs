using LLVMSharp.Interop;

namespace Shank.IRGenerator;

// a facade around a llvm value ref to make it more type safe and also to be able to get the actual type of the function as opposed to a function pointer type
// TODO: move all llvm facades to their own file
public class LLVMFunction
{
    public LLVMValueRef Function { get; private set; }
    public LLVMTypeRef ReturnType => TypeOf.ReturnType;
    public LLVMTypeRef TypeOf { get; }

    // TODO: move module logic out of here
    public LLVMFunction(LLVMModuleRef module, string name, LLVMTypeRef type)
    {
        Function = module.AddFunction(name, type);
        TypeOf = type;
    }

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
    LLVMModuleRef module,
    string name,
    LLVMTypeRef type,
    List<LLVMParameter> parameters
) : LLVMFunction(module, name, type)
{
    public List<LLVMParameter> Parameters { get; } = parameters;
}

// this class adds extension methods to llvm modules to add functions, but return them to us as our llvm function facade
public static class LLVMAddFunctionExtension
{
    public static LLVMFunction addFunction(this LLVMModuleRef module, string name, LLVMTypeRef type)
    {
        return new LLVMFunction(module, name, type);
    }

    public static LLVMShankFunction addFunction(
        this LLVMModuleRef module,
        string name,
        LLVMTypeRef type,
        List<LLVMParameter> parameters
    )
    {
        return new LLVMShankFunction(module, name, type, parameters);
    }
}