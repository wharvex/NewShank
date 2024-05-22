using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

public class ProgramNode : ASTNode
{
    public Dictionary<string, ModuleNode> Modules { get; } = [];
    public ModuleNode? StartModule { get; set; }

    public ModuleNode GetStartModuleSafe() =>
        StartModule ?? throw new InvalidOperationException("Expected StartModule to not be null.");

    public void AddToModules(ModuleNode m)
    {
        // Add the module and return if there is no name collision.
        if (Modules.TryAdd(m.Name, m))
        {
            return;
        }

        if (m.Name.Equals("default"))
        {
            // At this point, there is a name collision and the name is `default', so we use
            // GetFromModulesSafe because we know it's there.
            GetFromModulesSafe("default").MergeModule(m);
        }

        throw new InvalidOperationException("Module `" + m.Name + "' already exists.");
    }

    public ModuleNode? GetFromModules(string name)
    {
        if (Modules.TryGetValue(name, out var m))
        {
            // TryGetValue always makes its "out" argument nullable, even if the kvp's Value is not.
            return m
                ?? throw new InvalidOperationException(
                    "Expected module `" + name + "' to not be null."
                );
        }

        return null;
    }

    public ModuleNode GetFromModulesSafe(string name) =>
        GetFromModules(name)
        ?? throw new InvalidOperationException("Module `" + name + "' not found.");

    public void SetStartModule()
    {
        var maybeStartModules = Modules
            .Where(kvp => kvp.Value.Functions.ContainsKey("start"))
            .Select(kvp => kvp.Value)
            .ToList();

        StartModule = maybeStartModules.Count switch
        {
            1 => maybeStartModules[0],
            > 1
                => throw new InvalidOperationException(
                    "Multiple start functions not allowed. This should be a SemanticErrorException."
                ),
            < 1
                => throw new InvalidOperationException(
                    "At least one start function required. This should be a SemanticErrorException."
                ),
        };
    }

    public override LLVMValueRef Visit(
        Visitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }
}
