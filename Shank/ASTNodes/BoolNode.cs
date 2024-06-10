using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

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

    // public override LLVMValueRef Visit(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     return visitor.Visit(this);
    // }

    public override T Visit<T>(ExpressionVisitor<T> visit)
    {
        return visit.Accept(this);
    }
}
