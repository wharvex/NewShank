using System.Text;
using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.Interfaces;

namespace Shank.ASTNodes;

public class FunctionCallNode : StatementNode, ILlvmTranslatable
{
    public string Name { get; set; }
    public int LineNum { get; set; }
    public List<ParameterNode> Parameters { get; } = [];
    public string OverloadNameExt { get; set; } = "";
    // generics of the called function that this call site instiated to specific types
    // useful/needed for monomorphization
    public Dictionary<string, VariableNode.DataType> InstiatedGenerics { get; set; }

    public FunctionCallNode(string name)
    {
        Name = name;
    }

    public bool EqualsWrtNameAndParams(
        CallableNode givenFunction,
        Dictionary<string, VariableNode> variablesInScope
    )
    {
        // If the names don't match, it's not a match.
        if (!givenFunction.Name.Equals(Name))
        {
            return false;
        }

        // If the param counts don't match, it's not a match.
        if (givenFunction.ParameterVariables.Count != Parameters.Count)
        {
            return false;
        }

        // If there's any parameter whose type and 'var' status would disqualify the given
        // function from matching this call, return false, otherwise true.
        return !Parameters
            .Where(
                (p, i) =>
                    !p.EqualsWrtTypeAndVar(givenFunction.ParameterVariables[i], variablesInScope)
            )
            .Any();
    }

    public override object[] returnStatementTokens()
    {
        var b = new StringBuilder();
        if (Parameters.Any())
        {
            Parameters.ForEach(p => b.AppendLine($"   {p}"));
        }

        object[] arr = { "FUNCTION", Name, b.ToString() };
        return arr;
    }

    public override void VisitStatement(
        LLVMVisitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        var function =
            context.GetFunction(Name) ?? throw new Exception($"function {Name} not found");
        // if any arguement is not mutable, but is required to be mutable
        if (
            function
                .ArguementMutability.Zip(Parameters.Select(p => p.IsVariable))
                .Any(a => a is { First: true, Second: false })
        )
        {
            throw new Exception($"call to {Name} has a mismatch of mutability");
        }

        var parameters = Parameters.Select(p => p.Visit(visitor, context, builder, module));
        builder.BuildCall2(function.TypeOf, function.Function, parameters.ToArray());
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
        b.AppendLine($"Function {Name}:");
        if (Parameters.Any())
        {
            b.AppendLine("Parameters:");
            Parameters.ForEach(p => b.AppendLine($"   {p}"));
        }

        return b.ToString();
    }
}
