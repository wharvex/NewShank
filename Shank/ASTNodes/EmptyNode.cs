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

    public override void Accept(Visitor v)
    {
        throw new NotImplementedException();
    }

    public override ASTNode? Walk(SAVisitor v)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return "Empty " + Comment;
    }
}
