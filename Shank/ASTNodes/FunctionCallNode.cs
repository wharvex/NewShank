using System.Text;
using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class FunctionCallNode : StatementNode
{
    public string Name { get; set; }
    // If its null then we have a call to a builtin
    public string? FunctionDefinitionModule { get; set; }
    public int LineNum { get; set; }
    public List<ExpressionNode> Arguments { get; } = [];
    public string OverloadNameExt { get; set; } = "";

    // generics of the called function that this call site instantiated to specific types
    // useful/needed for monomorphization
    public Dictionary<string, Type> InstantiatedGenerics { get; set; } = [];
    // if this is not null this must be calling variadic function
    public List<Type>? InstantiatedVariadics { get; set; } = null;

    public FunctionCallNode(string name)
    {
        Name = name;
    }

    // Copy constructor for monomorphization
    public FunctionCallNode(FunctionCallNode copy, Dictionary<string, Type> instantiatedGenerics)
    {
        FileName = copy.FileName;
        Line = copy.Line;
        InstantiatedGenerics = instantiatedGenerics;
        FunctionDefinitionModule = copy.FunctionDefinitionModule;
        Name = copy.Name;
        OverloadNameExt = copy.OverloadNameExt;
        Arguments = copy.Arguments;
        LineNum = copy.LineNum;
    }
    // Copy constructor for monomorphization (we need separate one for variadic function calls)
    public FunctionCallNode(FunctionCallNode copy, List<Type> instantiatedVariadics)
    {
        FileName = copy.FileName;
        Line = copy.Line;
        InstantiatedGenerics = copy.InstantiatedGenerics;
        FunctionDefinitionModule = copy.FunctionDefinitionModule;
        Name = copy.Name;
        OverloadNameExt = copy.OverloadNameExt;
        Arguments = copy.Arguments;
        LineNum = copy.LineNum;
        InstantiatedVariadics = instantiatedVariadics;
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

        // If the param counts don't match, it's not a match.
        // if (givenFunction.ParameterVariables.Count != Parameters.Count)
        // {
        //     return false;
        // }
        if (givenFunction.ParameterVariables.Count != Arguments.Count)
        {
            return false;
        }
        // If there's any parameter whose type and 'var' status would disqualify the given
        // function from matching this call, return false, otherwise true.
        // return !Parameters
        //     .Where(
        //         (p, i) =>
        //             !p.EqualsWrtTypeAndVar(
        //                 givenFunction.ParameterVariables[i],
        //                 variablesInScope
        //             )
        //     )
        //     .Any();
        //return !Arguments
        // .Where(
        //     (p, i) =>
        //         !p.EqualsWrtTypeAndVar(
        //             givenFunction.ParameterVariables[i],
        //             variablesInScope
        //         )
        // )
        // .Any();
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
        // if (Parameters.Any())
        // {
        //     Parameters.ForEach(p => b.AppendLine($"   {p}"));
        // }

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

    // public override void VisitStatement(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     visitor.Visit(this);
    //     // var function =
    //     //     context.GetFunction(Name) ?? throw new Exception($"function {Name} not found");
    //     // // if any arguement is not mutable, but is required to be mutable
    //     // if (
    //     //     function
    //     //         .ArguementMutability.Zip(Parameters.Select(p => p.IsVariable))
    //     //         .Any(a => a is { First: true, Second: false })
    //     // )
    //     // {
    //     //     throw new Exception($"call to {Name} has a mismatch of mutability");
    //     // }
    //     //
    //     // var parameters = Parameters.Select(p => p.Visit(visitor, context, builder, module));
    //     // builder.BuildCall2(function.TypeOf, function.Function, parameters.ToArray());
    // }

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

    public override void Visit(StatementVisitor visit)
    {
        visit.Accept(this);
    }

    public override void Accept(Visitor v) => v.Visit(this);
}
