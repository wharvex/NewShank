using System.Diagnostics;
using System.IO;
using LLVMSharp.Interop;
using Shank.ExprVisitors;

namespace Shank;

public class LLVMCodeGen
{
    public LLVMModuleRef ModuleRef;

    public void CodeGen(CompileOptions compileOptions, ProgramNode programNode)
    {
        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
        LLVM.InitializeAllAsmPrinters();
        LLVM.InitializeAllAsmParsers();
        var module = LLVMModuleRef.CreateWithName("main");

        LLVMBuilderRef builder = module.Context.CreateBuilder();
        FileStream fs;

        // string? directory = Path.GetDirectoryName(compileOptions.OutFile);

        var context = new Context(null, new CFuntions(module));
        programNode.VisitProgram(new LLVMVisitor(context, builder, module), context, builder, module);
        //outputting directly to an object file
        //https://llvm.org/docs/tutorial/MyFirstLanguageFrontend/LangImpl08.html
        var targetTriple = LLVMTargetRef.DefaultTriple;
        var target = LLVMTargetRef.GetTargetFromTriple(targetTriple);
        var cpu = "generic";
        var features = "";
        var opt = compileOptions.OptLevel;
        var targetMachine = target.CreateTargetMachine(
            targetTriple,
            cpu,
            features,
            opt,
            LLVMRelocMode.LLVMRelocPIC,
            LLVMCodeModel.LLVMCodeModelMedium
        );

        //use the clang drivers to get something like {"-O3 emit-llvm "}
        var out_string = "";
        if (compileOptions.CompileToObj)
            targetMachine.TryEmitToFile(
                module,
                compileOptions.OutFile,
                LLVMCodeGenFileType.LLVMObjectFile,
                out out_string
            );
        else
            File.WriteAllText(compileOptions.OutFile, module.ToString());
        Console.WriteLine("code successfully compiled");
        Console.WriteLine($"Code gen path {compileOptions.OutFile} ");
        Console.WriteLine("IR result");
        module.Dump();
    }
}
