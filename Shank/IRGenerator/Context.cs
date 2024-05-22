using LLVMSharp.Interop;
using Shank.ExprVisitors;

namespace Shank;

/// <summary>
/// container for Varaibles (we need a type and refrence)
/// </summary>
public struct LlvmVaraible
{
    public LLVMValueRef ValueRef { get; set; }
    public LLVMTypeRef TypeRef { get; set; }

    public LlvmVaraible(LLVMValueRef valueRef, LLVMTypeRef typeRef)
    {
        ValueRef = valueRef;
        TypeRef = typeRef;
    }
}

public class Context
{
    public Context(LLVMValueRef startFunction, ModuleNode moduleNode)
    {
        CurrentFunction = startFunction;
        moduleNode = moduleNode;
    }

    public ModuleNode moduleNode { get; set; }
    public LLVMValueRef CurrentFunction { get; set; }
    public Dictionary<string, LLVMTypeRef> CustomTypes { get; } = new();
    public Dictionary<string, LLVMValueRef> Functions { get; } = new();
    public Dictionary<string, LlvmVaraible> Variables { get; set; } = new();

    // global variables, constants or variables defined at the top level
    private Dictionary<string, LlvmVaraible> GloabalVariables { get; } = new();

    /// <summary>
    /// converts shank type to LLVM type
    /// </summary>
    /// <param name="dataType"></param>
    /// <param name="unknownType"></param>
    /// <returns></returns>
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

    /// <summary>
    /// helper function. it retusns the visitor type 
    /// </summary>
    /// <param name="dataType"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Visitor GetExprFromType(LLVMTypeRef typeRef)
    {
        if (typeRef == LLVMTypeRef.Int64)
            return new IntegerExprVisitor();
        else if (typeRef == LLVMTypeRef.Int8)
            return new CharExprVisitor();
        else if (typeRef == LLVMTypeRef.Int1)
            return new BoolNodeVisitor();
        else
            throw new Exception("undefined type");
    }


    /// <summary>
    /// gets the varaible 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public LlvmVaraible GetVaraible(string name)
    {
        if (GloabalVariables.TryGetValue(name, out var GlobalVar))
        {
            return GlobalVar;
        }
        else if (Variables.TryGetValue(name, out var varaible))
        {
            return varaible;
        }
        else
        {
            throw new Exception("undefined varname");
        }
    }

    /// <summary>
    /// adds a varaivle to the dictionary
    /// </summary>
    /// <param name="name">var name (the key)</param>
    /// 
    /// <param name="valueRef">the LLVMref (needed for assignments)</param>
    /// <param name="type">Data type the varaible is</param>
    /// <param name="isGlobal">IsGlobal</param>
    /// <exception cref="Exception">if it already exists</exception>
    public void AddVaraible(string name, LLVMValueRef valueRef, LLVMTypeRef type, bool isGlobal)
    {
        if (GloabalVariables.ContainsKey(name) || Variables.ContainsKey(name))
            throw new Exception("error");
        else if (isGlobal)
            GloabalVariables.Add(name, new LlvmVaraible(valueRef, type));
        else
            Variables.Add(name, new LlvmVaraible(valueRef, type));
    }

    /// <summary>
    /// resets the Varaible dictionary run at the end of a function
    /// </summary>
    public void ResetLocal()
    {
        Variables = new();
    }
}