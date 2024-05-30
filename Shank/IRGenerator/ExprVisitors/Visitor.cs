using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public abstract class Visitor<T>
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
    public abstract T Visit(IntNode node);

    public abstract T Visit(FloatNode node);

    public abstract T Visit(VariableReferenceNode node);

    public abstract T Visit(CharNode node);

    public abstract T Visit(BoolNode node);

    public abstract T Visit(StringNode node);
    public abstract T Visit(MathOpNode node);
    public abstract T Visit(BooleanExpressionNode node);
    public abstract T Visit(RecordNode node);
    public abstract void Visit(AssignmentNode node);
}
