using Optional;
using Shank.ASTNodes;
using Shank.AstVisitorsTim;
using Shank.Utils;

namespace Shank;

public abstract class SAVisitor
{
    public static InterpretOptions? ActiveInterpretOptions { get; set; }

    public static bool GetVuopTestFlag()
    {
        return ActiveInterpretOptions?.VuOpTest ?? false;
    }

    public virtual ASTNode? Visit(ProgramNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(ProgramNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(ModuleNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(ModuleNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(FunctionNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(FunctionNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(FunctionCallNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(OverloadedFunctionNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(OverloadedFunctionNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(FunctionCallNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(VariableDeclarationNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(VariableDeclarationNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(VariableUsageNodeTemp node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(VariableUsageNodeTemp node)
    {
        return null;
    }

    public virtual ASTNode? Visit(VariableUsagePlainNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(VariableUsagePlainNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(AssignmentNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(AssignmentNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(WhileNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(WhileNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(IfNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(IfNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(ForNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(ForNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(RepeatNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(RepeatNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(MathOpNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(MathOpNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(IntNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(IntNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(FloatNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(FloatNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(CharNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(CharNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(BoolNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(BoolNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(BooleanExpressionNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(BooleanExpressionNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(StringNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(StringNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(EnumNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(EnumNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(RecordNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(RecordNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(BuiltInFunctionNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(BuiltInFunctionNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(TestNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(TestNode node)
    {
        return null;
    }

    public static Type GetTypeOfExpression(
        ExpressionNode node,
        Dictionary<string, VariableDeclarationNode> variables
    )
    {
        if (node is VariableUsageNodeTemp variableUsageNodeTemp)
        {
            if (GetVuopTestFlag())
            {
                return variableUsageNodeTemp.GetMyType(variables, GetTypeOfExpression);
            }
            var variableUsagePlainNode = (VariableUsagePlainNode)variableUsageNodeTemp;
            var name = variableUsagePlainNode.Name;
            var variableDeclarationNode = variables.GetValueOrDefault(variableUsagePlainNode.Name);
            switch (variableUsagePlainNode.ExtensionType)
            {
                case VariableUsagePlainNode.VrnExtType.None:
                    return variableDeclarationNode.Type;
                case VariableUsagePlainNode.VrnExtType.RecordMember:
                    switch (variableDeclarationNode.Type)
                    {
                        case InstantiatedType instantiatedType:
                            return GetTypeRecursive(instantiatedType, variableUsagePlainNode)
                                ?? variableDeclarationNode.Type;
                        case ReferenceType(InstantiatedType reference):
                            return GetTypeRecursive(reference, variableUsagePlainNode)
                                ?? variableDeclarationNode.Type;
                        default:
                            throw new SemanticErrorException(
                                $"Invalid member access, tried to access non record ",
                                variableUsagePlainNode
                            );
                    }
                case VariableUsagePlainNode.VrnExtType.ArrayIndex:
                    switch (variableDeclarationNode.Type)
                    {
                        case ArrayType arrayType:
                            return
                                GetTypeOfExpression(variableUsagePlainNode.Extension!, variables)
                                is IntegerType
                                ? arrayType.Inner
                                : throw new SemanticErrorException(
                                    "Array indexer does not resolve to a number",
                                    variableUsagePlainNode
                                );
                        default:
                            throw new SemanticErrorException(
                                "Invalid array index, tried to index into non array",
                                variableUsagePlainNode
                            );
                    }
                case VariableUsagePlainNode.VrnExtType.Enum:
                    return variableDeclarationNode.Type;
                default:
                    throw new SemanticErrorException(
                        "Could not find type of variable",
                        variableUsagePlainNode
                    );
            }
        }
        switch (node)
        {
            case IntNode:
                return new IntegerType();
            case FloatNode:
                return new RealType();
            case StringNode:
                return new StringType();
            case CharNode:
                return new CharacterType();
            case BoolNode:
                return new BooleanType();
            case BooleanExpressionNode:
                return new BooleanType();
            case MathOpNode mathVal:
                switch (mathVal.Left)
                {
                    case IntNode:
                        return new IntegerType();
                    case FloatNode:
                        return new RealType();
                    case StringNode:
                        return new StringType();
                    case CharNode:
                        return new CharacterType();
                    case VariableUsagePlainNode variableUsage:
                        return GetTypeOfExpression(variableUsage, variables);
                    case MathOpNode:
                        return GetTypeOfExpression(mathVal.Left, variables);
                    case BoolNode:
                        throw new SemanticErrorException(
                            "Could not find a valid type expression",
                            mathVal
                        );
                    case BooleanExpressionNode:
                        throw new SemanticErrorException(
                            "Could not find a valid type expression",
                            mathVal
                        );
                    default:
                        throw new SemanticErrorException(
                            "Could not find a valid type expression",
                            mathVal
                        );
                }
                break;
            default:
                throw new Exception();
        }
    }

    static Type GetVariableType(
        VariableUsageNodeTemp vun,
        Dictionary<string, VariableDeclarationNode> vdnByName
    )
    {
        var vunPlainName = vun.GetPlain().Name;

        if (!vdnByName.TryGetValue(vunPlainName, out var vdn))
        {
            throw new SemanticErrorException($"Variable {vunPlainName} not found", vun);
        }

        vun.GetPlain().ReferencesGlobalVariable = vdn.IsGlobal;
        vun.NewReferencesGlobalVariable = vdn.IsGlobal;
        var vtVis = new VunTypeGettingVisitor(vdn.Type, vdnByName);
        vun.Accept(vtVis);
        return vtVis.VunType;
    }

    private static Type? GetTypeRecursive(
        InstantiatedType targetDefinition,
        VariableUsagePlainNode targetUsage
    )
    {
        if (targetUsage.Extension is null)
            return null;
        var vndt = targetDefinition.Inner.GetMember(
            targetUsage.GetRecordMemberReferenceSafe().Name,
            targetDefinition.InstantiatedGenerics
        )!;

        InstantiatedType? innerVndt = vndt switch
        {
            InstantiatedType r => r,
            ReferenceType(InstantiatedType r1) => r1,
            _ => null
        };
        return innerVndt is { } v
            ? GetTypeRecursive(v, (VariableUsagePlainNode)targetUsage.GetExtensionSafe()) ?? vndt
            : vndt;
    }

    protected static Type ResolveType(
        Type member,
        ModuleNode module,
        List<string> generics,
        Func<string, GenericType> genericCollector
    )
    {
        return member switch
        {
            UnknownType u => ResolveType(u, module, generics, genericCollector),
            ReferenceType(var u) => handleReferenceType(u),
            ArrayType(var u, Range r) => HandleArrayType(u, r),
            _ => member
        };

        Type handleReferenceType(Type type)
        {
            var resolvedType = ResolveType(type, module, generics, genericCollector);
            if (resolvedType is not (RecordType or InstantiatedType or ArrayType or GenericType))
            {
                throw new SemanticErrorException(
                    $"tried to use refersTo (dynamic memory management) on a non record or array type {resolvedType}",
                    module
                );
            }

            return new ReferenceType(resolvedType);
        }

        Type HandleArrayType(Type t, Range r)
        {
            var resolvedType = ResolveType(t, module, generics, genericCollector);
            return new ArrayType(resolvedType, r);
        }
    }

    protected static Type ResolveType(
        UnknownType member,
        ModuleNode module,
        List<string> generics,
        Func<string, GenericType> genericCollector
    )
    {
        var resolveType =
            // TODO: should this be the other way I.E. generics shadow other types
            module.Records.GetValueOrDefault(member.TypeName)?.Type
            ?? (Type?)module.Enums.GetValueOrDefault(member.TypeName)?.EType
            ?? (
                generics.Contains(member.TypeName)
                    ? member.TypeParameters.Count != 0
                        ? throw new SemanticErrorException(
                            $"generics type cannot have generics on it",
                            module
                        )
                        : genericCollector(member.TypeName)
                    : throw new SemanticErrorException($"Unbound type {member}", module)
            );
        if (resolveType is EnumType && member.TypeParameters.Count != 0)
        {
            throw new SemanticErrorException($"Enums do not have generic types", module);
        }
        else if (resolveType is RecordType record)
        {
            if (record.Generics.Count != member.TypeParameters.Count)
            {
                throw new SemanticErrorException(
                    $"not proper amount of types for generics {record.Generics}",
                    module
                );
            }

            var instantiatedGenerics = record
                .Generics.Zip(
                    member.TypeParameters.Select(
                        type => ResolveType(type, module, generics, genericCollector)
                    )
                )
                .ToDictionary();
            resolveType = new InstantiatedType(record, instantiatedGenerics);
        }

        return resolveType;
    }
}

public class ImportVisitor : SAVisitor
{
    private Dictionary<string, ModuleNode> Modules;

    public override ASTNode? Visit(ProgramNode node)
    {
        BuiltInFunctions.Register(node.GetStartModuleSafe().Functions);
        Modules = node.Modules;
        return null;
    }

    public override ASTNode? Visit(ModuleNode node)
    {
        node.UpdateExports();

        // This is looping over the names of the modules from which we're importing.
        foreach (var import in node.getImportNames().Keys)
        {
            if (Modules.ContainsKey(import))
            {
                RecursiveImport(node, Modules[import]);
            }
            else
            {
                throw new SemanticErrorException(
                    "Could not find " + import + " in the list of modules.",
                    node
                );
            }
        }

        foreach (var current in node.getImportNames().Keys)
        {
            if (node.getImportNames()[current].Count == 0)
            {
                var tempList = new LinkedList<string>();
                foreach (string s in Modules[current].getExportNames())
                {
                    tempList.AddLast(s);
                }

                node.getImportNames()[current] = tempList;
            }
        }

        return null;
    }

    private void RecursiveImport(ModuleNode currentModule, ModuleNode otherModule)
    {
        // Here we're passing in ALL of the other module's functions/enums/records, in addition to its actual exports.
        currentModule.UpdateImports(
            Modules[otherModule.getName()].getFunctions(),
            Modules[otherModule.getName()].getEnums(),
            Modules[otherModule.getName()].Records,
            Modules[otherModule.getName()].getExportedFunctions()
        );

        if (Modules[otherModule.getName()].getImportNames().Count > 0)
        {
            foreach (
                string? moduleToBeImported in Modules[otherModule.getName()].getImportNames().Keys
            )
            {
                if (Modules.ContainsKey(moduleToBeImported))
                {
                    RecursiveImport(currentModule, Modules[moduleToBeImported]);
                }
            }
        }
    }
}

public class RecordVisitor : SAVisitor
{
    private ModuleNode module;

    public override ASTNode? Visit(ModuleNode node)
    {
        module = node;
        return null;
    }

    public override ASTNode? Visit(RecordNode node)
    {
        List<string> usedGenerics = [];
        var genericContext = new RecordGenericContext(node.Name, node.GetParentModuleSafe());
        node.Type.Fields = node.Type.Fields.Select(field =>
        {
            return KeyValuePair.Create(
                field.Key,
                ResolveType(
                    field.Value,
                    module,
                    node.GenericTypeParameterNames,
                    generic =>
                    {
                        usedGenerics.Add(generic);
                        return new GenericType(generic, genericContext);
                    }
                )
            );
        })
            .ToDictionary();
        if (!usedGenerics.Distinct().SequenceEqual(node.GenericTypeParameterNames))
        {
            // TODO: make warning function for uniformity
            throw new SemanticErrorException(
                $"Generic Type parameter(s) {string.Join(", ", node.GenericTypeParameterNames.Except(usedGenerics.Distinct()))} are unused for record {node.Name}",
                node
            );
        }
        return null;
    }
}

public class UnknownTypesVisitor : SAVisitor
{
    private ModuleNode currentModule;

    public override ASTNode? Visit(ModuleNode node)
    {
        currentModule = node;
        CheckVariables(
            currentModule.GlobalVariables.Values.ToList(),
            currentModule,
            [],
            new DummyGenericContext()
        );
        return null;
    }

    public override ASTNode? Visit(FunctionNode node)
    {
        var genericContext = new FunctionGenericContext(
            node.Name,
            currentModule.Name,
            node.Overload
        );
        CheckVariables(
            node.LocalVariables,
            currentModule,
            node.GenericTypeParameterNames,
            genericContext
        );
        node.LocalVariables.ForEach(vdn => OutputHelper.DebugPrintJson(vdn, vdn.Name ?? "null"));
        var generics = node.GenericTypeParameterNames;
        List<string> usedGenerics = [];
        foreach (var variable in node.ParameterVariables)
        {
            // find the type of each parameter, and also see what generics each parameter uses
            variable.Type = ResolveType(
                variable.Type,
                currentModule,
                generics,
                generic =>
                {
                    usedGenerics.Add(generic);
                    return new GenericType(generic, genericContext);
                }
            );
        }

        // if not all generics are used in the parameters that means those generics cannot be inferred, but they could be used for variables which is bad
        if (!usedGenerics.Distinct().SequenceEqual(generics))
        {
            throw new SemanticErrorException(
                $"Generic Type parameter(s) {string.Join(", ", generics.Except(usedGenerics.Distinct()))}  cannot be inferred for function {node.Name}",
                node
            );
        }
        return null;
    }

    private void CheckVariables(
        List<VariableDeclarationNode> variables,
        ModuleNode currentModule,
        List<string> generics,
        GenericContext genericInfo
    )
    {
        foreach (var variable in variables)
        {
            // if it's a constant then it cannot refer to another constant/variable so the only case for variable is it's an enum constant
            // might need  similar logic for default values of functions, and weird enum comparisons i.e. red = bar, where red is an enum constant
            // because currently we do assume lhs determine type
            if (variable is { IsConstant: true, InitialValue: { } init })
            {
                // from the parsers pov we should make an enum node, b/c this can't be any random variable
                if (init is StringNode n)
                {
                    foreach (
                        var enumDefinition in currentModule.Enums.Values.Concat(
                            currentModule.Imported.Values
                        )
                    )
                    {
                        if (enumDefinition is EnumNode e)
                        {
                            if (e.EType.Variants.Contains(n.Value))
                            {
                                variable.Type = e.EType;
                                break;
                            }
                        }
                    }

                    if (variable.Type is not EnumType)
                    {
                        variable.Type = new StringType();
                    }
                }
                else
                {
                    variable.Type = GetTypeOfExpression(init, []);
                }
            }
            else
            {
                variable.Type = ResolveType(
                    variable.Type,
                    currentModule,
                    generics,
                    generic => new GenericType(generic, genericInfo)
                );
            }
        }
    }
}

public class TestVisitor : SAVisitor
{
    private String ModuleName;
    private Dictionary<string, CallableNode> Functions;

    public override ASTNode? Visit(ModuleNode node)
    {
        ModuleName = node.Name;
        Functions = node.OriginalFunctions;
        return null;
    }

    public override ASTNode? Visit(TestNode node)
    {
        if (Functions.ContainsKey(node.targetFunctionName))
        {
            ((FunctionNode)Functions[node.targetFunctionName]).Tests.Add(node.Name, node);
        }
        else
        {
            throw new SemanticErrorException(
                $"Could not find the function {node.targetFunctionName} in the module {ModuleName} to be tested."
            );
        }
        return null;
    }
}

public class MathOpNodeVisitor : SAVisitor
{
    private Dictionary<string, VariableDeclarationNode> Variables;

    public override ASTNode? Visit(FunctionNode node)
    {
        Variables = node.VariablesInScope;
        return null;
    }

    public override ASTNode? PostWalk(MathOpNode node)
    {
        if (
            GetTypeOfExpression(node.Left, Variables).GetType()
            != GetTypeOfExpression(node.Right, Variables).GetType()
        )
        {
            throw new SemanticErrorException(
                "Math expressions require the same types on both sides.",
                node
            );
        }
        return null;
    }
}

public class BooleanExpectedVisitor : SAVisitor
{
    private Dictionary<string, VariableDeclarationNode> Variables;

    public override ASTNode? Visit(FunctionNode node)
    {
        Variables = node.VariablesInScope;
        return null;
    }

    public override ASTNode? Visit(IfNode node)
    {
        switch (node.Expression)
        {
            case BooleanExpressionNode:
                return null;
            case BoolNode:
                return null;
            case VariableUsageNodeTemp variableUsageNodeTemp:
                if (
                    GetTypeOfExpression(variableUsageNodeTemp, Variables).GetType()
                    != typeof(BooleanType)
                )
                    throw new SemanticErrorException(
                        "Cannot use a non boolean variable in an if statement.",
                        variableUsageNodeTemp
                    );
                return null;
            default:
                throw new SemanticErrorException("Boolean expression expected.", node);
        }
    }

    public override ASTNode? Visit(WhileNode node)
    {
        return null;
    }

    public override ASTNode? Visit(RepeatNode node)
    {
        return null;
    }
}

public class VariableDeclarationVisitor : SAVisitor
{
    private Dictionary<string, VariableDeclarationNode> Variables;

    public override ASTNode? Visit(FunctionNode node)
    {
        foreach (var local in node.LocalVariables)
        {
            foreach (var parameter in node.ParameterVariables)
            {
                if (parameter.Name.Equals(local.Name))
                {
                    throw new SemanticErrorException(
                        $"The variable, {local.Name} has already been created.",
                        local
                    );
                }
            }
        }

        return null;
    }
}

public class AssignmentVisitor : SAVisitor
{
    private Dictionary<string, VariableDeclarationNode> Variables;

    public override ASTNode? Visit(FunctionNode node)
    {
        Variables = node.VariablesInScope;
        return null;
    }

    public override ASTNode? Visit(AssignmentNode node)
    {
        // Control flow reroute for vuop testing.
        if (GetVuopTestFlag())
        {
            if (Variables.TryGetValue(node.NewTarget.GetPlain().Name, out var targetDeclaration))
            {
                if (targetDeclaration.IsConstant)
                {
                    throw new SemanticErrorException(
                        $"Variable {node.Target.Name} is not mutable, you cannot assign to it.",
                        node
                    );
                }

                node.NewTarget.GetPlain().ReferencesGlobalVariable = targetDeclaration.IsGlobal;
            }

            var targetType = Variables[node.NewTarget.GetPlain().Name].Type;
            NewCheckAssignment(
                node.NewTarget.GetPlain().Name,
                targetType,
                node.Expression,
                Variables,
                node.NewTarget
            );
        }
        else
        {
            if (Variables.TryGetValue(node.Target.Name, out var targetDeclaration))
            {
                if (targetDeclaration.IsConstant)
                {
                    throw new SemanticErrorException(
                        $"Variable {node.Target.Name} is not mutable, you cannot assign to it.",
                        node
                    );
                }

                node.Target.ReferencesGlobalVariable = targetDeclaration.IsGlobal;
            }

            var targetType = GetTypeOfExpression(node.Target, Variables);
            CheckAssignment(node.Target.Name, targetType, node.Expression, Variables);
        }
        return null;
    }

    private static void CheckAssignment(
        string targetName,
        Type targetType,
        ExpressionNode expression,
        Dictionary<string, VariableDeclarationNode> variables
    )
    {
        CheckRange(targetName, targetType, expression, variables);
        // if(targetType )

        if (
            targetType is EnumType e
            && expression is VariableUsagePlainNode v
            && e.Variants.Contains(v.Name)
        )
        {
            if (v.ExtensionType != VariableUsagePlainNode.VrnExtType.None)
            {
                throw new SemanticErrorException($"ambiguous variable name {v.Name}", expression);
            }

            v.ExtensionType = VariableUsagePlainNode.VrnExtType.Enum;
            v.Extension = new IntNode(e.Variants.IndexOf(v.Name));
            v.ReferencesGlobalVariable = true;
        }
        else
        {
            var expressionType = GetTypeOfExpression(expression, variables);
            if (!targetType.Equals(expressionType))
            {
                throw new SemanticErrorException(
                    $"Type mismatch cannot assign to {targetName}: {targetType} {expression}: {expressionType}",
                    expression
                );
            }
        }
    }

    private static void NewCheckAssignment(
        string targetName,
        Type targetType,
        ExpressionNode expression,
        Dictionary<string, VariableDeclarationNode> vDecs,
        VariableUsageNodeTemp target
    )
    {
        var vtVis = new VunTypeGettingVisitor(targetType, vDecs);
        target.Accept(vtVis);

        var expressionType = GetTypeOfExpression(expression, vDecs);
        if (!vtVis.VunType.Equals(expressionType))
        {
            throw new SemanticErrorException(
                "Type mismatch; cannot assign `"
                    + expression
                    + " : "
                    + expressionType
                    + "' to `"
                    + targetName
                    + " : "
                    + targetType
                    + "'.",
                expression
            );
        }
    }

    private static void CheckRange(
        String? variable,
        Type targetType,
        ASTNode expression,
        Dictionary<string, VariableDeclarationNode> variablesLookup
    )
    {
        if (targetType is RangeType i) // all other i range type are bounded by integers
        {
            var from = i.Range.From;
            var to = i.Range.To;
            int upper = (int)GetMaxRange(expression, variablesLookup);
            int lower = (int)GetMinRange(expression, variablesLookup);

            if (lower < from || upper > to)
                throw new Exception(
                    $"The variable {variable!} can only be assigned expressions that wont overstep its range ({from}..{to}), but attempted to assign to expression {expression} with range ({lower}..{upper}."
                );
        }
    }

    private static float GetMaxRange(
        ASTNode node,
        Dictionary<string, VariableDeclarationNode> variables
    )
    {
        if (node is MathOpNode mon)
        {
            switch (mon.Op)
            {
                case MathOpNode.MathOpType.Plus:
                    return GetMaxRange(mon.Left, variables) + GetMaxRange(mon.Right, variables);
                case MathOpNode.MathOpType.Minus:
                    return GetMaxRange(mon.Left, variables) - GetMinRange(mon.Right, variables);
                case MathOpNode.MathOpType.Times:
                    return GetMaxRange(mon.Left, variables) * GetMaxRange(mon.Right, variables);
                case MathOpNode.MathOpType.Divide:
                    return GetMinRange(mon.Left, variables) / GetMaxRange(mon.Right, variables);
                case MathOpNode.MathOpType.Modulo:
                    return GetMaxRange(mon.Right, variables) - 1;
            }
        }

        if (node is IntNode i)
            return i.Value;
        if (node is FloatNode f)
            return f.Value;
        if (node is StringNode s)
            return s.Value.Length;
        if (node is VariableUsagePlainNode vrn)
        {
            var dataType = GetTypeOfExpression(vrn, variables);
            if (dataType is RangeType t)
            {
                return t.Range.To;
            }

            throw new Exception("Ranged variables can only be assigned variables with a range.");
        }

        throw new Exception(
            "Unrecognized node type on line "
                + node.Line
                + " in math expression while checking range"
        );
    }

    private static float GetMinRange(
        ASTNode node,
        Dictionary<string, VariableDeclarationNode> variables
    )
    {
        if (node is MathOpNode mon)
        {
            switch (mon.Op)
            {
                case MathOpNode.MathOpType.Plus:
                    return GetMinRange(mon.Left, variables) + GetMinRange(mon.Right, variables);
                case MathOpNode.MathOpType.Minus:
                    return GetMinRange(mon.Left, variables) - GetMaxRange(mon.Right, variables);
                case MathOpNode.MathOpType.Times:
                    return GetMinRange(mon.Left, variables) * GetMinRange(mon.Right, variables);
                case MathOpNode.MathOpType.Divide:
                    return GetMaxRange(mon.Left, variables) / GetMinRange(mon.Right, variables);
                case MathOpNode.MathOpType.Modulo:
                    return 0;
            }
        }

        if (node is IntNode i)
            return i.Value;
        if (node is FloatNode f)
            return f.Value;
        if (node is StringNode s)
            return s.Value.Length;
        if (node is VariableUsagePlainNode vrn)
        {
            var dataType = GetTypeOfExpression(vrn, variables);
            if (dataType is RangeType t)
            {
                return t.Range.From;
            }

            throw new Exception("Ranged variables can only be assigned variables with a range.");
        }

        throw new Exception("Unrecognized node type in math expression while checking range");
    }
}

public class InfiniteLoopVisitor : SAVisitor
{
    private Stack<Dictionary<string, VariableUsagePlainNode>> LoopDeclarationStack =
        new Stack<Dictionary<string, VariableUsagePlainNode>>();
    private Stack<Dictionary<string, VariableUsagePlainNode>> LoopStatementStack =
        new Stack<Dictionary<string, VariableUsagePlainNode>>();

    private bool inLoopDeclaration;
    private bool inLoopStatements;

    public override ASTNode? Visit(VariableUsagePlainNode node)
    {
        if (inLoopDeclaration)
        {
            if (!LoopDeclarationStack.Peek().ContainsKey(node.Name))
                LoopDeclarationStack.Peek().Add(node.Name, node);
        }
        else if (inLoopStatements)
        {
            if (!LoopStatementStack.Peek().ContainsKey(node.Name))
                LoopStatementStack.Peek().Add(node.Name, node);
        }

        return null;
    }

    public override ASTNode? Visit(ForNode node)
    {
        if (inLoopDeclaration)
        {
            inLoopDeclaration = false;
            inLoopStatements = true;
        }
        return null;
    }

    public override ASTNode? Visit(AssignmentNode node)
    {
        if (inLoopDeclaration)
        {
            inLoopDeclaration = false;
            inLoopStatements = true;
        }
        return null;
    }

    public override ASTNode? Visit(FunctionCallNode node)
    {
        if (inLoopDeclaration)
        {
            inLoopDeclaration = false;
            inLoopStatements = true;
        }
        return null;
    }

    public override ASTNode? Visit(IfNode node)
    {
        if (inLoopDeclaration)
        {
            inLoopDeclaration = false;
            inLoopStatements = true;
        }
        return null;
    }

    public override ASTNode? Visit(WhileNode node)
    {
        LoopDeclarationStack.Push(new Dictionary<string, VariableUsagePlainNode>());
        LoopStatementStack.Push(new Dictionary<string, VariableUsagePlainNode>());
        inLoopDeclaration = true;
        return null;
    }

    public override ASTNode? PostWalk(WhileNode node)
    {
        bool isInfinite = true;
        foreach (var variable in LoopDeclarationStack.Peek().Keys)
        {
            if (LoopStatementStack.Peek().ContainsKey(variable))
            {
                isInfinite = false;
                break;
            }
        }

        if (LoopDeclarationStack.Peek().Count == 0)
        {
            throw new SemanticErrorException(
                "This Loop is infinite because it does not use a variable in the loop declaration.",
                node
            );
        }

        if (isInfinite)
        {
            throw new SemanticErrorException(
                "This Loop is infinite because it does not use the variable in the loop statements.",
                node
            );
        }

        LoopDeclarationStack.Pop();
        var loopStatements = LoopStatementStack.Pop();
        if (LoopStatementStack.Count != 0)
        {
            foreach (var loop in loopStatements)
            {
                if (!LoopStatementStack.Peek().ContainsKey(loop.Key))
                {
                    LoopStatementStack.Peek().Add(loop.Key, loop.Value);
                }
            }
        }
        if (LoopStatementStack.Count == 0)
            inLoopStatements = false;
        return null;
    }

    public override ASTNode? Visit(RepeatNode node)
    {
        LoopDeclarationStack.Push(new Dictionary<string, VariableUsagePlainNode>());
        LoopStatementStack.Push(new Dictionary<string, VariableUsagePlainNode>());
        inLoopDeclaration = true;
        return null;
    }

    public override ASTNode? PostWalk(RepeatNode node)
    {
        bool isInfinite = true;
        foreach (var variable in LoopDeclarationStack.Peek().Keys)
        {
            if (LoopStatementStack.Peek().ContainsKey(variable))
            {
                isInfinite = false;
                break;
            }
        }

        if (isInfinite)
        {
            throw new SemanticErrorException(
                "This Loop is infinite because it does not use a variable in the loop.",
                node
            );
        }

        LoopDeclarationStack.Pop();
        LoopStatementStack.Pop();
        if (LoopStatementStack.Count == 0)
            inLoopStatements = false;
        return null;
    }
}

public class ForNodeVisitor : SAVisitor
{
    private Dictionary<string, VariableDeclarationNode> Variables;

    public override ASTNode? Visit(FunctionNode node)
    {
        Variables = node.VariablesInScope;
        return null;
    }

    public override ASTNode? Visit(ForNode node)
    {
        var iterationVariable = GetVuopTestFlag() ? node.NewVariable : node.Variable;
        var typeOfIterationVariable = GetVuopTestFlag()
            ? GetTypeOfExpression(node.NewVariable, Variables)
            : GetTypeOfExpression(node.Variable, Variables);
        if (Variables[iterationVariable.GetPlain().Name].IsConstant)
        {
            throw new SemanticErrorException(
                $"cannot iterate in a for loop with variable {iterationVariable} as it is not declared mutable",
                iterationVariable
            );
        }

        if (typeOfIterationVariable is not IntegerType)
        {
            throw new SemanticErrorException(
                $"For loop has a non-integer index called {node.Variable.Name}. This is not allowed.",
                node.Variable
            );
        }

        if (GetTypeOfExpression(node.From, Variables) is not IntegerType)
        {
            throw new SemanticErrorException(
                $"For loop must have its ranges be integers!",
                node.From
            );
        }

        if (GetTypeOfExpression(node.To, Variables) is not IntegerType)
        {
            throw new SemanticErrorException(
                $"For loop must have its ranges be integers!",
                node.To
            );
        }

        return null;
    }
}

public class BooleanExpressionNodeVisitor : SAVisitor
{
    protected Dictionary<string, VariableDeclarationNode> Variables;

    public override ASTNode? Visit(FunctionNode node)
    {
        Variables = node.VariablesInScope;
        return null;
    }

    public override ASTNode? Visit(BooleanExpressionNode node)
    {
        if (
            GetTypeOfExpression(node.Left, Variables).GetType()
            != GetTypeOfExpression(node.Right, Variables).GetType()
        )
        {
            throw new SemanticErrorException(
                "Cannot compare expressions of different types.",
                node
            );
        }
        return null;
    }
}

public class NewFunctionCallVisitor : FunctionCallVisitor
{
    public override ASTNode? Visit(FunctionCallNode node)
    {
        // look up the function
        var function =
            Functions.GetValueOrDefault(node.Name)
            ?? BuiltIns.GetValueOrDefault(node.Name)
            ?? throw new SemanticErrorException($"Function {node.Name} not found", node);
        CheckFunctionCall(node, function, Variables);
        return null;
    }

    // Check if a function call is valid (arguments match parameters, there is a correct overload, ...).
    // This is public so old Semantic Analysis can still function, but this semantic analysis pass does not need to be update in multiple places each time something need to be changed.
    public static void CheckFunctionCall(
        FunctionCallNode functionCallNode,
        CallableNode function,
        Dictionary<string, VariableDeclarationNode> variables
    )
    {
        functionCallNode.FunctionDefinitionModule = function.parentModuleName;
        switch (function)
        {
            // If it's a call to an overloaded function.
            case OverloadedFunctionNode overloads:
            {
                // For example:
                // define coerce(var target: integer; value: integer)
                //      target := value
                // define coerce(var target: integer; value: real)
                //      realToInteger value, var target
                // define coerce(var target: integer; value: boolean)
                //      if value then
                //          target := 0
                //      else
                //          target := 1
                // define coerce(var target: A; value: A) generic A
                //      target := value
                // define start()
                // variables value: integer
                //      coerce var value, false
                // We try each overload, splitting the results into a list of correct overloads, and a list of bad overloads.
                var possibleOverloads = overloads.Overloads.Values.AggregateEither(
                    overload =>
                        CheckFunctionCallInner(functionCallNode, overload, variables)
                            .Map(correct => (overload, correct))
                            .MapRight(error => (overload, error))
                );
                var overload = possibleOverloads.Left.ToList() switch
                {
                    // if there is only a single overload, then great we found the correct overload.
                    // This is what happens in our example.
                    [var validOverload]
                        => validOverload,
                    // if there are no valid overloads then print all the bad overloads and why they were bad
                    // Lets say that instead of calling coerce with a boolean we called it with a string `coerce var value, ""`.
                    // So we would get an error:
                    // No overload for call to coerce, with arguments integer, string.
                    // Overload with parameters integer, integer mismatched because: type mismatch.
                    // ...
                    // Overload with parameters A, A mismatched because: generic A cannot match integer and string
                    []
                        => throw new SemanticErrorException(
                            $"""
                                 No overload found for call to {functionCallNode.Name}, with arguments {functionCallNode.Arguments.Select(arg => arg.Type).ToString(open: "(", close: ")")}.
                                 {possibleOverloads.Right.ToString(badOverload => $"Overload {functionCallNode.Name}{badOverload.overload.ParameterVariables.Select(parameter => parameter.Type).ToString(open: "(", close: ")")} mismatched because: {badOverload.Item2}\n.", delimiter: "", open: "", close: "")}
                                 """,
                            functionCallNode
                        ),
                    // if there are many overloads that are correct, then we cannot determine which one is the most correct, so we spit out the list of all the valid overloads
                    // (back to our example) If we had called coerce with integer `coerce var value, 5`.
                    // We would get the error:
                    // Ambiguous overload for call to coerce, with arguments integer, string.
                    // Possible valid overloads included:
                    // Overload with parameters integer, integer.
                    // Overload with parameters A, A.
                    var validOverloads
                        => throw new SemanticErrorException(
                            $"""
                                Ambiguous overloads for call to {functionCallNode.Name}, with arguments {functionCallNode.Arguments.Select(arg => arg.Type).ToString(open: "(", close: ")")}.
                                Possible valid overloads included:
                                {validOverloads.ToString(overload => $"Overload {functionCallNode.Name}{overload.overload.ParameterVariables.Select(parameter => parameter.Type).ToString(open: "(", close: ")")}.\n", delimiter: "", open: "", close: "")}
                                """,
                            functionCallNode
                        )
                };
                // If we had any generics we mark down what each generic corresponds to (called a substitution).
                functionCallNode.InstantiatedGenerics = overload.Item2;
                // We then mark down the correct overload, so we don't have to do this again.
                functionCallNode.Overload = overload.Item1.Overload;
                break;
            }
            case BuiltInVariadicFunctionNode variadicFunction:
            {
                // So for our example this time:
                // enum colors = [red, green, blue]
                // define start()
                // variables a: integer
                //      write var a, 4, 5
                //      write red
                //      write "hello", "world"
                // Note: write's parameters must be immutably passed.
                // if it's a variadic function there are to cases: either all the arguments have to be var or not
                if (!variadicFunction.AreParametersConstant) // hack to verify that all parameters are passed in as var
                // We don't have any such functions, so we have no example for this case.
                {
                    if (
                        functionCallNode.Arguments.Find(
                            node =>
                                node
                                    is not VariableUsageNodeTemp
                                    {
                                        // Verifying that the argument is marked as var.
                                        NewIsInFuncCallWithVar: true

                                        // Since we know that the argument is var, we now verify that the actual variable the argument refersTo is also mutable.
                                    }
                                && !variables[
                                    ((VariableUsageNodeTemp)node).GetPlain().Name
                                ].IsConstant
                        ) is
                        { } badArgument
                    )
                    {
                        // TODO: better message, if the problem is the underlying variable not being mutable.
                        throw new SemanticErrorException(
                            $"Cannot call builtin variadic {variadicFunction.Name} with non var argument {badArgument}.",
                            functionCallNode
                        );
                    }
                }
                // In our example the first call to write fails here, because we pass in `a` as mutable.
                else if (
                    // Verifying that the arguments are not marked as var.
                    // By finding any arguments that are marked as var, and if there giving an error.
                    functionCallNode.Arguments.Find(
                        node => node is VariableUsageNodeTemp { NewIsInFuncCallWithVar: true }
                    ) is
                    { } badArgument
                )
                {
                    throw new SemanticErrorException(
                        $"Cannot call builtin variadic {variadicFunction.Name} with var argument {badArgument}.",
                        functionCallNode
                    );
                }
                // We mark the function call as being a call to this function with these argument types.
                // Our second call to start fails because although the users intention was to use the enum constant red, enum special casing does not work unless the parameter type is an enum.
                // But our third call would succeed, and we would store for monomorphization in InstantiatedVariadics on the function call [string, string].
                functionCallNode.InstantiatedVariadics = functionCallNode
                    .Arguments.Select(arg => GetTypeOfExpression(arg, variables))
                    .ToList();
                break;
            }
            default:
                // We will use this example code:
                // record HashMap generic K, V
                //      key: K
                //      value: V
                //      next: refersTo HashMap K, V
                // define AddToHashMap(key: K, value: V, var map: refersTo HashMap K, V) generic K, V
                // variables newNode: refersTo HashMap K, V
                //      allocateMemory var newNode
                //      newNode.key := key
                //      newNode.value := value
                //      newNode.next := map
                //      map := newNode
                // define Swap(var a: A, var b: A) generic A
                // variables temp: A
                //      temp := a
                //      a := b
                //      b := temp
                // define BoolToString(b: boolean, var result: string)
                //      if b
                //          result := "true"
                //      else
                //          result := "false"
                // define Greeter(name: string, greeting: string = "Hello", grammar: string = "!" )
                //      write greeting, (name + grammar)
                //
                // define start()
                // variables map: HashMap string, number
                // variables boolString: string
                // variables x, y: integer
                //      AddToHashMap "first", false, var map { case 1}
                //      Swap var x, var y { case 2 }
                //      Greeter "John", "Goodbye" { case 3 }
                //      BoolToString 4, var boolString { case 4 }
                //      { TODO: invalid arity case }
                //      { TODO: var mismatch case }
                //      { TODO: enum special casing }
                //
                // Note: we will only be considering the call in start.
                functionCallNode.InstantiatedGenerics = CheckFunctionCallInner(
                        functionCallNode,
                        function,
                        variables
                    )
                    .OrElseThrow(message => new SemanticErrorException(message, functionCallNode));
                // At the end the call to AddToHashMap will fail with `generic V cannot match both integer and boolean.
                // The Swap call is ok, and we will mark for monomorphization that we have a substitution(InstantiatedGenerics) [A=integer].
                // The call to Greeter is ok, and does not give us any substitution.
                // The call to BoolToString fails with `types boolean and integer do not match`.
                break;
        }
    }

    private static Either<Dictionary<string, Type>, string> CheckFunctionCallInner(
        FunctionCallNode functionCallNode,
        CallableNode fn,
        Dictionary<string, VariableDeclarationNode> variables
    )
    {
        var args = functionCallNode.Arguments;
        functionCallNode.FunctionDefinitionModule = fn.parentModuleName!;
        if (CheckArity(fn, args))
            return Either<Dictionary<string, Type>, string>.Right(
                "For function "
                    + fn.Name
                    + ", "
                    + args.Count
                    + " parameters were passed in, but "
                    + fn.ParameterVariables.Count
                    + " are required."
            );
        // We then go through all the (parameter, argument) pairs, but we skip any unnecessary default parameters (as we have already done the arity check), and typecheck them.
        // In our first case we would have [(key: K, "first"), (value: V, false), (map: HashMap K, V, map: HashMap string, number)].
        // For the second case we have [(var a: A, var x: integer), (var b: A, var y: integer)].
        // The third case is the most interesting as we don't include the last parameter, we get [(name: string, "John"), (greeting: string, "Goodbye")].
        // With the fourth case we get [(b: boolean, 4), (var result: string, var boolString: string)].
        var selectMany = fn.ParameterVariables.Take(args.Count)
            .Zip(args)
            .TryAll(paramAndArg =>
            {
                var param = paramAndArg.First;
                var argument = paramAndArg.Second;
                // For each parameter and argument we check that their mutability matches (their varness).
                var parameterMutability = CheckParameterMutability(
                    param,
                    argument,
                    variables,
                    functionCallNode
                );
                return parameterMutability.HasValue
                    ? Either<IEnumerable<(string, Type)>, string>.Right(
                        parameterMutability.ValueOr("")
                    )
                    :
                    // We then infer any generics while making sure that the types of the parameter and argument match.
                    // We return the mapping of any inferred generics to their actual type.
                    TypeCheckAndInstantiateGenericParameter(param, argument, variables, fn);
                // each argument can return substitutions for many generics, so we mush them all into one list.
            })
            .Map(substitution => substitution.SelectMany(x => x))
            // We use distinct in case two (or more) parameters give us back the same generic substitution.
            // Note: if two (or more) parameters give back different substitutions they are not distinct.
            .Map(Enumerable.Distinct);

        return
        // We then group the substitutions by the generic type they are substituting for, and if any of these substitution groups has more than one substitution it means that we have two different substitutions for the same generic, so the generic cannot match both of them, so we give an error.
        selectMany.FlatMap(
            inferred =>
                inferred.GroupBy(pair => pair.Item1).FirstOrDefault(group => group.Count() > 1)
                    is { } bad
                    ? Either<Dictionary<string, Type>, string>.Right(
                        $"Generic {bad.Key} cannot match {string.Join(" and ", bad.Select(ty => ty.Item2))}."
                    )
                    : Either<Dictionary<string, Type>, string>.Left(inferred.ToDictionary())
        );
    }

    // Checks that the number of arguments `matches` the number of parameters, returning false if they do not match.
    private static bool CheckArity(CallableNode fn, List<ExpressionNode> args)
    {
        // we cannot just autofill all the default values yet, because what if it is an invalid overload
        // so what we do is we trim any extra default parameters out of the parameter count
        // Back to our example:
        // For case 1: We have 3 arguments, and 3 parameters (none of which are default parameters).
        // For the left hand side of the subtraction we first skip the first 3 parameters yielding us a count of 0, since there is no default parameters, we have 3 == 3 - 0, which is good.
        // For case 2: We have 2 arguments, and 2 parameters (none of which are default parameters).
        // For the left hand side of the subtraction we first skip the first 2 parameters yielding us a count of 0, since there is no default parameters, we have 2 == 2 - 0, which is good.
        // Case 3 is the most interesting here: we have 2 arguments, and 3 parameters with two of the being default parameters.
        // So skipping us the first 2 parameters yields us a list of one parameter, we know that this is a default parameters, so we keep it yielding us again a count of one, so we have 2 == 3 - 1, which is good.
        // However, if we would've called Greeting with no arguments, we would have well zero arguments, and 3 parameters with two of the being default parameters.
        // We would skip none of the parameters for the left hand side of the subtraction, and then only count the default ones, yielding us 2, 0 != 3 - 2, which is bad, so we give the error.
        // For case 4: We have 2 arguments, and 2 parameters (none of which are default parameters).
        // For the left hand side of the subtraction we first skip the first 2 parameters yielding us a count of 0, since there is no default parameters, we have 2 == 2 - 0, which is good.
        return args.Count
            != fn.ParameterVariables.Count
                - fn.ParameterVariables.Skip(args.Count)
                    .Count(parameter => parameter.IsDefaultValue);
    }

    private static Either<
        IEnumerable<(string, Type)>,
        string
    > TypeCheckAndInstantiateGenericParameter(
        VariableDeclarationNode param,
        ExpressionNode argument,
        Dictionary<string, VariableDeclarationNode> variables,
        CallableNode fn
    )
    {
        // check that the argument passed in has the right type for its parameter
        // and also if the parameter has any generics try to instate them
        try
        {
            SemanticAnalysis.CheckRange(param.Name!, param.Type, argument, variables);
        }
        catch (SemanticErrorException error)
        {
            return Either<IEnumerable<(string, Type)>, string>.Right(error.Message);
        }

        if (
            param.Type is EnumType e
            && argument is VariableUsagePlainNode v
            && e.Variants.Contains(v.Name)
        )
        {
            // Enum special casing
            if (variables.ContainsKey(v.Name))
            {
                return Either<IEnumerable<(string, Type)>, string>.Right(
                    $"Ambiguous variable name {v.Name}."
                );
            }

            argument.Type = param.Type;
            return Either<IEnumerable<(string, Type)>, string>.Left([]);
        }

        var expressionType = GetTypeOfExpression(argument, variables);
        argument.Type = argument.Type is DefaultType ? expressionType : argument.Type;
        return !param.Type.Equals(expressionType)
            ?
            // Infer generics.
            MatchTypes(param.Type, expressionType)
                .MapRight(
                    mismatch =>
                        $"Type mismatch cannot pass to {param.Name!}: {param.Type} {argument}: {expressionType}, because: {mismatch}."
                )
            : Either<IEnumerable<(string, Type)>, string>.Left([]);

        // Match types given two types (the first is the parameter type, the second is the argument type)
        // Attempts to find a substitution for the generics in the parameter type to new types,
        // given the fact that the parameter type is a more specific version of the argument type.
        // You may wonder why we return Option as opposed to throwing an exception?
        // It is because we don't want the error when we have a type error to be about some nested type, but rather about a whole parameter argument type mismatch.
        // Another approach would be to throw an exception and then catch it in TypeCheckAndInstantiateGenericParameter and throw a new error there that says the parameter and argument type do not match, and use the original error as the reason.
        Either<IEnumerable<(string, Type)>, string> MatchTypes(Type paramType, Type type)
        {
            return (paramType, type) switch
            {
                // If we have some generic type we create a substitution that maps the generic type to the type (base case).
                (GenericType g, _) => MatchTypesGeneric(g, type),

                // You cannot pass an argument of a more general type that corresponds to a parameter with a more specific type.
                // We know this is a more general argument because the case above catches all generic parameters
                // (even ones with a generic argument).
                (_, GenericType)
                    => Either<IEnumerable<(string, Type)>, string>.Right(
                        $"The {paramType} is more specific than {type}."
                    ),

                // If we have a reference type and another reference type we just unify their inner types.
                (ReferenceType param, ReferenceType arg) => MatchTypes(param.Inner, arg.Inner),

                // An instantiated type holds two things: the actual types and the actual types for any generics in the actual types.
                // To find the substitution for two instantiated types, we first verify that they are the same underlying type.
                // This verification is what the `when ... Equals ...` below is about.
                (InstantiatedType param, InstantiatedType arg) when arg.Inner.Equals(param.Inner)
                    => MatchTypesInstantiated(param, arg),

                // TODO: validate ranges
                // Same as ReferenceType but for arrays.
                (ArrayType param, ArrayType arg) => MatchTypes(param.Inner, arg.Inner),

                ({ } a, { } b)
                    => Either<IEnumerable<(string, Type)>, string>
                        .Left([])
                        .Filter(a.Equals(b), $"Types {a} and {b} do not match.")
            };
        }

        Either<IEnumerable<(string, Type)>, string> MatchTypesGeneric(GenericType g, Type type) =>
            Either<IEnumerable<(string, Type)>, string>.Left(Enumerable.Repeat((g.Name, type), 1));

        // Inferring generics and type checking records.
        Either<IEnumerable<(string, Type)>, string> MatchTypesInstantiated(
            InstantiatedType paramType,
            InstantiatedType type
        ) =>
            // We do not care about the record fields, we only care about any generics that the record has (and the actual types the user put for each generic, called InstantiatedGenerics or the substitutions for the record).
            // Another way to think about this is to look at a few types:
            // record person
            //      name: string
            //      age: integer
            // Two person types will always be equal. Compare this to this type:
            // record LinkedList generic A
            //      data: A
            //      next: refersTo LinkedList A
            // In this case two different LinkedList can be different if the generic type A is instantiated with a different type, for example LinkedList integer and LinkedList real.
            // What stays the same is the fields, this is why we just check the InstantiatedGenerics.
            // You may ask why don't we check that the two underlying records are the same, the answer is we do that in MatchTypes with the `when` clause under the InstantiatedType case.
            paramType
                .InstantiatedGenerics.Values.Zip(type.InstantiatedGenerics.Values)
                .Select(pair => MatchTypes(pair.Item1, pair.Item2))
                // Joining the results together a bit complicated because of the Option.
                // What happens is we see if the first result is ok (with the flatmap, and if so we check if the second result is ok if it is we join the first and the second result, in any other case we return none (meaning this part of the type failed to type check).
                .Aggregate((first, second) => first.FlatMap(f => second.Map(f.Union)));
    }

    // assumptions if the argument is a variable it assumed to be there already from previous check in check function call
    // This function is a bit weird in that if there is a failure it will return Option.Some, and in the case where everything is ok it will return Option.None
    // Usually we think of None as being the bad case, but not here.
    private static Option<string> CheckParameterMutability(
        VariableDeclarationNode param,
        ExpressionNode argument,
        Dictionary<string, VariableDeclarationNode> variables,
        FunctionCallNode fn
    )
    {
        // check that the argument passed in has the right type of mutability for its parameter
        if (argument is VariableUsageNodeTemp variableUsageNodeTemp)
        {
            var lookedUpArgument = variables[variableUsageNodeTemp.GetPlain().Name];
            if (!variableUsageNodeTemp.NewIsInFuncCallWithVar && !param.IsConstant)
            {
                return Option.Some(
                    "Cannot pass non var argument when you annotate an argument var."
                );
            }

            if (param.IsConstant && variableUsageNodeTemp.NewIsInFuncCallWithVar)
            {
                return Option.Some(
                    "Unused var annotation: argument marked as var, but parameter is not marked as var."
                );
            }

            if (
                !param.IsConstant
                && variableUsageNodeTemp.NewIsInFuncCallWithVar
                && lookedUpArgument.IsConstant
            )
            {
                return Option.Some(
                    "You tried to annotate an argument var, even though the variable referenced was not mutable."
                );
            }
        }
        else if (!param.IsConstant)
        {
            return Option.Some(
                "Cannot pass non var argument when function is expecting it to be var."
            );
        }
        return Option.None<string>();
    }
}

public class FunctionCallVisitor : SAVisitor
{
    protected Dictionary<string, CallableNode> Functions;

    protected Dictionary<string, VariableDeclarationNode> Variables;

    protected Dictionary<string, CallableNode> BuiltIns;

    public override ASTNode? Visit(ModuleNode node)
    {
        Functions = node.OriginalFunctions;
        foreach (var import in node.Imported)
        {
            if (!Functions.ContainsKey(import.Key))
            {
                Functions.Add(import.Key, (CallableNode)import.Value);
            }
        }

        BuiltIns = new Dictionary<string, CallableNode>();
        BuiltInFunctions.Register(BuiltIns);
        return null;
    }

    public override ASTNode? Visit(FunctionNode node)
    {
        Variables = node.VariablesInScope;
        return null;
    }
}

public class FunctionCallExistsVisitor : FunctionCallVisitor
{
    public override ASTNode? Visit(FunctionCallNode node)
    {
        if (!Functions.ContainsKey(node.Name) && !BuiltIns.ContainsKey(node.Name))
            throw new SemanticErrorException(
                node.Name + " does not exist in the current context.",
                node
            );
        return null;
    }
}

public class FunctionCallTypeVisitor : FunctionCallVisitor
{
    public override ASTNode? Visit(FunctionCallNode node)
    {
        if (Functions.TryGetValue(node.Name, out var function))
        {
            if (function is not BuiltInVariadicFunctionNode)
            {
                for (var index = 0; index < node.Arguments.Count; index++)
                {
                    if (
                        GetTypeOfExpression(node.Arguments[index], Variables).GetType()
                        != function.ParameterVariables[index].Type.GetType()
                    )
                    {
                        throw new SemanticErrorException(
                            "Type of argument does not match what is required for the function",
                            node
                        );
                    }

                    if (
                        function.ParameterVariables[index].Type is EnumType enumType
                        && node.Arguments[index] is VariableUsagePlainNode variableUsagePlainNode
                        && enumType.Variants.Contains(variableUsagePlainNode.Name)
                    )
                    {
                        if (
                            variableUsagePlainNode.ExtensionType
                            != VariableUsagePlainNode.VrnExtType.None
                        )
                        {
                            throw new SemanticErrorException(
                                $"ambiguous variable name {variableUsagePlainNode.Name}",
                                node.Arguments[index]
                            );
                        }
                    }
                }
            }
        }
        return null;
    }
}

public class FunctionCallCountVisitor : FunctionCallVisitor
{
    public override ASTNode? Visit(FunctionCallNode node)
    {
        if (Functions.TryGetValue(node.Name, out var function))
        {
            int defaultParameterCount = 0;
            foreach (var variableDeclarationNode in Variables.Values)
            {
                if (variableDeclarationNode.IsDefaultValue)
                    defaultParameterCount++;
            }

            if (function is not BuiltInVariadicFunctionNode)
            {
                if (
                    node.Arguments.Count < function.ParameterVariables.Count - defaultParameterCount
                    || node.Arguments.Count > function.ParameterVariables.Count
                )
                    throw new SemanticErrorException(
                        "Function call does not have a valid amount of arguments for the function",
                        node
                    );
            }
        }

        return null;
    }
}

public class FunctionCallDefaultVisitor : FunctionCallVisitor
{
    public override ASTNode? Visit(FunctionCallNode node)
    {
        // Adds the default values to the function call to help with the compiler
        var function =
            Functions.GetValueOrDefault(node.Name)
            ?? BuiltIns.GetValueOrDefault(node.Name)
            ?? throw new SemanticErrorException($"Function {node.Name} not found", node);
        if (function is OverloadedFunctionNode overloads)
        {
            function = overloads.Overloads[node.Overload];
        }
        if (function.ParameterVariables.Count - node.Arguments.Count > 0)
        {
            for (int i = node.Arguments.Count; i < function.ParameterVariables.Count; i++)
            {
                node.Arguments.Add((ExpressionNode)function.ParameterVariables[i].InitialValue);
            }
        }

        return null;
    }
}

public class FunctionCallMutabilityVisitor : FunctionCallVisitor
{
    public override ASTNode? Visit(FunctionCallNode node)
    {
        var function = Functions[node.Name];
        for (var index = 0; index < function.ParameterVariables.Count; index++)
        {
            if (!function.ParameterVariables[index].IsConstant)
            {
                if (node.Arguments[index] is not VariableUsageNodeTemp)
                {
                    throw new SemanticErrorException(
                        $"cannot pass non var argument when you annotate an argument var",
                        node
                    );
                }
            }
        }

        return null;
    }
}

public class BuiltInFunctionCallVisitor : FunctionCallVisitor
{
    public override ASTNode? Visit(FunctionCallNode node)
    {
        if (BuiltIns.ContainsKey(node.Name))
        {
            var function = BuiltIns[node.Name];
            if (function is BuiltInVariadicFunctionNode builtInVariadicFunctionNode)
            {
                if (!builtInVariadicFunctionNode.AreParametersConstant) // hack to verify that all parameters are passed in as var
                {
                    if (
                        node.Arguments.Find(
                            n =>
                                n is not VariableUsageNodeTemp temp
                                || !temp.GetPlain().IsInFuncCallWithVar
                        ) is
                        { } badArgument
                    )
                    {
                        throw new SemanticErrorException(
                            $"cannot call builtin variadic {builtInVariadicFunctionNode.Name} with non var argument {badArgument}",
                            node
                        );
                    }
                }
            }
        }
        return null;
    }
}

public class FunctionCallGenericsVariadicsVisitor : FunctionCallVisitor
{
    public override ASTNode? Visit(FunctionCallNode node)
    {
        CallableNode function;
        if (BuiltIns.ContainsKey(node.Name))
        {
            function = BuiltIns[node.Name];
            if (function is BuiltInVariadicFunctionNode builtInVariadicFunctionNode)
            {
                node.InstantiatedVariadics = node.Arguments.Select(
                    arg => GetTypeOfExpression(arg, Variables)
                )
                    .ToList();
                node.FunctionDefinitionModule = builtInVariadicFunctionNode.parentModuleName!;
            }
            else
            {
                node.InstantiatedGenerics = GetInstantiatedGenerics(function, node);
            }
        }
        else
        {
            function = Functions[node.Name];
            node.InstantiatedGenerics = GetInstantiatedGenerics(function, node);
        }
        return null;
    }

    private Dictionary<string, Type> GetInstantiatedGenerics(
        CallableNode function,
        FunctionCallNode functionCall
    )
    {
        var selectMany = function
            .ParameterVariables.Zip(functionCall.Arguments)
            .SelectMany(paramAndArg =>
            {
                var param = paramAndArg.First;
                var argument = paramAndArg.Second;
                var argumentType = GetTypeOfExpression(paramAndArg.Second, Variables);
                IEnumerable<(string, Type)> typeCheckAndInstiateGenericParameter =
                    !param.Type.Equals(argumentType)
                        ?
                        // infer instantiated type
                        MatchTypes(param.Type, argumentType)
                            .ValueOr(
                                () =>
                                    throw new SemanticErrorException(
                                        $"Type mismatch cannot pass to {param.Name!}: {param.Type} {argument}: {argumentType}",
                                        argument
                                    )
                            )
                        : [];
                return typeCheckAndInstiateGenericParameter;
            })
            .Distinct();
        return
            selectMany.GroupBy(pair => pair.Item1).FirstOrDefault(group => group.Count() > 1)
                is { } bad
            ? throw new SemanticErrorException(
                $"generic {bad.Key} cannot match {string.Join(" and ", bad.Select(ty => ty.Item2))}",
                functionCall
            )
            : selectMany.ToDictionary();
    }

    // match types given two types (the is first the parameter type, the second is the argument type) attempts to find a substitution for the generics in the parameter type to new types given the fact that the parameter type is a more specific version of the argument type
    Option<IEnumerable<(string, Type)>> MatchTypes(Type paramType, Type type) =>
        (paramType, type) switch
        {
            // if we have some generic type we create a substitution that maps to the generic type to the type (base case)
            (GenericType g, _) => MatchTypesGeneric(g, type),
            // you cannot pass as a parameter a more general type that corresponds to an argument with a more specific type
            // we know this is a more general argument because the case above catches all generic parameters (even ones with a generic argument)
            (_, GenericType) => Option.None<IEnumerable<(string, Type)>>(),
            // if we have a reference type and another reference type we just unify their inner types
            (ReferenceType param, ReferenceType arg) => MatchTypes(param.Inner, arg.Inner),
            // // an instantiated type holds two things the actual types and the actual types for any generics in the actual type
            // to find the substitution for tow instantiated types we first verify that they are the same underlying type the `where ... Equals ...`
            (InstantiatedType param, InstantiatedType arg) when arg.Inner.Equals(param.Inner)
                => MatchTypesInstantiated(param, arg),
            // TODO: validate ranges
            // same as ReferenceType but for arrays
            (ArrayType param, ArrayType arg) => MatchTypes(param.Inner, arg.Inner),
            ({ } a, { } b) => Option.Some(Enumerable.Empty<(string, Type)>()).Filter(a.Equals(b))
        };

    Option<IEnumerable<(string, Type)>> MatchTypesGeneric(GenericType g, Type type)
    {
        return Option.Some(Enumerable.Repeat((g.Name, type), 1));
    }

    Option<IEnumerable<(string, Type)>> MatchTypesInstantiated(
        InstantiatedType paramType,
        InstantiatedType type
    ) =>
        paramType
            .InstantiatedGenerics.Values.Zip(type.InstantiatedGenerics.Values)
            .Select((pair) => MatchTypes(pair.Item1, pair.Item2))
            .Aggregate((first, second) => first.FlatMap(f => second.Map(f.Union)));
}

public class MathOpNodeOptimizer : SAVisitor
{
    public override ASTNode? PostWalk(MathOpNode node)
    {
        if (node.Op != MathOpNode.MathOpType.Divide && node.Op != MathOpNode.MathOpType.Times)
        {
            if (node.Left is IntNode left)
                if (left.Value == 0)
                    return node.Right;
            if (node.Left is FloatNode left2)
                if (left2.Value == 0.0)
                    return node.Right;

            if (node.Right is IntNode right)
                if (right.Value == 0)
                    return node.Left;
            if (node.Right is FloatNode right2)
                if (right2.Value == 0.0)
                    return node.Left;
        }
        return null;
    }
}

public class InvalidRecursiveTypeChecker : SAVisitor
{
    public override ASTNode? Visit(RecordNode node)
    {
        CheckRecursive(node.Type, []);
        return null;
    }

    private record Void;

    private readonly Void _void = new();

    // TODO: make recordCallStack also keep which member recursed to give better fixing suggestion
    private Void CheckRecursive(Type type, List<ModuleIndex> recordCallStack) =>
        type switch
        {
            GenericType
            or EnumType
            or StringType
            or RealType
            or ReferenceType
            or BooleanType
            or CharacterType
            or IntegerType
                => _void,
            ArrayType { Inner: { } inner } => CheckRecursive(inner, recordCallStack),
            InstantiatedType { Inner: { } inner } => CheckRecursiveRecord(inner, recordCallStack),
            RecordType record => CheckRecursiveRecord(record, recordCallStack),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    private Void CheckRecursiveRecord(RecordType record, List<ModuleIndex> recordCallStack)
    {
        var name = new ModuleIndex(new NamedIndex(record.Name), record.ModuleName);
        if (recordCallStack.Contains(name))
        {
            throw new SemanticErrorException(
                recordCallStack is [var recordName]
                    ? $"""
                Record {recordName} is recursive.
                Shank does not allow you to do this without a reference.
                To fix this put a refersTo on the field of {recordName} that is recursive.
                """
                    : $"""
                 Records {string.Join(", ", recordCallStack.Select(m => m.ToString()))} are mutually recursive
                 Shank does not allow you to do this without a reference.
                 To fix this put a refersTo on one of the fields that is recursive.
                 """
            );
        }
        foreach (var fieldsValue in record.Fields.Values)
        {
            CheckRecursive(fieldsValue, [.. recordCallStack, name]);
        }

        return _void;
    }
}
