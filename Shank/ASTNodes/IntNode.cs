using LLVMSharp.Interop;

namespace Shank.ASTNodes;

public class IntNode : ASTNode
{
    public override LLVMValueRef Accept(LLVMBuilderRef builder, LLVMModuleRef module)
    {
        // value requires a ulong cast, because that is what CreateConstInt requires
        return LLVMValueRef.CreateConstInt(module.Context.Int64Type, (ulong)Value);
    }

    // TODO: change to a long, if we want 64 bit integers by default
    public IntNode(int value)
    {
        Value = value;
    }

    public int Value { get; set; }

    public override string ToString()
    {
        return $"{Value}";
    }
}
