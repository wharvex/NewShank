using LLVMSharp.Interop;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class IntNode : ASTNode
{
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

    // public LLVMValueRef Accept(LLVMBuilderRef builder, LLVMModuleRef module)
    // {
    //     // value requires a ulong cast, because that is what CreateConstInt requires
    //     return LLVMValueRef.CreateConstInt(module.Context.Int64Type, (ulong)Value);
    // }
    //
    // public override LLVMValueRef Visit()
    // {
    //     throw new NotImplementedException();
    // }
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
