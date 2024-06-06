using System.Text.Json.Serialization;
using LLVMSharp.Interop;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

[JsonDerivedType(typeof(StringNode))]
[JsonDerivedType(typeof(IntNode))]
[JsonDerivedType(typeof(FloatNode))]
[JsonDerivedType(typeof(BoolNode))]
[JsonDerivedType(typeof(CharNode))]
[JsonDerivedType(typeof(VariableUsageNode))]
[JsonDerivedType(typeof(MathOpNode))]
[JsonDerivedType(typeof(BooleanExpressionNode))]
[JsonDerivedType(typeof(StatementNode))]
[JsonDerivedType(typeof(FunctionNode))]
public abstract class ASTNode
{
    public string NodeName { get; init; }
    public string InheritsDirectlyFrom { get; init; }
    public int Line { get; init; }

    public enum BooleanExpressionOpType
    {
        lt,
        le,
        gt,
        ge,
        eq,
        ne
    }

    public enum MathOpType
    {
        plus,
        minus,
        times,
        divide,
        modulo
    }

    public enum VrnExtType
    {
        RecordMember,
        ArrayIndex,
        Enum,
        None
    }

    protected ASTNode()
    {
        NodeName = GetType().Name;
        InheritsDirectlyFrom = GetType().BaseType?.Name ?? "None";
        Line = Parser.Line;
    }

    // public abstract LLVMValueRef Accept(LLVMBuilderRef builder, LLVMModuleRef module);
    public abstract LLVMValueRef Visit(
        Visitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    );
}
