using LLVMSharp.Interop;
using Shank.ExprVisitors;

namespace Shank;

public class BuiltInFunctionNode : CallableNode
{
    public BuiltInFunctionNode(string name, BuiltInCall execute)
        : base(name, execute) { }

    public bool IsVariadic = false;
    public override LLVMValueRef Visit(Visitor visitor, Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        throw new NotImplementedException();
    }

    public override void Visit(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        throw new NotImplementedException();
    }
}
