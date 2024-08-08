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

    // TODO: better soloution
    // We currently really on the function name and overload being nullable, so that the global variables of the module are stored VariableDeclarations[(moduleName, null, null)]
    private string? CurrentFunctionName { get; set; }
    private TypeIndex? CurrentFunctionOverload { get; set; }
    private string? CurrentEnumName { get; set; }

    // not enough information for overloads
    public Dictionary<
        (string, string?, TypeIndex?),
        List<VariableDeclarationNode>
    > VariableDeclarations { get; set; } = [];

    public Dictionary<
        string,
        List<VariableDeclarationNode>
    > VariableDeclarationsFromEnums { get; set; } = [];

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
        CurrentEnumName = n.TypeName;
        shortCircuit = false;
        return n;
    }

    public override ASTNode Visit(FunctionNode n, out bool shortCircuit)
    {
        CurrentFunctionName = n.Name;
        CurrentFunctionOverload = n.Overload;
        shortCircuit = false;
        return n;
    }

    public override ASTNode Visit(OverloadedFunctionNode n, out bool shortCircuit)
    {
        shortCircuit = false;
        return n;
    }

    public override ASTNode? Visit(VariableDeclarationNode n)
    {
        if (
            VariableDeclarations.TryGetValue(
                (CurrentModuleName, CurrentFunctionName, CurrentFunctionOverload),
                out var vDex
            )
        )
        {
            vDex.Add(n);
        }
        else
        {
            VariableDeclarations[
                (CurrentModuleName, CurrentFunctionName, CurrentFunctionOverload)
            ] = [n];
        }

        return n;
    }

    public override ASTNode Visit(VariableDeclarationNode n, out bool shortCircuit)
    {
        if (
            VariableDeclarations.TryGetValue(
                (CurrentModuleName, CurrentFunctionName, CurrentFunctionOverload),
                out var vDex
            )
        )
        {
            vDex.Add(n);
        }
        else
        {
            VariableDeclarations[
                (CurrentModuleName, CurrentFunctionName, CurrentFunctionOverload)
            ] = [n];
        }

        shortCircuit = true;

        return n;
    }

    public override ASTNode Final(FunctionNode n)
    {
        List<VariableDeclarationNode> varsInScopeList = [];
        if (VariableDeclarations.TryGetValue((CurrentModuleName, n.Name, n.Overload), out var vDex))
        {
            varsInScopeList.AddRange(vDex);
        }

        if (VariableDeclarations.TryGetValue((CurrentModuleName, null, null), out var globalVDex))
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
        // clearing the function information so that the global variables of any module after this one do not get stuffed into this functions's variables index.
        CurrentFunctionName = null;
        CurrentFunctionOverload = null;
        return n;
    }
}
