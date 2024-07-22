using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

public interface Index;

public record struct NamedIndex(string Name) : Index;

public readonly record struct ModuleIndex(NamedIndex Name, string Module) : Index
{
    public override string ToString()
    {
        return $"{Module}::{Name.Name}";
    }
};

public record struct TypeIndex(List<Type> InstantiatedTypes)
{
    private int? hashCode = null;

    public readonly bool Equals(TypeIndex other)
    {
        return other.InstantiatedTypes.SequenceEqual(InstantiatedTypes);
    }

    public override int GetHashCode()
    {
        if (hashCode != null)
            return (int)hashCode!;
        var hash = InstantiatedTypes
            .Select(element => EqualityComparer<Type>.Default.GetHashCode(element))
            .Where(h => h != 0)
            .Aggregate(17, (current, h) => unchecked(current * h));
        hashCode = hash;
        return (int)hashCode!;
    }
}

public readonly record struct TypedModuleIndex(ModuleIndex Index, TypeIndex Types) : Index
{
    public override string ToString()
    {
        return $"{Index}({string.Join(", ", Types.InstantiatedTypes.Select(type => $"{type}"))})";
    }
}

public readonly record struct TypedBuiltinIndex(string Name, TypeIndex Types) : Index
{
    public override string ToString()
    {
        return $"{Name}({string.Join(", ", Types.InstantiatedTypes.Select(type => $"{type}"))})";
    }
}

public class MonomorphizedProgramNode
{
    public Dictionary<TypedModuleIndex, RecordNode> Records { get; } = [];
    public Dictionary<ModuleIndex, EnumNode> Enums { get; } = [];
    public Dictionary<ModuleIndex, VariableDeclarationNode> GlobalVariables { get; } = [];
    public Dictionary<TypedModuleIndex, FunctionNode> Functions { get; } = [];
    public Dictionary<TypedBuiltinIndex, BuiltInFunctionNode> BuiltinFunctions { get; } = [];
}

