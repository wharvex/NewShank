using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

/// <summary>
/// does monomorphization
/// what is monomorphization?
/// lets say you have this code
/// define swap(var a: A, var b: A) generic A
/// variables temp: A
///     temp := a
///     b := a
///     a := b
/// define start()
/// variables x, y: integer
/// variables a, b: bool
///     swap var x, var y
///     swap var a, var b
///
/// Now LLVM (and most other low level representations of programs) do not have a concept of generics.
/// So we use monomorphization to implement generics.
/// The first step is during semantic analysis is to infer and mark what each generic in the function corresponds to (this is called a substitution).
/// So you could imagine that after semantic analysis, the start function looks like:
/// define start()
/// variables x, y: integer
/// variables a, b: boolean
///     swap var x, var y [A=integer]
///     swap var a, var b [A=boolean]
///
/// In the monomorophization stage what we do is we copy the generic function for each different type the generics are used with (you can think of as like turning each generic function into an overloaded function)
/// We have to keep some more information, the information is instantiatedTypes which is like [A=integer] or [B=integer], this corresponds to any of the types we inferred for each generic, I will also be calling this a substitution (it is a mapping from generics to their actual type).
/// This information starts out as empty (because the start function does not have any generics).
/// We then traverse starting at the start function.
/// When we traverse to a function we first apply the substitution to the parameters and variables, then we traverse through the body of the function.
/// If its a builtin function then there is no body to traverse through.
/// We then add it back to the program indexed by the name, module, and parameters.
/// The only type of statement that we really care about when traversing is the function call (we have to traverse through all the other statements to find the function calls).
/// When we see a function call, we lookup the function, and the traverse with the substitution given by applying the current monomorphization substitution to the function calls substitution (inferred during semantic analysis)
///
/// So for our example what would happen:
/// instantiatedTypes = [], Visit(start)
/// found call (swap var x, var y) with substitution [A=integer]
///     instantiatedTypes = [A=integer], Visit(swap)
///     substituting parameters and variables
///     define swap(var a: integer, var b: integer) generic integer
///     variables temp: integer
///     adding function (swap, default, integer)
/// found call (swap var x, var y) with substitution [A=boolean]
///     instantiatedTypes = [A=boolean], Visit(swap)
///     substituting parameters and variables
///     define swap(var a: boolean, var b: boolean) generic boolean
///     variables temp: boolean
///     adding function (swap, default, boolean)
/// Resulting in:
/// define swap[default, integer](var a: integer, var b: integer)
/// variables temp: integer
///     temp := a
///     b := a
///     a := b
/// define swap[default, boolean](var a: boolean, var b: boolean)
/// variables temp: boolean
///     temp := a
///     b := a
///     a := b
/// define start()
/// variables x, y: integer
/// variables a, b: bool
///     swap[default, integer] var x, var y
///     swap[default, boolean] var a, var b
/// </summary>
public interface Index;

public record struct NamedIndex(string Name) : Index;

public readonly record struct ModuleIndex(NamedIndex Name, string Module) : Index
{
    public override string ToString()
    {
        return $"{Module}::{Name.Name}";
    }
};

// TODO: should also keep track of mutability of each type because different overloads could have different mutability for same parameter?
public record struct TypeIndex(List<Type> Types)
{
    private int? _hashCode = null;

    public readonly bool Equals(TypeIndex other)
    {
        return other.Types.SequenceEqual(Types);
    }

    public override int GetHashCode()
    {
        if (_hashCode != null)
            return (int)_hashCode!;
        var hash = Types
            .Select(element => EqualityComparer<Type>.Default.GetHashCode(element))
            .Where(h => h != 0)
            .Aggregate(17, (current, h) => unchecked(current * h));
        _hashCode = hash;
        return (int)_hashCode!;
    }

    public override readonly string ToString() => $"Types: {{{string.Join(",", Types)}}}";
}

public readonly record struct TypedModuleIndex(ModuleIndex Index, TypeIndex Types) : Index
{
    public override string ToString()
    {
        return $"{Index}({string.Join(", ", Types.Types.Select(type => $"{type}"))})";
    }
}

