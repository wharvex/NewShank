using LLVMSharp;
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
        Visitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        return visitor.Accept(this, context, builder, module);
    }
}
