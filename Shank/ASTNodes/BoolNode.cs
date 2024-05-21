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

    public override string ToString()
    {
        return $"{Value}";
    }

    public override LLVMValueRef Visit(IVisitor visitor, Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        throw new NotImplementedException();
    }
}