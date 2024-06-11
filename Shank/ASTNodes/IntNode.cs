using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

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

    public override T Visit<T>(ExpressionVisitor<T> visit)
    {
        return visit.Accept(this);
    }

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
