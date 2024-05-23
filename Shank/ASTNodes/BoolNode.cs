using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

public class BoolNode : ASTNode
{
    public BoolNode(bool value)
    {
        Value = value;
    }

    public bool Value { get; set; }
    public int GetValueAsInt() => Value ? 1 : 0; //Get as int (used for the "ulong" requirment)

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