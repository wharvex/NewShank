using System.Runtime.InteropServices;
using LLVMSharp;
using Shank.ASTNodes;

namespace Shank;

public abstract class SAVisitor
{
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

    protected Type GetTypeOfExpression(
        ExpressionNode node,
        Dictionary<string, VariableDeclarationNode> variables
    )
    {
        if (node is VariableUsagePlainNode variableUsagePlainNode)
        {
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
                    default:
                        throw new Exception();
                }
                break;
            default:
                throw new Exception();
        }
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
        Func<GenericType, GenericType> genericCollector
    )
    {
        return member switch
        {
            UnknownType u => ResolveType(u, module, generics, genericCollector),
            ReferenceType(UnknownType u) => handleReferenceType(u),
            ArrayType(UnknownType u, Range r) => HandleArrayType(u, r),
            _ => member
        };

        Type handleReferenceType(UnknownType type)
        {
            var resolvedType = ResolveType(type, module, generics, genericCollector);
            if (resolvedType is not (RecordType or InstantiatedType or GenericType))
            {
                throw new SemanticErrorException(
                    $"tried to use refersTo (dynamic memory management) on a non record type {resolvedType}",
                    module
                );
            }

            return new ReferenceType(resolvedType);
        }

        Type HandleArrayType(UnknownType t, Range r)
        {
            var resolvedType = ResolveType(t, module, generics, genericCollector);
            return new ArrayType(resolvedType, r);
        }
    }

    protected static Type ResolveType(
        UnknownType member,
        ModuleNode module,
        List<string> generics,
        Func<GenericType, GenericType> genericCollector
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
                        : genericCollector(new GenericType(member.TypeName))
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
        currentModule.updateImports(
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
        node.Type.Fields = node.Type.Fields.Select(field =>
        {
            return KeyValuePair.Create(
                field.Key,
                ResolveType(
                    field.Value,
                    module,
                    node.GenericTypeParameterNames,
                    (GenericType generic) =>
                    {
                        usedGenerics.Add(generic.Name);
                        return generic;
                    }
                )
            );
        })
            .ToDictionary();
        if (!usedGenerics.Distinct().SequenceEqual(node.GenericTypeParameterNames))
        {
            // TODO: make warnnig function for uniformity
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
        return null;
    }

    public override ASTNode? Visit(FunctionNode node)
    {
        CheckVariables(currentModule.GlobalVariables.Values.ToList(), currentModule, []);

        CheckVariables(node.LocalVariables, currentModule, node.GenericTypeParameterNames ?? []);
        node.LocalVariables.ForEach(vdn => OutputHelper.DebugPrintJson(vdn, vdn.Name ?? "null"));
        var generics = node.GenericTypeParameterNames ?? [];
        List<string> usedGenerics = [];
        foreach (var variable in node.ParameterVariables)
        {
            // find the type of each parameter, and also see what generics each parameter uses
            variable.Type = ResolveType(
                variable.Type,
                currentModule,
                generics,
                (GenericType generic) =>
                {
                    usedGenerics.Add(generic.Name);
                    return generic;
                }
            );
        }

        // if not all generics are used in the parameters that means those generics cannot be infered, but they could be used for variables which is bad
        if (!usedGenerics.Distinct().SequenceEqual(generics))
        {
            throw new SemanticErrorException(
                $"Generic Type parameter(s) {string.Join(", ", generics.Except(usedGenerics.Distinct()))}  cannot be infered for function {node.Name}",
                node
            );
        }
        return null;
    }

    private void CheckVariables(
        List<VariableDeclarationNode> variables,
        ModuleNode currentModule,
        List<String> generics
    )
    {
        foreach (var variable in variables)
        {
            // if its a constant then it cannot refer to another constant/variable so the only case for variable is its an emum cohnstant
            // might need  similiar logic for defaulat values of functions, and weird enum comparissons i.e. red = bar, where red is an enum constant
            // because currently we do assume lhs determine type
            if (variable is { IsConstant: true, InitialValue: { } init })
            {
                // from the parsers pov we should make an enum node, b/c this can't be any random varialbe
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
                variable.Type = ResolveType(variable.Type, currentModule, generics, x => x);
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
        LoopStatementStack.Pop();
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
        if (GetTypeOfExpression(node.Variable, Variables) is not IntegerType)
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
        // Adds the default values to the funciton call to help with the compiler
        if (Functions.TryGetValue(node.Name, out var function))
        {
            if (function.ParameterVariables.Count - node.Arguments.Count > 0)
            {
                for (int i = node.Arguments.Count; i < function.ParameterVariables.Count; i++)
                {
                    node.Arguments.Add((ExpressionNode)function.ParameterVariables[i].InitialValue);
                }
            }
        }

        return null;
    }
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
