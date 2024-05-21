using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

public class StringNode : ASTNode
{
    public StringNode(string value)
    {
        Value = value;
    }

    public string Value { get; set; }

    public override string ToString()
    {
        return $"{Value}";
    }

    public override LLVMValueRef Visit(
        IVisitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }
}
