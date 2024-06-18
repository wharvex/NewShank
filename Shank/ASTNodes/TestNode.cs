using Shank.ASTNodes;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class TestNode : FunctionNode
{
    public string targetFunctionName;
    public List<VariableDeclarationNode> testingFunctionParameters = new();

    public TestNode(string name, string targetFnName)
        : base(name)
    {
        Name = name;
        targetFunctionName = targetFnName;
        IsPublic = false;
        Execute = (List<InterpreterDataType> paramList) =>
            Interpreter.InterpretFunction(this, paramList);
    }

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
