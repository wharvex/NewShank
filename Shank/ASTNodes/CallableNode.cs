namespace Shank.ASTNodes;

public abstract class CallableNode(
    string name,
    string moduleName,
    CallableNode.BuiltInCall? execute = null,
    bool isPublicIn = false
) : ASTNode
{
    public string Name { get; set; } = name;

    public string parentModuleName { get; set; } = moduleName;
    public Index MonomorphizedName { get; set; }

    public TypeIndex Overload { get; private set; } = new([]);
    public List<string> GenericTypeParameterNames { get; set; } = [];

    public void SetOverload() =>
        Overload = new TypeIndex(ParameterVariables.Select(p => p.Type).ToList());

    // TODO: This property is only set, it's never actually used. What should it be used for?
    // I think it indicates that this function was exported.
    public bool IsPublic { get; set; } = isPublicIn;

    // TODO: Get rid of this since we have ASTNode.Line.
    public int LineNum { get; set; }

    public List<VariableDeclarationNode> ParameterVariables { get; set; } = [];

    public delegate void BuiltInCall(List<InterpreterDataType> parameters);

    // It is a little confusing that the type of this field is called "BuiltInCall", because this
    // field is also used to execute non-builtin functions.
    public BuiltInCall? Execute = execute;

    public bool ShouldSerializeExecute()
    {
        return false;
    }

    public bool IsValidOverloadOf(CallableNode cn) =>
        ParameterVariables.Where((pv, i) => !cn.ParameterVariables[i].EqualsForOverload(pv)).Any();

    public override string ToString()
    {
        return Name;
    }
}
