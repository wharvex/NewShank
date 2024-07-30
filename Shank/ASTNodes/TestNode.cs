namespace Shank.ASTNodes;

public class TestNode : FunctionNode
{
    public string targetFunctionName;
    public List<VariableDeclarationNode> testingFunctionParameters = new();

    public TestNode(string name, string moduleName, string targetFnName)
        : base(name, moduleName)
    {
        Name = name;
        targetFunctionName = targetFnName;
        IsPublic = false;
        Execute = paramList => Interpreter.InterpretFunction(this, paramList);
    }

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

        for (var index = 0; index < LocalVariables.Count; index++)
        {
            LocalVariables[index] = (VariableDeclarationNode)(
                LocalVariables[index].Walk(v) ?? LocalVariables[index]
            );
        }

        for (var index = 0; index < Statements.Count; index++)
        {
            Statements[index] = (StatementNode)(Statements[index].Walk(v) ?? Statements[index]);
        }

        return v.PostWalk(this);
    }
}
