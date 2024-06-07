using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class RecordMemberNode : StatementNode
{
    public RecordMemberNode(string name, IType newType)
    {
        Name = name;
        NewType = newType;
    }

    public string Name { get; init; }

    // public VariableNode.DataType Type { get; set; }

    // public string? UnknownType { get; init; }

    public IType NewType { get; set; }
    public ASTNode? From { get; set; }
    public ASTNode? To { get; set; }

    // public string GetUnknownTypeSafe() =>
    //  UnknownType ?? throw new InvalidOperationException("Expected UnknownType to not be null.");

    // public RecordMemberNode(string name, VariableNode.DataType type)
    // {
    //     Name = name;
    //     Type = type;
    // }

    // public RecordMemberNode(string name, string unknownType)
    // {
    //     Name = name;
    //     Type = VariableNode.DataType.Unknown;
    //     UnknownType = unknownType;
    // }

    // public RecordMemberNode(string name, string dataType, string unknownType)
    // {
    //     Name = name;
    //     Type = VariableNode.DataType.Reference;
    //     UnknownType = unknownType;
    // }

    //public VariableNode.DataType GetTypeResolveUnknown(ModuleNode module)
    //{
    //    return Type != VariableNode.DataType.Unknown
    //        ? Type
    //        : (SemanticAnalysis.GetNamespaceOfRecordsAndEnumsAndImports(module)[GetUnknownTypeSafe()] as RecordNode)?.;
    //}
    // public override void VisitStatement(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     throw new NotImplementedException();
    // }

    public override LLVMValueRef Visit(
        LLVMVisitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }

    public override T Visit<T>(ExpressionVisitor<T> visit)
    {
        throw new NotImplementedException();
    }
}