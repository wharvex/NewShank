using LLVMSharp.Interop;

namespace Shank.IRGenerator.CompilerPractice;

public class LlvmModWrapper
{
    public LLVMModuleRef Module { get; }

    public LlvmModWrapper()
    {
        Module = new LLVMModuleRef();
    }
}
