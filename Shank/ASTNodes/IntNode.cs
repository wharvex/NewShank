using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class IntNode : ExpressionNode
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

    // public override LLVMValueRef Visit(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     // value requires a ulong cast, because that is what CreateConstInt requires
    //     return visitor.Visit(this);
    // }

    public override void Accept(Visitor v) => v.Visit(this);

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;
        return v.PostWalk(this);
    }
}
