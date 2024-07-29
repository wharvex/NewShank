using Shank.ExprVisitors;

namespace Shank.ASTNodes;

///<summary>
///     Represents a plain variable usage node in the abstract syntax tree (AST).
///</summary>

public class VariableUsagePlainNode : VariableUsageNodeTemp
{
    ///<summary>
    ///     Initializes a new instance of the <see cref="VariableUsagePlainNode"/> class with the specified name and module name.
    ///</summary>
    ///<param name="name">The name of the variable.</param>
    ///<param name="moduleName">The name of the module where the variable is defined.</param>

    public VariableUsagePlainNode(string name, string moduleName)
    {
        Name = name;
        Extension = null;
        ExtensionType = VrnExtType.None;
        ModuleName = moduleName;
    }

    ///<summary>
    ///     Initializes a new instance of the <see cref="VariableUsagePlainNode"/> class with the specified name, extension, extension type, and module name.
    ///</summary>
    ///<param name="name">The name of the variable.</param>
    ///<param name="extension">The extension of the variable (e.g., an array subscript or a record member).</param>
    ///<param name="extensionType">The type of the extension.</param>
    ///<param name="moduleName">The name of the module where the variable is defined.</param>

    public VariableUsagePlainNode(
        string name,
        ExpressionNode extension,
        VrnExtType extensionType,
        string moduleName
    )
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

    ///<summary>
    ///     Gets the name of the variable.
    ///</summary>

    public string Name { get; init; }

    ///<summary>
    ///     Gets the name of the module where the variable is defined.
    ///</summary>
    ///
    public string ModuleName { get; init; }

    /// <summary>
    /// Represents an extension of the base variable reference (e.g. an array subscript or a record member).
    /// </summary>
    public ExpressionNode? Extension { get; set; }

    ///<summary>
    ///     Gets or sets the type of the extension.
    ///</summary>

    public VrnExtType ExtensionType { get; set; }

    ///<summary>
    ///     Gets or sets the name of the enclosing variable reference node if this node represents a record member.
    ///</summary>

    public string? EnclosingVrnName { get; set; }

    ///<summary>
    ///     Gets or sets a value indicating whether the variable usage represents a function call.
    ///</summary>

    public bool IsVariableFunctionCall { get; set; }

    ///<summary>
    ///     Gets or sets a value indicating whether the variable usage references a global variable.
    ///</summary>

    public bool ReferencesGlobalVariable { get; set; }

    ///<summary>
    ///     Returns the monomorphized name of the variable, considering whether it references a global variable.
    ///</summary>
    ///<returns>
    ///     An <see cref="Index"/> representing the monomorphized name of the variable.
    ///</returns>

    public Index MonomorphizedName() =>
        ReferencesGlobalVariable
            ? new ModuleIndex(new NamedIndex(Name), ModuleName)
            : new NamedIndex(Name);

    ///<summary>
    ///     Safely gets the extension node.
    ///</summary>
    ///<returns>
    ///     The <see cref="ASTNode"/> representing the extension.
    ///     Throws an <see cref="InvalidOperationException"/> if the extension is <c>null</c>.
    ///</returns>
    ///
    public ASTNode GetExtensionSafe() =>
        Extension ?? throw new InvalidOperationException("Expected Extension to not be null.");

    ///<summary>
    ///     Safely gets the extension node as a <see cref="VariableUsagePlainNode"/>.
    ///</summary>
    ///<returns>
    ///     The <see cref="VariableUsagePlainNode"/> representing the extension.
    ///     Throws an <see cref="InvalidOperationException"/> if the extension is not a <see cref="VariableUsagePlainNode"/>.
    ///</returns>
    public VariableUsagePlainNode GetRecordMemberReferenceSafe() =>
        GetExtensionSafe() as VariableUsagePlainNode
        ?? throw new InvalidOperationException("Expected Extension to be a VariableReferenceNode.");

    ///<summary>
    ///     Gets the nested names as a list of strings.
    ///</summary>
    ///<returns>
    ///     A list of strings representing the nested names.
    ///     Throws an <see cref="InvalidOperationException"/> if the extension type is not <see cref="VrnExtType.RecordMember"/> or if the variable reference node is enclosed.
    ///</returns>
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

    ///<summary>
    ///     Gets the nested names as a list of strings.
    ///</summary>
    ///<returns>
    ///     A list of strings representing the nested names.
    ///     Throws an <see cref="InvalidOperationException"/> if the extension type is not <see cref="VrnExtType.RecordMember"/> or if the variable reference node is enclosed.
    ///</returns>
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

    ///<summary>
    ///     Gets the nested names as a list of strings.
    ///</summary>
    ///<returns>
    ///     A list of strings representing the nested names.
    ///     Throws an <see cref="InvalidOperationException"/> if the extension type is not <see cref="VrnExtType.RecordMember"/> or if the variable reference node is enclosed.
    ///</returns>
    public override string ToString()
    {
        return $"{Name + (Extension != null ? (", Index: " + Extension) : string.Empty)}";
    }


    public enum VrnExtType
    {
        RecordMember,
        ArrayIndex,
        Enum,
        None
    }

    ///<summary>
    ///     Accepts a generic visitor for processing this node.
    ///</summary>
    ///<param name="v">The generic visitor to accept.</param>
    public override void Accept(Visitor v) => v.Visit(this);

    ///<summary>
    ///     Walks the node with a semantic analysis visitor
    ///</summary>
    ///<param name="v">The semantic analysis visitor that processes the node.</param>
    ///<returns>
    ///     <para>The resulting AST node if changes are made.</para>
    ///     <para>Returns <c>null</c> if no changes are made.</para>
    ///</returns>
    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        if (Extension != null)
            Extension = (ExpressionNode)(Extension.Walk(v) ?? Extension);

        return v.PostWalk(this);
    }
}