public readonly record struct TypedBuiltinIndex(string Name, TypeIndex Types) : Index
{
    public override string ToString()
    {
        return $"{Name}({string.Join(", ", Types.Types.Select(type => $"{type}"))})";
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
    public bool OptionsUnitTest { get; }
    public MonomorphizedProgramNode ProgramNode { get; set; } = programNode;

    public MonomorphizationVisitor(bool optionsUnitTest)
        : this([], null!, null!, new MonomorphizedProgramNode())
    {
        OptionsUnitTest = optionsUnitTest;
    }

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
            if (ProgramNode.BuiltinFunctions.ContainsKey(typedBuiltinIndex))
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
            // TODO: maybe specialize this further by turning it into multiple individual function calls for each argument
            // (might be annoying for write, because "write "Foo"" would probably become "writeString "Foo"; writeString "\n"",
            // we would also probably have a lot of "writeString " "" when we have multiple arguments)
            ProgramNode.BuiltinFunctions[typedBuiltinIndex] = function;
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
            var parameters = node.ParameterVariables.Select(declarationNode =>
            {
                declarationNode.Accept(
                    new MonomorphizationVisitor(
                        instantiatedGenerics,
                        nonMonomorphizedProgramNode,
                        start,
                        ProgramNode
                    )
                    {
                        expr = expr
                    }
                );
                return (VariableDeclarationNode)Pop();
            })
                .ToList();
            var typedBuiltinIndex = new TypedBuiltinIndex(
                node.Name,
                new TypeIndex(parameters.Select(parameter => parameter.Type).ToList())
            );
            if (ProgramNode.BuiltinFunctions.ContainsKey(typedBuiltinIndex))
            {
                Push(typedBuiltinIndex);
                return;
            }

            var function = new BuiltInFunctionNode(node, parameters)
            {
                MonomorphizedName = typedBuiltinIndex
            };
            ProgramNode.BuiltinFunctions[typedBuiltinIndex] = function;
            Push(typedBuiltinIndex);
        }
    }

    public override void Visit(FunctionCallNode node)
    {
        // we check if its a builtin function (the module name is BuiltInFunctionNode.BuiltinModuleName)
        if (node.FunctionDefinitionModule == BuiltInFunctionNode.BuiltinModuleName)
        {
            var builtInFunctionNode = (
                nonMonomorphizedProgramNode.GetStartModuleSafe().getFunction(node.Name)
                as BuiltInFunctionNode
            )!;
            Visit(builtInFunctionNode, node);
            Push(new FunctionCallNode(node, (TypedBuiltinIndex)Pop()));
        }
        else
        {
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

            var moduleFunction = module.Functions[node.Name];

            (
                moduleFunction switch
                {
                    FunctionNode f => f,
                    OverloadedFunctionNode overload => overload.Overloads[node.Overload],
                    _ => null
                }
            )!.Accept(
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
            // TODO: less hacky solution to demodularize global variables, maybe only add them when they are used
            foreach (var (_, value) in module.GlobalVariables)
            {
                value.Accept(this);
                ProgramNode.GlobalVariables[(ModuleIndex)value.MonomorphizedName()] =
                    (VariableDeclarationNode)Pop();
            }

            Push(new FunctionCallNode(node, (TypedModuleIndex)Pop()));
        }
    }

    public override void Visit(FunctionNode node)
    {
        var parameters = node.ParameterVariables.Select(declarationNode =>
        {
            declarationNode.Accept(this);
            return (VariableDeclarationNode)Pop();
        })
            .ToList();
        var typedModuleIndex = new TypedModuleIndex(
            new ModuleIndex(new NamedIndex(node.Name), node.parentModuleName),
            new TypeIndex(parameters.Select(p => p.Type).ToList())
        );
        if (ProgramNode.Functions.TryGetValue(typedModuleIndex, out _))
        {
            Push(typedModuleIndex);
            return;
        }
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
        // TODO: less hacky solution to demodularize global variables, maybe only add them when they are used
        foreach (var (_, value) in node.GlobalVariables)
        {
            value.Accept(this);
            ProgramNode.GlobalVariables[(ModuleIndex)value.MonomorphizedName()] =
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
        if (OptionsUnitTest)
        {
            var unitTests = node.Modules.Values.SelectMany(
                module =>
                    module.Tests.Values.Select(p =>
                    {
                        p.Accept(this);
                        return (p, (TypedModuleIndex)Pop());
                    })
            );

            var functionCallNodes = unitTests.Select(
                u =>
                    new FunctionCallNode(u.Item2.Index.Name.Name)
                    {
                        InstantiatedGenerics = [],
                        FunctionDefinitionModule = u.Item2.Index.Module,
                        MonomphorizedFunctionLocater = u.Item2
                    }
            );
            node.StartModule.GetStartFunctionSafe().Statements.AddRange(functionCallNodes);
            foreach (var testNode in unitTests.Select(u => u.p))
            {
                node.StartModule.addFunction(testNode);
            }
            node.StartModule.GetStartFunctionSafe().Accept(this);
        }
        else
        {
            node.StartModule!.Accept(this);
        }
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

    public override void Visit(OverloadedFunctionNode node)
    {
        throw new NotImplementedException();
    }
}

// Monomorphization for types does the actual subisiton applications.
// We still need to keep instantiatedTypes (it is actually the whole point of the instantiatedTypes becasue we do the actual subsititons here).
// We traverse each type.
// There are two interesting cases generics and records.
// For the generic case, we lookup the generic name in the instantiatedTypes and return it.
// In the record case we recurse down through the fields, then we add the (copied) record to the program indexed by the name, module, and the types that each generic of the record is substituted.
// All the other cases are just recursing down the type or just returning the current type.
//
// Lets get back to our swap example:
// We are instanting the parameters (we have instantiatedTypes=[A=integer]), in swap(var a: A, var b: A) generic A.
// So we traverse from monomorphization and we have to instantiate the types (A, A).
// So we go and traverse the type and we find its a generic, and we have a subsition [A=integer], so we return integer.
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
                instantiatedTypes.OrderBy(pair => pair.Key).Where(pair => type.Generics.Contains(pair.Key)).Select(pair => pair.Value).ToList()
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
