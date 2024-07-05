using Shank.ASTNodes;

namespace Shank.WalkCompliantVisitors;

public class VariablesGettingVisitor : WalkCompliantVisitor
{
    private string? _currentModuleName;
    private string CurrentModuleName
    {
        get => _currentModuleName ?? throw new InvalidOperationException();
        set => _currentModuleName = value;
    }
    private string? CurrentFunctionName { get; set; }

    public Dictionary<
        (string, string?),
        List<VariableDeclarationNode>
    > VariableDeclarations { get; set; } = [];

    public override ASTNode Visit(ProgramNode n, out bool shortCircuit)
    {
        shortCircuit = false;
        return n;
    }

    public override ASTNode Visit(ModuleNode n, out bool shortCircuit)
    {
        CurrentModuleName = n.Name;
        shortCircuit = false;
        return n;
    }

    public override ASTNode Visit(EnumNode n, out bool shortCircuit)
    {
        shortCircuit = false;
        return n;
    }

    public override ASTNode Visit(FunctionNode n, out bool shortCircuit)
    {
        CurrentFunctionName = n.Name;
        shortCircuit = false;
        return n;
    }

    public override ASTNode? Visit(VariableDeclarationNode n)
    {
        if (
            VariableDeclarations.TryGetValue((CurrentModuleName, CurrentFunctionName), out var vDex)
        )
        {
            vDex.Add(n);
        }
        else
        {
            VariableDeclarations[(CurrentModuleName, CurrentFunctionName)] = [n];
        }

        return n;
    }

    public override ASTNode Visit(VariableDeclarationNode n, out bool shortCircuit)
    {
        if (
            VariableDeclarations.TryGetValue((CurrentModuleName, CurrentFunctionName), out var vDex)
        )
        {
            vDex.Add(n);
        }
        else
        {
            VariableDeclarations[(CurrentModuleName, CurrentFunctionName)] = [n];
        }

        shortCircuit = true;

        return n;
    }

    public override ASTNode Final(FunctionNode n)
    {
        List<VariableDeclarationNode> varsInScopeList = [];
        if (VariableDeclarations.TryGetValue((CurrentModuleName, n.Name), out var vDex))
        {
            varsInScopeList.AddRange(vDex);
        }

        if (VariableDeclarations.TryGetValue((CurrentModuleName, null), out var globalVDex))
        {
            varsInScopeList.AddRange(globalVDex);
        }

        // First two method chains here remove name duplicates.
        // See: https://stackoverflow.com/a/4095023/16458003
        n.VariablesInScope = varsInScopeList
            .GroupBy(e => e.GetNameSafe())
            .Select(e => e.First())
            .Select(e => new KeyValuePair<string, VariableDeclarationNode>(e.GetNameSafe(), e))
            .ToDictionary();
        return n;
    }
}
