using System.Text.Json.Serialization;
using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

[JsonDerivedType(typeof(FunctionNode))]
[JsonDerivedType(typeof(BuiltInFunctionNode))]
public abstract class CallableNode : ASTNode
{
    public string Name { get; set; }

    public string? parentModuleName { get; set; }

    public bool IsPublic { get; set; }

    public int LineNum { get; set; }

    public List<VariableNode> ParameterVariables { get; } = [];

    protected CallableNode(string name)
    {
        Name = name;
        IsPublic = false;
    }

    protected CallableNode(string name, string moduleName)
    {
        Name = name;
        parentModuleName = moduleName;
        IsPublic = false;
    }

    protected CallableNode(string name, BuiltInCall execute)
    {
        Name = name;
        Execute = execute;
        IsPublic = false;
    }

    protected CallableNode(string name, string moduleName, bool isPublicIn)
    {
        Name = name;
        parentModuleName = moduleName;
        IsPublic = isPublicIn;
    }

    public delegate void BuiltInCall(List<InterpreterDataType> parameters);

    public BuiltInCall? Execute;

    public bool IsValidOverloadOf(CallableNode cn) =>
        ParameterVariables.Where((pv, i) => !cn.ParameterVariables[i].EqualsForOverload(pv)).Any();

    public string GetNameForLlvm() =>
        Name switch
        {
            "write" => "printf",
            "start" => "main",
            _ => Name
        };

    public abstract override LLVMValueRef Visit(
        Visitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    );
}