// TODO: maybe use an visitor that return something, as opposed to void as we are not modifying the original ast, but creating a new version of it
public class MonomorphizationVisitor(
    Dictionary<string, Type> instantiatedTypes,
    ProgramNode nonMonomorphizedProgramNode,
    ModuleNode start,
    MonomorphizedProgramNode programNode
) : Visitor
{
    public MonomorphizedProgramNode ProgramNode = programNode;

    public MonomorphizationVisitor()
        : this([], null!, null!, new MonomorphizedProgramNode()) { }

    private Stack<object> expr = new();

    private object Pop() => expr.Pop();

    private void Push(object expr) => this.expr.Push(expr);

    public override void Visit(IntNode node) { }

    public override void Visit(FloatNode node) { }

    public override void Visit(VariableUsagePlainNode node) { }

    public override void Visit(CharNode node) { }

    public override void Visit(BoolNode node) { }

    public override void Visit(StringNode node) { }

    public override void Visit(MathOpNode node) { }

    public override void Visit(BooleanExpressionNode node) { }

    public override void Visit(RecordNode node) { }

    private void Visit(BuiltInFunctionNode node, FunctionCallNode caller)
    {
        if (node is BuiltInVariadicFunctionNode variadicFunctionNode)
        {
            var instantiatedVariadics = caller
                .InstantiatedVariadics.Select(
                    instantiatedType =>
                        instantiatedType.Accept(
                            new MonomorphizationTypeVisitor(instantiatedTypes, start, ProgramNode)
                        )
                )
                .ToList();
            var typedBuiltinIndex = new TypedBuiltinIndex(
                node.Name,
                new TypeIndex(instantiatedVariadics)
            );
            Push(typedBuiltinIndex);
            if (programNode.BuiltinFunctions.ContainsKey(typedBuiltinIndex))
            {
                return;
            }

            var parameters = instantiatedVariadics
                .Select(
                    (parameterType, index) =>
                        new VariableDeclarationNode()
                        {
                            // each parameters name is just its index, because we don't actually need the name, as we will usually just iterate through the parameter list
                            Name = $"Parameter{index}",
                            Type = parameterType,
                            IsConstant = variadicFunctionNode.AreParametersConstant
                        }
                )
                .ToList();
            var function = new BuiltInFunctionNode(variadicFunctionNode, parameters)
            {
                MonomorphizedName = typedBuiltinIndex
            };
            // TODO: maybe specialize this further by turning it into multiple individual function calls for each arguement (might be annoying for write, because "write "Foo"" would probably become "writeString "Foo"; writeString "\n"", we would also probalb have a lot of "writeString " "" when we have mutliple arguements)
            programNode.BuiltinFunctions[typedBuiltinIndex] = function;
        }
        else
        {
            var instantiatedGenerics = caller
                .InstantiatedGenerics.Select(
                    instantiatedType =>
                        (
                            instantiatedType.Key,
                            instantiatedType.Value.Accept(
                                new MonomorphizationTypeVisitor(
                                    instantiatedTypes,
                                    start,
                                    ProgramNode
                                )
                            )
                        )
                )
                .ToDictionary();
            var typedBuiltinIndex = new TypedBuiltinIndex(
                node.Name,
                new TypeIndex(
                    instantiatedGenerics
                        .OrderBy(pair => pair.Key)
                        .Select(pair => pair.Value)
                        .ToList()
                )
            );
            if (programNode.BuiltinFunctions.ContainsKey(typedBuiltinIndex))
            {
                Push(typedBuiltinIndex);
                return;
            }

            var parameters = node.ParameterVariables.Select(declarationNode =>
            {
                declarationNode.Accept(
                    new MonomorphizationVisitor(
                        instantiatedGenerics,
                        nonMonomorphizedProgramNode,
                        start,
                        programNode
                    )
                    {
                        expr = expr
                    }
                );
                return (VariableDeclarationNode)Pop();
            })
                .ToList();
            var function = new BuiltInFunctionNode(node, parameters)
            {
                MonomorphizedName = typedBuiltinIndex
            };
            programNode.BuiltinFunctions[typedBuiltinIndex] = function;
            Push(typedBuiltinIndex);
        }
    }

    public override void Visit(FunctionCallNode node)
    {
        // no module means it is a builtin see FunctionCallNode.FunctionDefinitionModule
        if (node.FunctionDefinitionModule is null)
        {
            Console.WriteLine("1st time with add");
            BuiltInFunctionNode builtInFunctionNode = (
                nonMonomorphizedProgramNode.GetStartModuleSafe().getFunction(node.Name)
                as BuiltInFunctionNode
            )!;
            Visit(builtInFunctionNode!, node);
            Push(new FunctionCallNode(node, (TypedBuiltinIndex)Pop()));
        }
        else
        {
            Console.WriteLine("2nd time with add");

            var module = nonMonomorphizedProgramNode.GetFromModulesSafe(
                node.FunctionDefinitionModule
            );
            var instantiatedGenerics = node.InstantiatedGenerics.Select(
                instantiatedType =>
                    (
                        instantiatedType.Key,
                        instantiatedType.Value.Accept(
                            new MonomorphizationTypeVisitor(instantiatedTypes, start, ProgramNode)
                        )
                    )
            )
                .ToDictionary();
            module
                .Functions[node.Name]
                .Accept(
                    new MonomorphizationVisitor(
                        instantiatedGenerics,
                        nonMonomorphizedProgramNode,
                        module,
                        ProgramNode
                    )
                    {
                        expr = expr
                    }
                );
            // TODO: less hacky solution to demodulize global variables, maybe only add them when they are used
            foreach (var (key, value) in module.GlobalVariables)
            {
                value.Accept(this);
                programNode.GlobalVariables[(ModuleIndex)value.MonomorphizedName()] =
                    (VariableDeclarationNode)Pop();
            }

            Push(new FunctionCallNode(node, (TypedModuleIndex)Pop()));
        }
    }

    public override void Visit(FunctionNode node)
    {
        Console.WriteLine("func node: " + node.ToString());
        var typedModuleIndex = new TypedModuleIndex(
            new ModuleIndex(new NamedIndex(node.Name), node.parentModuleName!),
            new TypeIndex(
                instantiatedTypes.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList()
            )
        );
        if (ProgramNode.Functions.TryGetValue(typedModuleIndex, out _))
        {
            Push(typedModuleIndex);
            return;
        }

        var parameters = node.ParameterVariables.Select(declarationNode =>
        {
            declarationNode.Accept(this);
            return (VariableDeclarationNode)Pop();
        })
            .ToList();
        var variables = node.LocalVariables.Select(declarationNode =>
        {
            declarationNode.Accept(this);
            return (VariableDeclarationNode)Pop();
        })
            .ToList();
        var statements = node.Statements.Select(statementNode =>
        {
            statementNode.Accept(this);
            return (StatementNode)Pop();
        })
            .ToList();

        var function = new FunctionNode(node, parameters, variables, statements)
        {
            MonomorphizedName = typedModuleIndex
        };
        ProgramNode.Functions[typedModuleIndex] = function;
        Push(typedModuleIndex);
    }

    public override void Visit(WhileNode node)
    {
        var children = node.Children.Select(statementNode =>
        {
            statementNode.Accept(this);
            return (StatementNode)Pop();
        })
            .ToList();
        Push(new WhileNode(node, children));
    }

    public override void Visit(AssignmentNode node)
    {
        Push(node);
    }

    public override void Visit(EnumNode node) { }

    public override void Visit(ModuleNode node)
    {
        start = node;
        // TODO: less hacky solution to demodulize global variables, maybe only add them when they are used
        foreach (var (key, value) in node.GlobalVariables)
        {
            value.Accept(this);
            programNode.GlobalVariables[(ModuleIndex)value.MonomorphizedName()] =
                (VariableDeclarationNode)Pop();
        }

        node.GetStartFunctionSafe().Accept(this);
    }

    public override void Visit(IfNode node)
    {
        if (node is ElseNode elseNode)
        {
            var children = node.Children.Select(statementNode =>
            {
                statementNode.Accept(this);
                return (StatementNode)Pop();
            })
                .ToList();
            Push(new ElseNode(elseNode, children));
        }
        else
        {
            var children = node.Children.Select(statementNode =>
            {
                statementNode.Accept(this);
                return (StatementNode)Pop();
            })
                .ToList();
            node.NextIfNode?.Accept(this);
            var nextIfNode = node.NextIfNode is null ? null : (IfNode)Pop();

            Push(new IfNode(node, children, nextIfNode));
        }
    }

    public override void Visit(RepeatNode node)
    {
        var children = node.Children.Select(statementNode =>
        {
            statementNode.Accept(this);
            return (StatementNode)Pop();
        })
            .ToList();
        Push(new RepeatNode(node, children));
    }

    public override void Visit(VariableDeclarationNode node)
    {
        var variableDeclarationNode = new VariableDeclarationNode(
            node,
            node.Type.Accept(new MonomorphizationTypeVisitor(instantiatedTypes, start, ProgramNode))
        );
        Push(variableDeclarationNode);
    }

    public override void Visit(ProgramNode node)
    {
        nonMonomorphizedProgramNode = node;
        node.StartModule!.Accept(this);
    }

    public override void Visit(ForNode node)
    {
        var children = node.Children.Select(statementNode =>
        {
            statementNode.Accept(this);
            return (StatementNode)Pop();
        })
            .ToList();
        Push(new ForNode(node, children));
    }

    public override void Visit(BuiltInFunctionNode node)
    {
        throw new NotImplementedException();
    }
}

