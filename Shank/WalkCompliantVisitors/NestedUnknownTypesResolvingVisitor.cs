using Shank.ASTNodes;

namespace Shank.WalkCompliantVisitors;

public class NestedUnknownTypesResolvingVisitor : WalkCompliantVisitor
{
    public delegate Type TypeResolver(
        Type member,
        ModuleNode module,
        List<string> generics,
        Func<string, GenericType> genericCollector
    );

    public TypeResolver ActiveTypeResolver { get; set; }

    private ModuleNode? _currentModule;
    public ModuleNode CurrentModule
    {
        get => _currentModule ?? throw new InvalidOperationException();
        set => _currentModule = value;
    }

    public NestedUnknownTypesResolvingVisitor(TypeResolver typeResolver)
    {
        ActiveTypeResolver = typeResolver;
    }

    public override ASTNode Visit(ProgramNode n, out bool shortCircuit)
    {
        shortCircuit = false;
        return n;
    }

    public override ASTNode Visit(ModuleNode n, out bool shortCircuit)
    {
        shortCircuit = false;
        CurrentModule = n;
        return n;
    }

    public override ASTNode Visit(RecordNode n, out bool shortCircuit)
    {
        shortCircuit = false;

        var genericContext = new RecordGenericContext(n.Name, n.GetParentModuleSafe());
        // This is adapted from SemanticAnalysis.AssignNestedTypes
        List<string> usedGenerics = [];
        n.Type.Fields = n.Type.Fields.Select(
            fieldKvp =>
                new KeyValuePair<string, Type>(
                    fieldKvp.Key,
                    ActiveTypeResolver(
                        fieldKvp.Value,
                        CurrentModule,
                        n.GenericTypeParameterNames,
                        generic =>
                        {
                            usedGenerics.Add(generic);
                            return new GenericType(generic, genericContext);
                        }
                    )
                )
        )
            .ToDictionary();

        if (!usedGenerics.Distinct().SequenceEqual(n.GenericTypeParameterNames))
            throw new SemanticErrorException(
                "Generic Type parameter(s)"
                    + string.Join(", ", n.GenericTypeParameterNames.Except(usedGenerics.Distinct()))
                    + " are unused for record "
                    + n.Name,
                n
            );

        return n;
    }
}
