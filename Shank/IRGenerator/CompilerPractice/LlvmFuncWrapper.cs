using LLVMSharp.Interop;
using Shank.Interfaces;

namespace Shank.IRGenerator.CompilerPractice;

public class LlvmFuncWrapper
{
    public LLVMTypeRef Type { get; }
    public LLVMValueRef Func { get; }
    public bool IsCFunc { get; }

    public LlvmFuncWrapper(LLVMTypeRef type, LLVMValueRef func, bool isCFunc)
    {
        Type = type;
        Func = func;
        IsCFunc = isCFunc;
    }

    public LlvmFuncWrapper(LLVMModuleRef mr, string funcName, LLVMTypeRef type, bool isCFunc)
    {
        Type = type;
        Func = mr.AddFunction(funcName, type);
        IsCFunc = isCFunc;
    }

    public static LLVMTypeRef CreateFuncType(
        LLVMTypeRef returnType,
        LLVMTypeRef[] paramTypes,
        bool isVarArg = false
    ) => LLVMTypeRef.CreateFunction(returnType, paramTypes, isVarArg);
}
