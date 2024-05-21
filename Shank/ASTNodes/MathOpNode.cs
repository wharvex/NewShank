using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

public class MathOpNode : ASTNode
{
    public MathOpNode(ASTNode left, MathOpType op, ASTNode right)
    {
        Left = left;
        Op = op;
        Right = right;
    }

    public MathOpType Op { get; init; }
    public ASTNode Left { get; init; }
    public ASTNode Right { get; init; }
    

    public override string ToString()
    {
        return $"{Left.ToString()} {Op} {Right.ToString()}";
    }

    public override LLVMValueRef Visit(IVisitor visitor, Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        throw new NotImplementedException();
    }
}