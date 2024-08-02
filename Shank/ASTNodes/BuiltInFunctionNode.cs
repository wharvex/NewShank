using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public enum BuiltInFunction
{
    Write,
    Substring,
    RealToInt,
    IntToReal,
    Read,
    AllocateMem,
    FreeMem,
    High,
    Low,
    IsSet,
    Left,
    Right,
    Size,
    AssertIsEqual,
    GetRandom,
}

public class BuiltInFunctionNode(string name, CallableNode.BuiltInCall execute)
    : CallableNode(name, BuiltinModuleName, execute)
{
    // The module name used for builtin functions
    public static string BuiltinModuleName { get; } = Guid.NewGuid().ToString();

    // Copy constructor for monomorphization
    public BuiltInFunctionNode(
        BuiltInFunctionNode function,
        List<VariableDeclarationNode> parameters
    )
        : this(function.Name, function.Execute)
    {
        LineNum = function.LineNum;
        FileName = function.FileName;
        Line = function.Line;
        ParameterVariables = parameters;
        GenericTypeParameterNames = function.GenericTypeParameterNames;
    }

    public override void Accept(Visitor v) => v.Visit(this);

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        for (var index = 0; index < ParameterVariables.Count; index++)
        {
            ParameterVariables[index] = (VariableDeclarationNode)(
                ParameterVariables[index].Walk(v) ?? ParameterVariables[index]
            );
        }

        return v.PostWalk(this);
    }

    public BuiltInFunction GetBuiltIn()
    {
        Dictionary<string, BuiltInFunction> dict =
            new()
            {
                ["write"] = BuiltInFunction.Write,
                ["read"] = BuiltInFunction.Read,
                ["allocateMemory"] = BuiltInFunction.AllocateMem,
                ["freeMemory"] = BuiltInFunction.FreeMem,
                ["isSet"] = BuiltInFunction.IsSet,
                ["high"] = BuiltInFunction.High,
                ["low"] = BuiltInFunction.Low,
                ["left"] = BuiltInFunction.Left,
                ["right"] = BuiltInFunction.Right,
                ["realToInteger"] = BuiltInFunction.RealToInt,
                ["integerToReal"] = BuiltInFunction.IntToReal,
                ["substring"] = BuiltInFunction.Substring,
                ["assertIsEqual"] = BuiltInFunction.AssertIsEqual,
                ["getRandom"] = BuiltInFunction.GetRandom
            };
        return dict[Name];
    }
}

public class BuiltInVariadicFunctionNode(
    string name,
    CallableNode.BuiltInCall execute,
    bool areParametersConstant = true
) : BuiltInFunctionNode(name, execute)
{
    public bool AreParametersConstant { get; } = areParametersConstant;
}
