using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class FloatNode : ExpressionNode
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

    // public override LLVMValueRef Visit(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     return visitor.Visit(this);
    // }

    public override T Accept<T>(ExpressionVisitor<T> visit) => visit.Visit(this);

    public override void Accept(Visitor v) => v.Visit(this);
}
