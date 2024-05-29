using System.Collections.ObjectModel;
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

        programNode.Visit(new Context(null, new CFuntions(module)), builder, module);
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

        if (!compileOptions.CompileToObj)
        {
            if (!Directory.Exists("bin"))
                Directory.CreateDirectory("bin");
            targetMachine.TryEmitToFile(
                module,
                "bin/a.o",
                LLVMCodeGenFileType.LLVMObjectFile,
                out out_string
            );
            Process link = new Process();
            link.StartInfo.FileName = "ld";
            link.StartInfo.Arguments = $" bin/a.o -o {compileOptions.OutFile} ";
            link.Start();
            link.WaitForExit();
            File.Delete("bin/a.o");
            Directory.Delete("bin");
        }
        else
        {
            targetMachine.TryEmitToFile(
                module,
                compileOptions.OutFile,
                LLVMCodeGenFileType.LLVMObjectFile,
                out out_string
            );
        }

        if (compileOptions.Assembly)
        {
            if (!Directory.Exists("Shank-Assembly"))
                Directory.CreateDirectory("Shank-Assembly");
            targetMachine.TryEmitToFile(
                module,
                "Shank-Assembly/" + Path.ChangeExtension(compileOptions.OutFile, ".s"),
                LLVMCodeGenFileType.LLVMAssemblyFile,
                out out_string
            );
        }

        if (compileOptions.emitIR)
        {
            if (!Directory.Exists("Shank-IR"))
                Directory.CreateDirectory("Shank-IR");
            File.WriteAllText(
                "Shank-IR/" + Path.ChangeExtension(compileOptions.OutFile, ".ll"),
                module.ToString()
            );
        }

        if (compileOptions.printIR)
        {
            Console.WriteLine("IR code gen");
            module.Dump();
        }

        Console.WriteLine("");
        Console.WriteLine("code successfully compiled");
        Console.WriteLine($"executable path: {compileOptions.OutFile} ");
        if (compileOptions.emitIR)
            Console.WriteLine(
                $"LLVM-IR file path: Shank-IR/{Path.ChangeExtension(compileOptions.OutFile, ".ll")}"
            );
        if (compileOptions.Assembly)
            Console.WriteLine(
                $"Assembly file file path: Shank-Assembly/{Path.ChangeExtension(compileOptions.OutFile, ".s")}"
            );
    }
}
