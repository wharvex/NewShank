using LLVMSharp.Interop;

namespace Shank;

public class LLVMCodeGen
{
    public LLVMModuleRef ModuleRef;
    public void CodeGen(string fileDir)
    {
        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
        LLVM.InitializeAllAsmPrinters();
        LLVM.InitializeAllAsmParsers();
        var module = LLVMModuleRef.CreateWithName("main");
        
        LLVMBuilderRef builder = module.Context.CreateBuilder();

    }
}