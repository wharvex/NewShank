using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public abstract class Visitor
{
    /// <summary>
    /// Integer nodes (ie 1,2,3,4...)
    /// </summary>
    /// <param name="node">the node</param>
    /// <param name="context">stores varaibles (unused here)</param>
    /// <param name="builder">LLVM boilerplate</param>
    /// <param name="module"></param>
    /// <returns>an LLVM value ref containing an i64 int</returns>
    /// <exception cref="Exception"></exception>
    public virtual LLVMValueRef Accept(
        IntNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }
    

    public virtual LLVMValueRef Accept(
        FloatNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        VariableReferenceNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        CharNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        BoolNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception("in Vsitor.cs");
    }

    public virtual LLVMValueRef Accept(
        StringNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        MathOpNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        BooleanExpressionNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }

    public virtual LLVMValueRef Accept(
        RecordNode node,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new Exception();
    }
}
