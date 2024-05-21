namespace Shank;

public class TestNode : FunctionNode
{
    public string targetFunctionName;
    public List<VariableNode> testingFunctionParameters = new();

    public TestNode(string name, string targetFnName)
        : base(name)
    {
        Name = name;
        targetFunctionName = targetFnName;
        IsPublic = false;
        Execute = (List<InterpreterDataType> paramList) =>
            Interpreter.InterpretFunction(this, paramList);
    }
}