using System.Text;
using Shank.ExprVisitors;
using Shank.WalkCompliantVisitors;

namespace Shank.ASTNodes;

public class FunctionNode : CallableNode
{
    public FunctionNode(string name, string moduleName, bool isPublic = false)
        : base(name, moduleName, isPublicIn: isPublic)
    {
        Execute = paramList => Interpreter.InterpretFunction(this, paramList);
        Name = name;
    }

    // Copy constructor for monomorphization
    public FunctionNode(
        FunctionNode function,
        List<VariableDeclarationNode> parameters,
        List<VariableDeclarationNode> variables,
        List<StatementNode> statements
    )
        : base(function.Name, function.parentModuleName)
    {
        LineNum = function.LineNum;
        FileName = function.FileName;
        Line = function.Line;
        Execute = function.Execute;
        ParameterVariables.AddRange(parameters);
        LocalVariables = variables;
        Statements = statements;
    }

    public List<VariableDeclarationNode> LocalVariables { get; set; } = [];
    public List<StatementNode> Statements { get; set; } = [];

    public Dictionary<string, TestNode> Tests { get; set; } = [];

    public Dictionary<string, VariableDeclarationNode> VariablesInScope { get; set; } = [];
    public Dictionary<string, List<VariableDeclarationNode>> EnumsInScope { get; set; } = [];

    public void ApplyActionToTests(
        Action<TestNode, List<InterpreterDataType>, ModuleNode?> action,
        ModuleNode module
    )
    {
        Tests.ToList().ForEach(testKvp => action(testKvp.Value, [], module));
    }

    public VariableDeclarationNode GetVariableNodeByName(string searchName)
    {
        return LocalVariables
                .Concat(ParameterVariables)
                .FirstOrDefault(
                    vn =>
                        (
                            vn
                            ?? throw new InvalidOperationException(
                                "Something went wrong internally. There should not be"
                                    + " null entries in FunctionNode.LocalVariables or"
                                    + " FunctionNode.ParameterVariables."
                            )
                        ).Name?.Equals(searchName)
                        ?? throw new InvalidOperationException(vn + " has no Name."),
                    null
                )
            ?? throw new ArgumentOutOfRangeException(
                nameof(searchName),
                "No variable found with given searchName."
            );
    }

    public override string ToString()
    {
        var linePrefix = $"function {Name}, line {Line}, ";
        var b = new StringBuilder();
        b.AppendLine(linePrefix + "begin");
        b.AppendLine(linePrefix + "param vars begin");
        ParameterVariables.ForEach(p => b.AppendLine(p + "; "));
        b.AppendLine(linePrefix + "param vars end");
        b.AppendLine(linePrefix + "local vars begin");
        LocalVariables.ForEach(v => b.Append(v + "; "));
        b.Append('\n');
        b.AppendLine(linePrefix + "local vars end");
        b.AppendLine(linePrefix + "statements begin");
        Statements.ForEach(s => b.AppendLine(s.ToString()));
        b.AppendLine(linePrefix + "statements end");
        b.AppendLine(linePrefix + "end");

        return b.ToString();
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

    public override ASTNode Walk(WalkCompliantVisitor v)
    {
        var ret = v.Visit(this, out var shortCircuit);
        if (shortCircuit)
        {
            return ret;
        }

        ParameterVariables = ParameterVariables.WalkList(v);
        LocalVariables = LocalVariables.WalkList(v);
        Statements = Statements.WalkList(v);
        Tests = Tests.WalkDictionary(v);

        return v.Final(this);
    }
}
