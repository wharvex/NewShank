using System.Text;
using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.IRGenerator;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;
using Exception = System.Exception;

namespace Shank.ASTNodes;

public class VariableNode : StatementNode
{
    public string? Name { get; set; }
    public string? ModuleName { get; set; }

    // public enum DataType
    // {
    //     Real,
    //     Integer,
    //     String,
    //     Character,
    //     Boolean,
    //     Array,
    //     Record,
    //     Enum,
    //     Reference,
    //     Unknown
    // };
    //
    public enum UnknownTypeResolver
    {
        Record,
        Enum,
        None,
        Multiple
    };

    public enum DeclarationContext
    {
        RecordDeclaration,
        EnumDeclaration,
        FunctionSignature,
        VariablesLine,
        ConstantsLine
    };

    // public DataType Type { get; set; }
    public Type Type { get; set; }

    public LLVMTypeRef GetLLVMType(Context context, Type type) =>
        context.GetLLVMTypeFromShankType(type, false)
        ?? throw new Exception($"Type {type} doesnt exist");

    // If Type is Array, then ArrayType is the type of its elements, or else it is null.
    // public DataType? ArrayType { get; set; }

    // public TypeUsage? ArrayTypeEnhanced { get; set; }

    /// <summary>
    /// If this variable was declared with an Identifier as its type_unit, then UnknownType is the
    /// Value of that Identifier.
    /// If this variable was not declared with an Identifier as its type_unit, then UnknownType
    /// should be null.
    /// </summary>
    // public string? UnknownType { get; set; }

    // public List<TypeUsage>? GenericTypeArgs { get; set; }

    public bool IsConstant { get; set; }
    public ASTNode? InitialValue { get; set; }

    // public ASTNode? From { get; set; }
    // public ASTNode? To { get; set; }

    public string GetNameSafe() =>
        Name ?? throw new InvalidOperationException("Expected Name to not be null");

    // public DataType GetArrayTypeSafe()
    // {
    //     return ArrayType
    //         ?? throw new InvalidOperationException("Expected ArrayType to not be null.");
    // }

    // public string GetUnknownTypeSafe()
    // {
    //     return UnknownType
    //         ?? throw new InvalidOperationException(
    //             "Expected " + nameof(UnknownType) + " to not be null."
    //         );
    // }

    public string GetModuleNameSafe() => ModuleName ?? "default";

    public string ToStringForOverloadExt() => "_" + (IsConstant ? "" : "VAR_") + Type;

    // TODO implemnted in unkown type


    /// <summary>
    /// Get this VariableNode's type based on what Extension the VariableReferenceNode that
    /// points to it might have.
    /// </summary>
    /// <param name="parentModule"></param>
    /// <param name="vrn">The VariableReferenceNode that points to this VariableNode</param>
    /// <returns></returns>
    public Type GetSpecificType(ModuleNode parentModule, VariableUsageNode vrn) =>
        vrn.ExtensionType switch
        {
            // TODO: make exttype more expressive/type constrained
            VariableUsageNode.VrnExtType.ArrayIndex
                => ((ArrayType)Type).Inner,
            VariableUsageNode.VrnExtType.RecordMember
                => ((RecordType)Type).Fields[vrn.GetRecordMemberReferenceSafe().Name],
            VariableUsageNode.VrnExtType.None
                => Type switch
                {
                    RecordType
                        => throw new NotImplementedException(
                            "It is not implemented yet to assign a record variable base to a target."
                        ),
                    ArrayType
                        => throw new NotImplementedException(
                            "It is not implemented yet to assign an array variable base to a target."
                        ),
                    _ => Type
                },
            _ => throw new NotImplementedException("Unknown VrnExtType member.")
        };

    // public DataType GetRecordMemberType(string memberName, Dictionary<string, RecordNode> records)
    // {
    //    return records[GetUnknownTypeSafe()].GetFromMembersByNameSafe(memberName).Type;
    // }

    public bool EqualsForOverload(VariableNode vn)
    {
        return vn.Type.Equals(Type) && vn.IsConstant == IsConstant;
    }

    public override string ToString()
    {
        var b = new StringBuilder();
        b.Append($"{Name} declared as {Type}");
        b.Append(IsConstant ? " const" : "");
        b.Append(InitialValue is not null ? $" init {InitialValue}" : "");
        return b.ToString();
    }

    // public override LLVMValueRef Visit(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     var name = GetNameSafe();
    //     // TODO: only alloca when !isConstant
    //
    //     LLVMValueRef v = builder.BuildAlloca(
    //         // isVar is false, because we are already creating it using alloca which makes it var
    //         context.GetLLVMTypeFromShankType(Type) ?? throw new Exception("null type"),
    //         name
    //     );
    //     var variable = context.NewVariable(Type);
    //     context.AddVariable(name, variable(v, !IsConstant), false);
    //     return v;
    // }

    public void VisitProto(VisitPrototype visitPrototype)
    {
        visitPrototype.Accept(this);
    }

    public override void Visit(StatementVisitor visit)
    {
        visit.Accept(this);
    }

    public override void Accept(Visitor v) => v.Visit(this);

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
