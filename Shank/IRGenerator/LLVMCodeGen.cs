using System.IO;
using LLVMSharp.Interop;

namespace Shank;

public class LLVMCodeGen
{
    public LLVMModuleRef ModuleRef;

    public void CodeGen(string fileDir, ProgramNode programNode)
    {
        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
        LLVM.InitializeAllAsmPrinters();
        LLVM.InitializeAllAsmParsers();
        var module = LLVMModuleRef.CreateWithName("main");

        LLVMBuilderRef builder = module.Context.CreateBuilder();
        FileStream fs;

        string directory = Path.GetDirectoryName(fileDir);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory); //l
        }
        File.WriteAllText(fileDir, module.ToString());
        Console.WriteLine("code successfully compiled");
        Console.WriteLine("IR code gen file path: " + fileDir);
    }
}
