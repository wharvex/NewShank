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

    public void VisitStatement(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        throw new NotImplementedException();
    }

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