public class MonomorphizationTypeVisitor(
    Dictionary<string, Type> instantiatedTypes,
    ModuleNode start,
    MonomorphizedProgramNode programNode
) : ITypeVisitor<Type>
{
    public Type Visit(DefaultType type) => type;

    public Type Visit(RealType type) => type;

    public Type Visit(RecordType type)
    {
        var typedModuleIndex = new TypedModuleIndex(
            new ModuleIndex(new NamedIndex(type.Name), type.ModuleName),
            new TypeIndex(
                instantiatedTypes.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList()
            )
        );
        if (programNode.Records.TryGetValue(typedModuleIndex, out var recordNode))
        {
            return recordNode.Type;
        }

        var record = new RecordNode(type.Name, type.ModuleName, [], [])
        {
            Type = { MonomorphizedIndex = typedModuleIndex }
        };
        programNode.Records[typedModuleIndex] = record;
        var recordType = new RecordType(
            type.Name,
            type.ModuleName,
            type.Fields.Select(field => (field.Key, field.Value.Accept(this))).ToDictionary(),
            []
        )
        {
            MonomorphizedIndex = typedModuleIndex
        };
        record.Type = recordType;
        return record.Type;
    }

    public Type Visit(InstantiatedType type)
    {
        type = (InstantiatedType)type.Instantiate(instantiatedTypes);
        return type.Inner.Accept(
            new MonomorphizationTypeVisitor(type.InstantiatedGenerics, start, programNode)
        );
    }

    public Type Visit(ArrayType type)
    {
        return new ArrayType(type.Inner.Accept(this), type.Range);
    }

    public Type Visit(EnumType type)
    {
        var moduleIndex = new ModuleIndex(new NamedIndex(type.Name), type.ModuleName);
        programNode.Enums.TryAdd(
            moduleIndex,
            (EnumNode)(start.getEnums().GetValueOrDefault(type.Name) ?? start.Imported[type.Name])
        );
        return type;
    }

    public Type Visit(ReferenceType type)
    {
        return new ReferenceType(type.Inner.Accept(this));
    }

    public Type Visit(UnknownType type) => type;

    public Type Visit(GenericType type)
    {
        return instantiatedTypes[type.Name];
    }

    public Type Visit(BooleanType type) => type;

    public Type Visit(CharacterType type) => type;

    public Type Visit(StringType type) => type;

    public Type Visit(IntegerType type) => type;
}
