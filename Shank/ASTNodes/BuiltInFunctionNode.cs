using CommandLine.Text;
using LLVMSharp.Interop;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class BuiltInFunctionNode : CallableNode
{
    public BuiltInFunctionNode(string name, BuiltInCall execute)
        : base(name, execute) { }

    public List<string> GenericTypeParameterNames { get; set; } = [];

    // public override LLVMValueRef Visit(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     throw new NotImplementedException();
    // }


    // Copy constructor for monomorphization
    public BuiltInFunctionNode(
        BuiltInFunctionNode function,
        List<VariableDeclarationNode> parameters
    )
        : base(function.Name)
    {
        parentModuleName = function.parentModuleName;
        LineNum = function.LineNum;
        FileName = function.FileName;
        Line = function.Line;
        Execute = function.Execute;
        ParameterVariables.AddRange(parameters);
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
}

public class BuiltInVariadicFunctionNode(
    string name,
    CallableNode.BuiltInCall execute,
    bool areParametersConstant = true
) : BuiltInFunctionNode(name, execute)
{
    public bool AreParametersConstant { get; } = areParametersConstant;
}
