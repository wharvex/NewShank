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

    public LLVMTypeRef? GetLLVMTypeFromShankType(
        VariableNode.DataType dataType,
        string? unknownType = null
    ) =>
        dataType switch
        {
            VariableNode.DataType.Integer => LLVMTypeRef.Int64,
            VariableNode.DataType.Real => LLVMTypeRef.Double,
            VariableNode.DataType.String => LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
            VariableNode.DataType.Boolean => LLVMTypeRef.Int8,
            VariableNode.DataType.Character => LLVMTypeRef.Int1,
            // if it's a custom type we look it up in the context
            VariableNode.DataType.Reference when unknownType != null
                => LLVMTypeRef.CreatePointer(CustomTypes[unknownType], 0),
            VariableNode.DataType.Array => LLVMTypeRef.Void,
            _ when unknownType != null => CustomTypes[unknownType],
            _ => null
        };
}
