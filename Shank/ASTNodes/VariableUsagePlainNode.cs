using Shank.ExprVisitors;

namespace Shank.ASTNodes;

///<summary>
///     Represents the root (left-most identifier) of a <see cref="VariableUsageNodeTemp"/>.
///
///     Note: This class used to be the main "VariableReferenceNode" class. As such, it still
///     contains the old way of handling array subscripts and dot-referenced record members.
///     These old properties and methods have been marked as obsolete and will/should eventually be
///     removed.
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
    public string ModuleName { get; init; }

    /// <summary>
    /// Warning: This property is going away.
    /// </summary>
    [Obsolete("Warning: This property is going away.")]
    public ExpressionNode? Extension { get; set; }

    /// <summary>
    /// Warning: This property is going away.
    /// </summary>
    [Obsolete("Warning: This property is going away.")]
    public VrnExtType ExtensionType { get; set; }

    /// <summary>
    /// Warning: This property is going away.
    /// </summary>
    [Obsolete("Warning: This property is going away.")]
    public string? EnclosingVrnName { get; set; }

    /// <summary>
    /// Warning: This property is moving. See: <see cref="VariableUsageNodeTemp.NewIsInFuncCallWithVar"/>
    /// </summary>
    [Obsolete(
        "Warning: This property is moving. See: VariableUsageNodeTemp#NewIsInFuncCallWithVar"
    )]
    public bool IsInFuncCallWithVar { get; set; }

    /// <summary>
    /// Warning: This property is moving. See: <see cref="VariableUsageNodeTemp.NewReferencesGlobalVariable"/>
    /// </summary>
    [Obsolete(
        "Warning: This property is moving. See: VariableUsageNodeTemp#NewReferencesGlobalVariable"
    )]
    public bool ReferencesGlobalVariable { get; set; }

    /// <summary>
    /// Warning: This method is moving. See: <see cref="VariableUsageNodeTemp.NewMonomorphizedName"/>
    /// </summary>
    [Obsolete("Warning: This method is moving. See: VariableUsageNodeTemp#NewMonomorphizedName")]
    public Index MonomorphizedName() =>
        ReferencesGlobalVariable
            ? new ModuleIndex(new NamedIndex(Name), ModuleName)
            : new NamedIndex(Name);

    /// <summary>
    /// Warning: This method is going away.
    /// </summary>
    [Obsolete("Warning: This method is going away.")]
    public ASTNode GetExtensionSafe() =>
        Extension ?? throw new InvalidOperationException("Expected Extension to not be null.");

    /// <summary>
    /// Warning: This method is going away.
    /// </summary>
    [Obsolete("Warning: This method is going away.")]
    public VariableUsagePlainNode GetRecordMemberReferenceSafe() =>
        GetExtensionSafe() as VariableUsagePlainNode
        ?? throw new InvalidOperationException("Expected Extension to be a VariableReferenceNode.");

    public override string ToString()
    {
        return Name;
    }

    [Obsolete("Warning: This enum is going away.")]
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
