using LLVMSharp.Interop;
using Shank.IRGenerator;
using System.Diagnostics;
using LLVMSharp.Interop;

namespace Shank;

public class LLVMCodeGen
{
    public LLVMModuleRef ModuleRef;

    // TODO: https://upload.wikimedia.org/wikipedia/commons/1/1b/Portable_Executable_32_bit_Structure_in_SVG_fixed.svg
    // this shows the format of a DOS PE. its good not to rely on CLANG forever with this if anyone is brave enough
    // to follow this.
    public void CodeGen(CompileOptions compileOptions, MonomorphizedProgramNode programNode)
    {
        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
        LLVM.InitializeAllAsmPrinters();
        LLVM.InitializeAllAsmParsers();
        var module = LLVMModuleRef.CreateWithName(
            Path.ChangeExtension(compileOptions.OutFile, ".ll")
        );
        LLVMBuilderRef builder = module.Context.CreateBuilder();
        FileStream fs;

        // string? directory = Path.GetDirectoryName(compileOptions.OutFile);

        var context = new Context(
            programNode,
            new CFuntions(module),
            new RandomInformation(module)
        );
        var compiler = new Compiler(context, builder, module, compileOptions);
        compiler.Compile(programNode);

        //outputting directly to an object file
        //https://llvm.org/docs/tutorial/MyFirstLanguageFrontend/LangImpl08.html
        var targetTriple = LLVMTargetRef.DefaultTriple;
        var target = LLVMTargetRef.GetTargetFromTriple(targetTriple);
        var cpu = compileOptions.TargetCPU;
        var features = "";
        var opt = compileOptions.OptLevel switch
        {
            OptPasses.Level0 => LLVMCodeGenOptLevel.LLVMCodeGenLevelNone,
            OptPasses.Level1 => LLVMCodeGenOptLevel.LLVMCodeGenLevelLess,
            OptPasses.Level2 => LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
            OptPasses.Level3 => LLVMCodeGenOptLevel.LLVMCodeGenLevelAggressive,
            _ => throw new Exception("eerror")
        };
        var targetMachine = target.CreateTargetMachine(
            targetTriple,
            cpu,
            features,
            opt,
            LLVMRelocMode.LLVMRelocPIC,
            LLVMCodeModel.LLVMCodeModelLarge
        );
        var out_string = "";
        if (!compileOptions.CompileOff)
        {
            if (!compileOptions.CompileToObj)
            {
                if (!Directory.Exists("Shank-bin"))
                    Directory.CreateDirectory("Shank-bin");
                targetMachine.TryEmitToFile(
                    module,
                    "Shank-bin/a.o",
                    //required because it needs to invoke the linker
                    LLVMCodeGenFileType.LLVMObjectFile,
                    out out_string
                );
                StringBuilder LinkerArgs = new StringBuilder();
                compileOptions
                    .LinkedFiles.ToList()
                    .ForEach(n =>
                    {
                        LinkerArgs.Append($"-l{n} ");
                    });
                Process link = new Process();
                link.StartInfo.FileName = compileOptions.LinkerOption;
                link.StartInfo.Arguments =
                    $" Shank-bin/a.o -L {compileOptions.LinkedPath} {LinkerArgs.ToString()} -o {compileOptions.OutFile} ";
                link.Start();
                link.WaitForExit();
                File.Delete("Shank-bin/a.o");
                Directory.Delete("Shank-bin");
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
        if (!compileOptions.CompileOff)
            if (compileOptions.CompileToObj)
                Console.WriteLine($"Object output path: {compileOptions.OutFile} ");
            else
                Console.WriteLine($"executable output path: {compileOptions.OutFile} ");
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
