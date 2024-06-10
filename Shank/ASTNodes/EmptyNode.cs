using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class EmptyNode(string comment) : ASTNode
{
    public string Comment { get; set; } = comment;

    // public override LLVMValueRef Visit(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     throw new NotImplementedException();
    // }

    public override T Visit<T>(ExpressionVisitor<T> visit)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return "Empty " + Comment;
    }
}
