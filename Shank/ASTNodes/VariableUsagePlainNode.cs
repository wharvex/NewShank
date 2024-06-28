using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class VariableUsagePlainNode : VariableUsageNodeTemp
{
    public VariableUsagePlainNode(string name, string moduleName)
    {
        Name = name;
        Extension = null;
        ExtensionType = VrnExtType.None;
        ModuleName = moduleName;
    }

    public VariableUsagePlainNode(string name, ExpressionNode extension, VrnExtType extensionType, string moduleName)
    {
        Name = name;
        Extension = extension;
        ExtensionType = extensionType;
        if (extensionType == VrnExtType.RecordMember)
        {
            ((VariableUsagePlainNode)Extension).EnclosingVrnName = Name;
        }
        ModuleName = moduleName;
    }

    public string Name { get; init; }
    public string ModuleName { get; init; }

    /// <summary>
    /// Represents an extension of the base variable reference (e.g. an array subscript or a record member).
    /// </summary>
    public ExpressionNode? Extension { get; init; }

    public VrnExtType ExtensionType { get; set; }

    public string? EnclosingVrnName { get; set; }

    public bool IsVariableFunctionCall { get; set; }

    public ASTNode GetExtensionSafe() =>
        Extension ?? throw new InvalidOperationException("Expected Extension to not be null.");

    public VariableUsagePlainNode GetRecordMemberReferenceSafe() =>
        GetExtensionSafe() as VariableUsagePlainNode
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
            if (ext is VariableUsagePlainNode vrn)
            {
                ret.Add(vrn.Name);
                ext = vrn.Extension;
                extType = vrn.ExtensionType;
            }
            else
            {
                throw new InvalidOperationException(
                    "Expected " + ext + " to be a VariableReferenceNode."
                );
            }
        }

        return ret;
    }

    // asumptions already analized
    public Type GetSpecificType(
        Dictionary<string, RecordNode> records,
        Dictionary<string, ASTNode> imports,
        Dictionary<string, VariableDeclarationNode> variables,
        string name
    )
    {
        var recordsAndImports = SemanticAnalysis.GetRecordsAndImports(records, imports);
        return variables[name].Type switch
        {
            RecordType r => r,
            ArrayType a => a.Inner,
            _ => variables[name].Type
        };
    }

    public override string ToString()
    {
        return $"{Name + (Extension != null ? (", Index: " + Extension) : string.Empty)}";
    }

    // public override LLVMValueRef Visit(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     return visitor.Visit(this);
    // }


    public enum VrnExtType
    {
        RecordMember,
        ArrayIndex,
        Enum,
        None
    }

    public override T Accept<T>(ExpressionVisitor<T> visit) => visit.Visit(this);

    public override void Accept(Visitor v) => v.Visit(this);
}
