using System.Text;
using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class FunctionCallNode : StatementNode
{
    public string Name { get; set; }

    // the value you need to lookup a function after monomophization
    public Index MonomphorizedFunctionLocater { get; init; }

    // Use BuiltinFuctionNode.BuiltinModuleName to get module name for builtin functions
    public string FunctionDefinitionModule { get; set; }
    public int LineNum { get; set; }
    public List<ExpressionNode> Arguments { get; } = [];
    public string OverloadNameExt { get; set; } = "";
    public TypeIndex Overload { get; set; }

    // generics of the called function that this call site instantiated to specific types
    // useful/needed for monomorphization
    public Dictionary<string, Type> InstantiatedGenerics { get; set; } = [];

    // if this is not null this must be calling variadic function
    public List<Type>? InstantiatedVariadics { get; set; }

    public FunctionCallNode(string name)
    {
        Name = name;
    }

    // Copy constructor for monomorphization
    public FunctionCallNode(FunctionCallNode copy, TypedModuleIndex moduleIndex)
    {
        MonomphorizedFunctionLocater = moduleIndex;
        FileName = copy.FileName;
        Line = copy.Line;
        InstantiatedGenerics = copy.InstantiatedGenerics;
        InstantiatedVariadics = copy.InstantiatedVariadics;
        FunctionDefinitionModule = copy.FunctionDefinitionModule;
        Name = copy.Name;
        OverloadNameExt = copy.OverloadNameExt;
        Arguments = copy.Arguments;
        LineNum = copy.LineNum;
    }

    // Copy constructor for monomorphization (we need separate one for variadic function calls)
    public FunctionCallNode(FunctionCallNode copy, TypedBuiltinIndex builtinIndex)
    {
        MonomphorizedFunctionLocater = builtinIndex;
        FileName = copy.FileName;
        Line = copy.Line;
        InstantiatedGenerics = copy.InstantiatedGenerics;
        FunctionDefinitionModule = copy.FunctionDefinitionModule;
        Name = copy.Name;
        OverloadNameExt = copy.OverloadNameExt;
        Arguments = copy.Arguments;
        LineNum = copy.LineNum;
        InstantiatedVariadics = copy.InstantiatedVariadics;
    }

    public bool EqualsWrtNameAndParams(
        CallableNode givenFunction,
        Dictionary<string, VariableDeclarationNode> variablesInScope
    )
    {
        // If the names don't match, it's not a match.
        if (!givenFunction.Name.Equals(Name))
        {
            return false;
        }

        if (givenFunction.ParameterVariables.Count != Arguments.Count)
        {
            return false;
        }

        for (int i = 0; i < Arguments.Count(); i++)
        {
            //Checks if it is a variable or constant
            if (
                (Arguments[i] is VariableUsageNodeTemp)
                != givenFunction.ParameterVariables[i].IsConstant
            )
                ;
        }

        return true;
    }

    public override object[] returnStatementTokens()
    {
        var b = new StringBuilder();

        if (Arguments.Any())
        {
            Arguments.ForEach(p => b.AppendLine($"   {p}"));
        }

        if (Arguments.Any())
        {
            Arguments.ForEach(p => b.AppendLine($"   {p}"));
        }

        object[] arr = { "FUNCTION", Name, b.ToString() };
        return arr;
    }

    public string GetNameForLlvm() =>
        Name switch
        {
            "write" => "printf",
            "start" => "main",
            _ => Name
        };

    public override string ToString()
    {
        var b = new StringBuilder();

        b.Append($"Call to function `{Name}'");
        if (Arguments.Count <= 0)
        {
            return b.ToString();
        }
        b.Append(" with arguments [ ");
        Arguments.ForEach(p => b.Append($"{p} "));
        b.Append(']');

        return b.ToString();
    }

    public override void Accept(Visitor v) => v.Visit(this);

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        for (var index = 0; index < Arguments.Count; index++)
        {
            Arguments[index] = (ExpressionNode)(Arguments[index].Walk(v) ?? Arguments[index]);
        }

        return v.PostWalk(this);
    }
}
