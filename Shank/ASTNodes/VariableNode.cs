using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Exception = System.Exception;

namespace Shank;

public class VariableNode : ASTNode
{
    public string? Name { get; set; }
    public string? ModuleName { get; set; }

    public enum DataType
    {
        Real,
        Integer,
        String,
        Character,
        Boolean,
        Array,
        Record,
        Enum,
        Reference,
        Unknown
    };

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

    public DataType Type { get; set; }

    public LLVMTypeRef GetLLVMType(Context context, DataType type) =>
        context.GetLLVMTypeFromShankType(type) ?? throw new Exception($"Type {Type} doesnt exist");

    // If Type is Array, then ArrayType is the type of its elements, or else it is null.
    public DataType? ArrayType { get; set; }

    public TypeUsage? ArrayTypeEnhanced { get; set; }

    /// <summary>
    /// If this variable was declared with an Identifier as its type_unit, then UnknownType is the
    /// Value of that Identifier.
    /// If this variable was not declared with an Identifier as its type_unit, then UnknownType
    /// should be null.
    /// </summary>
    public string? UnknownType { get; set; }

    public List<TypeUsage>? GenericTypeArgs { get; set; }

    public bool IsConstant { get; set; }
    public ASTNode? InitialValue { get; set; }
    public ASTNode? From { get; set; }
    public ASTNode? To { get; set; }

    public string GetNameSafe() =>
        Name ?? throw new InvalidOperationException("Expected Name to not be null");

    public DataType GetArrayTypeSafe()
    {
        return ArrayType
            ?? throw new InvalidOperationException("Expected ArrayType to not be null.");
    }

    public string GetUnknownTypeSafe()
    {
        return UnknownType
            ?? throw new InvalidOperationException(
                "Expected " + nameof(UnknownType) + " to not be null."
            );
    }

    public string GetModuleNameSafe() => ModuleName ?? "default";

    public string ToStringForOverloadExt() =>
        "_"
        + (IsConstant ? "" : "VAR_")
        + (Type == DataType.Unknown ? GetUnknownTypeSafe() : Type.ToString().ToUpper());

    public UnknownTypeResolver ResolveUnknownType(ModuleNode parentModule)
    {
        if (
            parentModule.getEnums().ContainsKey(GetUnknownTypeSafe())
            && parentModule.Records.ContainsKey(GetUnknownTypeSafe())
        )
        {
            return UnknownTypeResolver.Multiple;
        }

        if (parentModule.getEnums().ContainsKey(GetUnknownTypeSafe()))
        {
            return UnknownTypeResolver.Enum;
        }

        return parentModule.Records.ContainsKey(GetUnknownTypeSafe())
            ? UnknownTypeResolver.Record
            : UnknownTypeResolver.None;
    }

    /// <summary>
    /// Get this VariableNode's type based on what Extension the VariableReferenceNode that
    /// points to it might have.
    /// </summary>
    /// <param name="parentModule"></param>
    /// <param name="vrn">The VariableReferenceNode that points to this VariableNode</param>
    /// <returns></returns>
    public DataType GetSpecificType(ModuleNode parentModule, VariableReferenceNode vrn) =>
        vrn.ExtensionType switch
        {
            VrnExtType.ArrayIndex => GetArrayTypeSafe(),
            VrnExtType.RecordMember
                => GetRecordMemberType(
                    vrn.GetRecordMemberReferenceSafe().Name,
                    parentModule.Records
                ),
            VrnExtType.None
                => Type switch
                {
                    DataType.Record
                        => throw new NotImplementedException(
                            "It is not implemented yet to assign a record variable base to a target."
                        ),
                    DataType.Array
                        => throw new NotImplementedException(
                            "It is not implemented yet to assign an array variable base to a target."
                        ),
                    _ => Type
                },
            _ => throw new NotImplementedException("Unknown VrnExtType member.")
        };

    public DataType GetRecordMemberType(string memberName, Dictionary<string, RecordNode> records)
    {
        return records[GetUnknownTypeSafe()].GetFromMembersByNameSafe(memberName).Type;
    }

    public bool EqualsForOverload(VariableNode vn)
    {
        return vn.Type == Type && vn.IsConstant == IsConstant;
    }

    public override string ToString()
    {
        return Name
            + " : "
            + (Type == DataType.Array ? "Array of " + ArrayType : Type)
            + " "
            + (IsConstant ? "const" : string.Empty)
            + " "
            + (InitialValue == null ? string.Empty : InitialValue)
            + " "
            + (From == null ? string.Empty : " From: " + From)
            + " "
            + (To == null ? string.Empty : " To: " + To);
    }

    public override LLVMValueRef Visit(
        Visitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        string name = GetNameSafe();
        LLVMValueRef v = builder.BuildAlloca(
            context.GetLLVMTypeFromShankType(Type) ?? throw new Exception("null type"),
            GetNameSafe()
        );
        context.AddVaraible(GetNameSafe(), v, GetLLVMType(context, Type), false);
        return v;
    }
}
