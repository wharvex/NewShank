using System.Text.Json.Serialization;
using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.Interfaces;

namespace Shank;

[JsonDerivedType(typeof(FunctionNode))]
[JsonDerivedType(typeof(BuiltInFunctionNode))]
public abstract class CallableNode : ASTNode, ILlvmTranslatable
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

    public abstract override LLVMValueRef Visit(
        LLVMVisitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    );

    public override T Visit<T>(ExpressionVisitor<T> visit)
    {
        throw new NotImplementedException();
    }

    public virtual void Visit(StatementVisitor visit) { }

    public override string ToString()
    {
        return Name;
    }
}
