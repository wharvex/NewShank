using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

public class FloatNode : ASTNode
{
    public FloatNode(float value)
    {
        Value = value;
    }

    public float Value { get; set; }

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