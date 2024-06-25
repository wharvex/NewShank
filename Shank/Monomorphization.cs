using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

public record struct ModuleIndex(string Name, string Module);

public record struct TypedModuleIndex(ModuleIndex Index, Dictionary<string, Type> InstantiatedTypes)
{
    private int? hashCode = null;

    public bool Equals(TypedModuleIndex other)
    {
        return other.Index == Index && other.InstantiatedTypes.SequenceEqual(InstantiatedTypes);
    }

    public override int GetHashCode()
    {
        if (hashCode == null)
        {
            int hash = 17;
            foreach (var element in InstantiatedTypes)
            {
                int h = EqualityComparer<KeyValuePair<string, Type>>.Default.GetHashCode(element);
                if (h != 0)
                    hash = unchecked(hash * h);
            }

            hashCode = Index.GetHashCode() + hash;
        }

        return (int)hashCode!;
    }

    public override string ToString()
    {
        return $"{Index}({string.Join(", ", InstantiatedTypes.Select(typePair => $"{typePair.Value}"))})";
    }
}

public class MonomorphizedProgramNode
{
    public Dictionary<TypedModuleIndex, RecordNode> Records { get; } = [];
    public Dictionary<ModuleIndex, EnumNode> Enums { get; } = [];
    public Dictionary<ModuleIndex, VariableDeclarationNode> GlobalVariables { get; } = [];
    public Dictionary<TypedModuleIndex, FunctionNode> Functions { get; } = [];
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

    public override void Visit(FunctionCallNode node)
    {
        var module = nonMonomorphizedProgramNode.GetFromModulesSafe(node.FunctionDefinitionModule);
        module
            .Functions[node.Name]
            .Accept(
                new MonomorphizationVisitor(
                    node.InstiatedGenerics.Select(
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
                        .ToDictionary(),
                    nonMonomorphizedProgramNode,
                    start,
                    ProgramNode
                )
            );
        Push(node);
    }

    public override void Visit(FunctionNode node)
    {
        var typedModuleIndex = new TypedModuleIndex(
            new ModuleIndex(node.Name, node.parentModuleName!),
            instantiatedTypes
        );
        if (ProgramNode.Functions.TryGetValue(typedModuleIndex, out var functionNode))
        {
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

        var function = new FunctionNode(node, parameters, variables, statements);
        ProgramNode.Functions[typedModuleIndex] = function;
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
            node.Type.Accept(
                new MonomorphizationTypeVisitor(
                    instantiatedTypes,
                    start,
                    ProgramNode
                )
            )
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
}

public class MonomorphizationTypeVisitor(
    Dictionary<string, Type> instantiatedTypes,
    ModuleNode start,
    MonomorphizedProgramNode programNode
) : ITypeVisitor<Type>
{
    public Type Visit(RealType type) => type;

    public Type Visit(RecordType type)
    {
        var typedModuleIndex = new TypedModuleIndex(
            new ModuleIndex(type.Name, type.ModuleName),
            instantiatedTypes
        );
        if (programNode.Records.TryGetValue(typedModuleIndex, out var recordNode))
        {
            return recordNode.Type;
        }

        var record = new RecordNode(type.Name, type.ModuleName, [], []);
        programNode.Records[typedModuleIndex] = record;
        var recordType = new RecordType(
            type.Name,
            type.ModuleName,
            type.Fields.Select(field => (field.Key, field.Value.Accept(this))).ToDictionary(),
            []
        );
        record.Type = recordType;
        return record.Type;
    }

    public Type Visit(InstantiatedType type)
    {
        type = (InstantiatedType)type.Instantiate(instantiatedTypes);
        return type.Inner.Accept(
            new MonomorphizationTypeVisitor(
                type.InstantiatedGenerics,
                start,
                programNode
            )
        );
    }

    public Type Visit(ArrayType type)
    {
        return new ArrayType(type.Inner.Accept(this), type.Range);
    }

    public Type Visit(EnumType type)
    {
        var moduleIndex = new ModuleIndex(type.Name, type.ModuleName);
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
