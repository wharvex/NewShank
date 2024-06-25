using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class BuiltInFunctionNode : CallableNode
{
    public BuiltInFunctionNode(string name, BuiltInCall execute)
        : base(name, execute) { }

    // public override LLVMValueRef Visit(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     throw new NotImplementedException();
    // }

    public override void Accept(Visitor v) => throw new NotImplementedException("");
}

public class BuiltInVariadicFunctionNode(
    string name,
    CallableNode.BuiltInCall execute,
    bool areParametersConstant = true
) : BuiltInFunctionNode(name, execute)
{
    public bool AreParametersConstant { get; } = areParametersConstant;
}
