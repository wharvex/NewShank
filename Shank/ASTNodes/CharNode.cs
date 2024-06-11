using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.IRGenerator;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class CharNode : ASTNode
{
    public CharNode(char value)
    {
        Value = value;
    }

    public char Value { get; set; }

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
    //     // characters are equivalant to an unsigned 8 bit integer
    //     return LLVMValueRef.CreateConstInt(module.Context.Int8Type, Value, true);
    // }

    public override T Visit<T>(ExpressionVisitor<T> visit)
    {
        return visit.Accept(this);
    }

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
