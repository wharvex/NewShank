using System.IO;
using LLVMSharp.Interop;
using Shank.ExprVisitors;

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
        programNode.Visit(new Context(null), builder, module);

        //outputting directly to an object file
        //https://llvm.org/docs/tutorial/MyFirstLanguageFrontend/LangImpl08.html
        var targetTriple = LLVMTargetRef.DefaultTriple;
        var target = LLVMTargetRef.GetTargetFromTriple(targetTriple);
        var cpu = "generic";
        var features = "";
        var opt = LLVMCodeGenOptLevel.LLVMCodeGenLevelNone;
        var targetMachine = target.CreateTargetMachine(
            targetTriple,
            cpu,
            features,
            opt,
            LLVMRelocMode.LLVMRelocPIC,
            LLVMCodeModel.LLVMCodeModelMedium
        );
        var out_string = "";
        targetMachine.TryEmitToFile(
            module,
            "a.out",
            LLVMCodeGenFileType.LLVMObjectFile,
            out out_string
        );

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory); //l
        }

        File.WriteAllText(fileDir, module.ToString());
        Console.WriteLine("code successfully compiled");
        Console.WriteLine($"IR code gen path {fileDir} ");
        Console.WriteLine($"Object file path {fileDir} ");
        // Console.WriteLine("IR result");
        Console.WriteLine($"{module.ToString()}");
    }
}
