using LLVMSharp.Interop;

namespace Shank;

public class Context
{
    public Context(LLVMValueRef startFunction, ModuleNode moduleNode)
    {
        CurrentFunction = startFunction;
        ModuleNode = moduleNode;
    }

    public ModuleNode ModuleNode { get; set; }
    public LLVMValueRef CurrentFunction { get; set; }
    public Dictionary<string, LLVMTypeRef> CustomTypes { get; } = new();
    public Dictionary<string, LLVMValueRef> Functions { get; } = new();
    public Dictionary<string, LLVMValueRef> Variables { get; } = new();

    // global variables, constants or variables defined at the top level
    public Dictionary<string, LLVMValueRef> GloabalVariables { get; } = new();
}
