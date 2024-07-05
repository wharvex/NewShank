using Shank.ASTNodes;

namespace Shank.WalkCompliantVisitors;

public class ExpressionTypingVisitor(ExpressionTypingVisitor.ExprTyGetter getExpressionType)
    : WalkCompliantVisitor
{
    public delegate Type ExprTyGetter(
        ExpressionNode node,
        Dictionary<string, VariableDeclarationNode> variables
    );

    private FunctionNode? _currentFunction;
    public FunctionNode CurrentFunction
    {
        get => _currentFunction ?? throw new InvalidOperationException();
        set => _currentFunction = value;
    }

    public ExprTyGetter GetExpressionType { get; init; } = getExpressionType;

    public InterpretOptions? ActiveInterpretOptions { get; set; }

    public bool GetVuopTestFlag()
    {
        return ActiveInterpretOptions?.VuOpTest ?? false;
    }

    public override ASTNode Visit(ProgramNode n, out bool shortCircuit)
    {
        shortCircuit = false;
        return n;
    }

    public override ASTNode Visit(ModuleNode n, out bool shortCircuit)
    {
        shortCircuit = false;
        return n;
    }

    public override ASTNode Visit(FunctionNode n, out bool shortCircuit)
    {
        shortCircuit = false;
        CurrentFunction = n;
        return n;
    }

    public override ASTNode Visit(AssignmentNode n, out bool shortCircuit)
    {
        shortCircuit = false;
        return n;
    }

    public override ASTNode Visit(ExpressionNode n, out bool shortCircuit)
    {
        shortCircuit = true;
        n.Type = GetExpressionType(n, CurrentFunction.VariablesInScope);
        return n;
    }
}
