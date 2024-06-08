using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class VariableUsageNode : ASTNode
{
    public VariableUsageNode(string name)
    {
        Name = name;
        Extension = null;
        ExtensionType = VrnExtType.None;
    }

    public VariableUsageNode(string name, ASTNode extension, VrnExtType extensionType)
    {
        Name = name;
        Extension = extension;
        ExtensionType = extensionType;
        if (extensionType == VrnExtType.RecordMember)
        {
            ((VariableUsageNode)Extension).EnclosingVrnName = Name;
        }
    }

    public string Name { get; init; }

    /// <summary>
    /// Represents an extension of the base variable reference (e.g. an array subscript or a record member).
    /// </summary>
    public ASTNode? Extension { get; init; }

    public VrnExtType ExtensionType { get; set; }

    public string? EnclosingVrnName { get; set; }

    public ASTNode GetExtensionSafe() =>
        Extension ?? throw new InvalidOperationException("Expected Extension to not be null.");

    public VariableUsageNode GetRecordMemberReferenceSafe() =>
        GetExtensionSafe() as VariableUsageNode
        ?? throw new InvalidOperationException("Expected Extension to be a VariableReferenceNode.");

    public List<string> GetNestedNamesAsList()
    {
        List<string> ret = [Name];
        if (ExtensionType != VrnExtType.RecordMember)
        {
            return ret;
            //throw new InvalidOperationException(
            //    "Don't call this method on a VRN whose ExtensionType is anything other than "
            //        + "RecordMember."
            //);
        }

        if (EnclosingVrnName is not null)
        {
            return ret;
            //throw new InvalidOperationException("Don't call this method on an enclosed VRN.");
        }

        var ext = Extension;
        var extType = ExtensionType;
        while (extType == VrnExtType.RecordMember && ext is not null)
        {
            if (ext is VariableUsageNode vrn)
            {
                ret.Add(vrn.Name);
                ext = vrn.Extension;
                extType = vrn.ExtensionType;
            }
            else
            {
                throw new InvalidOperationException(
                    "Expected " + ext.NodeName + " to be a VariableReferenceNode."
                );
            }
        }

        return ret;
    }

    public VariableNode.DataType GetSpecificType(
        Dictionary<string, RecordNode> records,
        Dictionary<string, ASTNode> imports,
        Dictionary<string, VariableNode> variables,
        string name
    )
    {
        var recordsAndImports = SemanticAnalysis.GetRecordsAndImports(records, imports);
        return variables[name].Type switch
        {
            VariableNode.DataType.Record
                => ((RecordNode)recordsAndImports[name])
                    .GetFromMembersByNameSafe(GetRecordMemberReferenceSafe().Name)
                    .Type,
            VariableNode.DataType.Array => variables[name].GetArrayTypeSafe(),
            _ => variables[name].Type
        };
    }

    public override string ToString()
    {
        return $"{Name + (Extension != null ? (", Index: " + Extension) : string.Empty)}";
    }

    public override LLVMValueRef Visit(
        LLVMVisitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        return visitor.Visit(this);
    }

    public override T Visit<T>(ExpressionVisitor<T> visit)
    {
        return visit.Accept(this);
    }

    public enum VrnExtType
    {
        RecordMember,
        ArrayIndex,
        Enum,
        None
    }
}
