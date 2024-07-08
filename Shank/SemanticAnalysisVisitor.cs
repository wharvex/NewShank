using System.Runtime.InteropServices.JavaScript;
using Shank.ASTNodes;
using Shank.AstVisitorsTim;
using Shank.ExprVisitors;

namespace Shank;

/// <summary>
/// something the semantic analysis team should do tbh
/// </summary>
/// <param name="name"></param>
/// <param name="type"></param>
// struct Function(string name, FunctionType type)
// {
//     private string name;
//     private FunctionType type;
//
//     public void CheckType() { }
// }

public class SemanticAnalysisVisitor : Visitor
{
    private static ProgramNode program;
    public static ModuleNode StartModule { get; set; }
    public static Dictionary<string, ModuleNode>? Modules { get; set; }

    public static Dictionary<string, FunctionNode>? Functions { get; set; }

    public static Stack<Dictionary<string, VariableDeclarationNode>> variableStack { get; set; }
    public static Dictionary<string, VariableDeclarationNode> variables { get; set; }

    public static InterpretOptions? InterpreterOptions { get; set; }

    public static bool GetVuopTestFlag()
    {
        return InterpreterOptions?.VuOpTest ?? false;
    }

    public override void Visit(IntNode node)
    {
        if (GetTypeOfExpression(node).GetType() != typeof(IntegerType))
        {
            throw new SemanticErrorException("Integer is not an integer", node);
        }
    }

    public override void Visit(FloatNode node)
    {
        if (GetTypeOfExpression(node).GetType() != typeof(RealType))
        {
            throw new SemanticErrorException("Real is not an real", node);
        }
    }

    public override void Visit(VariableUsagePlainNode node)
    {
        // Checks if the variable has been declared or not
        if (!variableStack.Peek().ContainsKey(node.Name))
        {
            throw new SemanticErrorException("Variable is not declared in the current scope", node);
        }
    }

    public override void Visit(CharNode node)
    {
        if (GetTypeOfExpression(node).GetType() != typeof(CharacterType))
        {
            throw new SemanticErrorException("Character is not an character", node);
        }
    }

    public override void Visit(BoolNode node)
    {
        if (GetTypeOfExpression(node).GetType() != typeof(BooleanType))
        {
            throw new SemanticErrorException("Boolean is not an boolean", node);
        }
    }

    public override void Visit(StringNode node)
    {
        if (GetTypeOfExpression(node).GetType() != typeof(StringType))
        {
            throw new SemanticErrorException("String is not an string", node);
        }
    }

    public override void Visit(MathOpNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);

        if (GetTypeOfExpression(node.Left).GetType() != GetTypeOfExpression(node.Right).GetType())
            throw new SemanticErrorException("Math expression types do not match", node);
    }

    public override void Visit(BooleanExpressionNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
    }

    public override void Visit(RecordNode node)
    {
        //throw new NotImplementedException();
    }

    public override void Visit(FunctionCallNode node)
    {
        ASTNode calledFunction;
        //Checks if the function exists
        if (StartModule.getFunctions().ContainsKey(node.Name))
        {
            calledFunction = StartModule.getFunctions()[node.Name];
        }
        else if (StartModule.getImportedFunctions().ContainsKey(node.Name))
        {
            calledFunction = StartModule.getImportedFunctions()[node.Name];
        }
        else
        {
            throw new SemanticErrorException(
                "Cannot find the definition for the function " + node.Name,
                node
            );
        }

        //Makes sure that the argument count matches the amount needed
        int defaultParameterCount = 0;
        foreach (var variableDeclarationNode in ((CallableNode)calledFunction).ParameterVariables)
        {
            if (variableDeclarationNode.IsDefaultValue)
                defaultParameterCount++;
        }

        if (
            node.Arguments.Count
                < ((CallableNode)calledFunction).ParameterVariables.Count - defaultParameterCount
            || node.Arguments.Count > ((CallableNode)calledFunction).ParameterVariables.Count
                && calledFunction is not BuiltInVariadicFunctionNode
        )
            throw new SemanticErrorException(
                "Function call does not have a valid amount of arguments for the function",
                node
            );

        // Checks for if the type sof values in the function call match the types in the function parameters
        if (calledFunction is not BuiltInVariadicFunctionNode)
        {
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (
                    GetTypeOfExpression(node.Arguments[i]).GetType()
                    != ((CallableNode)calledFunction).ParameterVariables[i].Type.GetType()
                )
                {
                    throw new SemanticErrorException(
                        "Not all arguments in function call match the type required in the function parameters.",
                        node
                    );
                }

                //Checks to make sure that a variable is being passed in when it is expected
                if (!((CallableNode)calledFunction).ParameterVariables[i].IsConstant)
                    if (node.Arguments[i] is not VariableUsagePlainNode)
                        throw new SemanticErrorException(
                            "Cannot pass in a constant when a function is expecting a variable",
                            node
                        );
            }
        }

        // Adds the default values to the funciton call to help with the compiler
        if (((CallableNode)calledFunction).ParameterVariables.Count - node.Arguments.Count > 0)
        {
            for (
                int i = node.Arguments.Count;
                i < ((CallableNode)calledFunction).ParameterVariables.Count;
                i++
            )
            {
                node.Arguments.Add(
                    (ExpressionNode)
                        ((CallableNode)calledFunction).ParameterVariables[i].InitialValue
                );
            }
        }
    }

    private static Dictionary<string, VariableDeclarationNode> GetLocalAndGlobalVariables(
        ModuleNode module,
        List<VariableDeclarationNode> localVariables
    )
    {
        var ret = new Dictionary<string, VariableDeclarationNode>(module.GlobalVariables);
        localVariables.ForEach(v =>
        {
            if (!ret.TryAdd(v.GetNameSafe(), v))
            {
                throw new InvalidOperationException(
                    "Uncaught namespace conflict with local variable " + v.GetNameSafe()
                );
            }
        });
        return ret;
    }

    public override void Visit(FunctionNode node)
    {
        Functions.Add(node.Name, node);

        // try
        // {
        //     variableStack.Push(
        //         node.ParameterVariables.Concat(node.LocalVariables)
        //             .ToDictionary(var => var.Name, var => var)
        //     );
        //     variables = variableStack.Peek();
        // }
        // catch (ArgumentException)
        // {
        //     throw new SemanticErrorException(
        //         "Cannot declare a variable that has already been declared in the scope",
        //         node
        //     );
        // }

        foreach (var variableDeclarationNode in node.ParameterVariables)
        {
            variableDeclarationNode.Accept(this);
        }

        foreach (var variableDeclarationNode in node.LocalVariables)
        {
            variableDeclarationNode.Accept(this);
        }

        foreach (var statements in node.Statements)
        {
            statements.Accept(this);
        }

        //variableStack.Pop();
        variables = null;

        //throw new NotImplementedException();
    }

    public override void Visit(WhileNode node)
    {
        //throw new NotImplementedException();
    }

    public override void Visit(AssignmentNode node)
    {
        node.Target.Accept(this);
        node.Expression.Accept(this);
        // if (
        //     GetTypeOfExpression(node.Target).GetType()
        //     != GetTypeOfExpression(node.Expression).GetType()
        // )
        // {
        //     throw new SemanticErrorException("Type of variable and expression do not match", node);
        // }
        if (GetVuopTestFlag())
        {
            if (variables.TryGetValue(node.NewTarget.GetPlain().Name, out var targetDeclaration))
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

            var targetType = variables[node.NewTarget.GetPlain().Name].Type;
            NewCheckAssignment(
                node.NewTarget.GetPlain().Name,
                targetType,
                node.Expression,
                variables,
                node.NewTarget
            );
        }
        else
        {
            if (variables.TryGetValue(node.Target.Name, out var targetDeclaration))
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

            var targetType = GetTypeOfExpression(node.Target); //, variables);
            CheckAssignment(node.Target.Name, targetType, node.Expression, variables);
        }
    }

    private static Dictionary<string, Type> CheckAssignment(
        string targetName,
        Type targetType,
        ExpressionNode expression,
        Dictionary<string, VariableDeclarationNode> variables
    )
    {
        CheckRange(targetName, targetType, expression, variables);

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
        }
        else
        {
            var expressionType = GetTypeOfExpression(expression); //, variables);
            if (!targetType.Equals(expressionType))
            {
                throw new SemanticErrorException(
                    $"Type mismatch cannot assign to {targetName}: {targetType} {expression}: {expressionType}",
                    expression
                );
            }
        }

        return [];
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

        var expressionType = GetTypeOfExpression(expression); //, vDecs);
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
        // TODO: traverse record type if necesary
        // if (an.Expression is StringNode s)
        /*{
            try
            {
                var type = (StringType)variablesLookup[an.Target.Name].NewType;
                var from = type.Range.From;
                var to = type.Range.To;
                if (s.Value.Length < from || s.Value.Length > to)
                    throw new Exception(
                        $"The variable {an.Target.Name} can only be a length from {from.ToString()} to {to.ToString()}."
                    );
            }
            catch (InvalidCastException e)
            {

                throw new Exception("String types can only be assigned a range of two integers.", e);
            }
        }*/
        // else
        {
            // try
            {
                if (targetType is RangeType i) // all other i range type are bounded by integers
                {
                    var from = i.Range.From;
                    var to = i.Range.To;
                    int upper = (int)GetMaxRange(expression, variablesLookup);
                    int lower = (int)GetMinRange(expression, variablesLookup);

                    if (lower < from || upper > to)
                        throw new Exception(
                            $"The variable {variable!} can only be assigned expressions that wont overstep its range."
                        );
                }
            }
            /*catch (InvalidCastException e)
            {
                throw new Exception("Incorrect type of range.");
            }*/
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
            var dataType = GetTypeOfExpression(vrn); //, variables);
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
            var dataType = GetTypeOfExpression(vrn); //, variables);
            if (dataType is RangeType t)
            {
                return t.Range.From;
            }

            throw new Exception("Ranged variables can only be assigned variables with a range.");
        }

        throw new Exception("Unrecognized node type in math expression while checking range");
    }

    public override void Visit(EnumNode node)
    {
        //throw new NotImplementedException();
    }

    public override void Visit(ModuleNode node)
    {
        //Modules.Add(node.Name, node);

        if (node.Enums.Count > 0)
        {
            foreach (var enums in node.Enums.Values)
            {
                enums.Accept(this);
            }
        }

        if (node.Records.Count > 0)
        {
            foreach (var records in node.Records.Values)
            {
                records.Accept(this);
            }
        }

        foreach (var function in node.Functions.Values)
        {
            if (function is FunctionNode fn)
            {
                variables = GetLocalAndGlobalVariables(
                    node,
                    [.. fn.LocalVariables.Concat(fn.ParameterVariables)]
                );
                variableStack.Push(variables);
                function.Accept(this);
                variableStack.Pop();
            }
        }
    }

    public override void Visit(IfNode node)
    {
        //throw new NotImplementedException();
    }

    public override void Visit(RepeatNode node)
    {
        //throw new NotImplementedException();
    }

    public override void Visit(VariableDeclarationNode node)
    {
        // Makes sure that the value being put into the variable matches the type of the variable
        if (node.InitialValue != null)
        {
            ((ExpressionNode)node.InitialValue).Accept(this);
            if (
                node.Type.GetType()
                != GetTypeOfExpression((ExpressionNode)node.InitialValue).GetType()
            )
            {
                throw new SemanticErrorException(
                    "Type of variable and expression do not match",
                    node
                );
            }
        }
    }

    public override void Visit(ProgramNode node)
    {
        program = node;
        StartModule = node.GetStartModuleSafe();

        Modules = program.Modules;
        Functions = new Dictionary<string, FunctionNode>();
        variableStack = new Stack<Dictionary<string, VariableDeclarationNode>>();

        HandleExports();
        HandleImports();
        AssignNestedTypes();
        handleUnknownTypes();
        handleTests();

        foreach (KeyValuePair<string, ModuleNode> module in Modules)
        {
            if (module.Value.getName() == "default")
            {
                if (module.Value.getImportNames().Any())
                    throw new Exception("Cannot import to an unnamed module.");
                if (module.Value.getExportNames().Any())
                    throw new Exception("Cannot export from an unnamed module.");
            }

            //checking exports
            foreach (string exportName in module.Value.getExportNames())
            {
                if (
                    !module.Value.getFunctions().ContainsKey(exportName)
                    && !module.Value.getEnums().ContainsKey(exportName)
                    && !module.Value.Records.ContainsKey(exportName)
                )
                    throw new Exception(
                        $"Cannot export {exportName} from the module {module.Key} as it wasn't defined in that file."
                    );
            }

            //checking imports
            foreach (
                KeyValuePair<string, LinkedList<string>> import in module.Value.getImportNames()
            )
            {
                //checking that the target module exists
                if (!Modules.ContainsKey(import.Key))
                    throw new Exception($"Module {import.Key} does not exist");
                //if the i
                if (import.Value != null || import.Value.Count > 0)
                {
                    ModuleNode m = Modules[import.Key];

                    foreach (string s in import.Value)
                    {
                        if (
                            !m.getFunctions().ContainsKey(s)
                            && !m.getEnums().ContainsKey(s)
                            && !m.Records.ContainsKey(s)
                        )
                            throw new Exception(
                                $"The function {s} does not exist in module {import.Key}."
                            );
                        if (!m.getExportNames().Contains(s))
                            throw new Exception(
                                $"The module {import.Key} doesn't export the function {s}."
                            );
                    }
                }
            }
        }

        StartModule.Accept(this);

        // node.Modules.Values.ToList()
        //     .ForEach(n =>
        //     {
        //         n.Accept(this);
        //     });

        //Checks for if there is an import that cannot be found
        // foreach (var key in StartModule.getImportNames().Keys)
        // {
        //     if (!program.Modules.ContainsKey(key))
        //     {
        //         throw new Exception("Could not find " + key + " in the list of modules.");
        //     }
        // }
    }

    public static void HandleExports()
    {
        foreach (KeyValuePair<string, ModuleNode> currentModule in Modules)
        {
            currentModule.Value.UpdateExports();
        }
    }

    public static void HandleImports()
    {
        foreach (string currentImport in StartModule.getImportNames().Keys)
        {
            if (Modules.ContainsKey(currentImport))
            {
                recursiveImportCheck(Modules[currentImport]);
            }
            else
            {
                throw new Exception("Could not find " + currentImport + " in the list of modules.");
            }
        }

        foreach (var currentModule in StartModule.getImportNames())
        {
            if (StartModule.getImportNames()[currentModule.Key].Count == 0)
            {
                var tempList = new LinkedList<string>();
                foreach (string s in Modules[currentModule.Key].getExportNames())
                {
                    tempList.AddLast(s);
                }

                StartModule.getImportNames()[currentModule.Key] = tempList;
            }
        }
    }

    public static void recursiveImportCheck(ModuleNode m)
    {
        StartModule.updateImports(
            Modules[m.getName()].getFunctions(),
            Modules[m.getName()].getEnums(),
            Modules[m.getName()].Records,
            Modules[m.getName()].getExportedFunctions()
        );

        if (Modules[m.getName()].getImportNames().Count > 0)
        {
            foreach (string? moduleToBeImported in Modules[m.getName()].getImportNames().Keys)
            {
                if (Modules.ContainsKey(moduleToBeImported))
                {
                    m.updateImports(
                        Modules[moduleToBeImported].getFunctions(),
                        Modules[moduleToBeImported].getEnums(),
                        Modules[moduleToBeImported].Records,
                        Modules[moduleToBeImported].getExportedFunctions()
                    );
                    foreach (var currentModule in m.getImportNames())
                    {
                        if (m.getImportNames()[currentModule.Key].Count == 0)
                        {
                            var tempList = new LinkedList<string>();
                            foreach (string s in Modules[currentModule.Key].getExportNames())
                            {
                                tempList.AddLast(s);
                            }

                            m.getImportNames()[currentModule.Key] = tempList;
                        }
                    }

                    recursiveImportCheck(Modules[moduleToBeImported]);
                }
            }
        }
    }

    public static void AssignNestedTypes()
    {
        Dictionary<(string, string), RecordNode> resolvedRecords = new();
        foreach (var module in Modules.Values)
        {
            foreach (var record in module.Records.Values)
            {
                List<string> usedGenerics = [];
                record.Type.Fields = record
                    .Type.Fields.Select(field =>
                    {
                        return KeyValuePair.Create(
                            field.Key,
                            ResolveType(
                                field.Value,
                                module,
                                record.GenericTypeParameterNames,
                                (GenericType generic) =>
                                {
                                    usedGenerics.Add(generic.Name);
                                    return generic;
                                }
                            )
                        );
                    })
                    .ToDictionary();
                if (!usedGenerics.Distinct().SequenceEqual(record.GenericTypeParameterNames))
                {
                    // TODO: make warnnig function for uniformity
                    throw new SemanticErrorException(
                        $"Generic Type parameter(s) {string.Join(", ", record.GenericTypeParameterNames.Except(usedGenerics.Distinct()))} are unused for record {record.Name}",
                        record
                    );
                }
            }
        }
    }

    private static Type ResolveType(
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

    private static Type ResolveType(
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

    public static void handleUnknownTypes()
    {
        foreach (KeyValuePair<string, ModuleNode> currentModule in Modules)
        {
            foreach (
                KeyValuePair<string, CallableNode> function in currentModule.Value.getFunctions()
            )
            {
                CheckVariables(
                    currentModule.Value.GlobalVariables.Values.ToList(),
                    currentModule.Value,
                    []
                );
                if (function.Value is BuiltInFunctionNode)
                {
                    continue;
                }

                FunctionNode currentFunction = (FunctionNode)function.Value;
                CheckVariables(
                    currentFunction.LocalVariables,
                    currentModule.Value,
                    currentFunction.GenericTypeParameterNames ?? []
                );
                currentFunction.LocalVariables.ForEach(
                    vdn => OutputHelper.DebugPrintJson(vdn, vdn.Name ?? "null")
                );
                var generics = currentFunction.GenericTypeParameterNames ?? [];
                List<string> usedGenerics = [];
                foreach (var variable in currentFunction.ParameterVariables)
                {
                    // find the type of each parameter, and also see what generics each parameter uses
                    variable.Type = ResolveType(
                        variable.Type,
                        currentModule.Value,
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
                        $"Generic Type parameter(s) {string.Join(", ", generics.Except(usedGenerics.Distinct()))}  cannot be infered for function {currentFunction.Name}",
                        currentFunction
                    );
                }

                // foreach (var statement in currentFunction.Statements)
                /*{
                    if (statement is AssignmentNode assignment)
                    {

                        if (currentFunction.LocalVariables.Concat(currentFunction.ParameterVariables).FirstOrDefault(node =>
                               GetTypeRecursive(node.NewType, assignment.Target) is EnumType e && assignment.Target.Name == node.Name) is not null)
                        {
                            // how do we know all types in all modules that the current module we are using depends on are resolved
                            assignment.Target.ExtensionType = ASTNode.VrnExtType.Enum;
                        }
                    }
                }*/
            }
        }
    }

    private static void CheckVariables(
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
                    variable.Type = GetTypeOfExpression(init); //, []);
                }
            }
            else
            {
                variable.Type = ResolveType(variable.Type, currentModule, generics, x => x);
            }
        }
    }

    public static void handleTests()
    {
        foreach (KeyValuePair<string, ModuleNode> currentModule in Modules)
        {
            foreach (KeyValuePair<string, TestNode> test in currentModule.Value.getTests())
            {
                if (currentModule.Value.getFunctions().ContainsKey(test.Value.targetFunctionName))
                {
                    (
                        (FunctionNode)
                            currentModule.Value.getFunctions()[test.Value.targetFunctionName]
                    ).Tests.Add(test.Key, test.Value);
                }
                else
                    throw new Exception(
                        $"Could not find the function {test.Value.targetFunctionName} in the module {currentModule.Key} to be tested."
                    );
            }
        }
    }

    public override void Visit(ForNode node)
    {
        Console.WriteLine("ForNode visited");
        //throw new NotImplementedException();
    }

    public override void Visit(BuiltInFunctionNode node)
    {
        Console.WriteLine("BuiltInFunctionNode visited");
        //throw new NotImplementedException();
    }

    private static Type GetTypeOfExpression(
        ExpressionNode expression //,
    //Dictionary<string, VariableDeclarationNode> variables
    )
    {
        return expression switch
        {
            IntNode intNode => new IntegerType(),
            BooleanExpressionNode booleanExpressionNode
                => GetTypeOfBooleanExpression(booleanExpressionNode, variables),
            CharNode charNode => new CharacterType(),
            FloatNode floatNode => new RealType(),
            BoolNode boolNode => new BooleanType(),
            MathOpNode mathOpNode => GetTypeOfMathOp(mathOpNode, variables),
            StringNode stringNode => new StringType(),
            // Control flow reroute for vuop testing.
            VariableUsageNodeTemp variableReferenceNode
                => GetVuopTestFlag()
                    ? NewGetTypeOfVariableUsage(variableReferenceNode, variables)
                    : GetTypeOfVariableUsage(
                        (VariableUsagePlainNode)variableReferenceNode,
                        variables
                    ),
            _ => throw new ArgumentOutOfRangeException(expression.ToString())
        };

        Type GetTypeOfBooleanExpression(
            BooleanExpressionNode booleanExpressionNode,
            Dictionary<string, VariableDeclarationNode> variableNodes
        )
        {
            // TODO: are all things of the same type comparable
            var leftType = GetTypeOfExpression(booleanExpressionNode.Left); //, variables);
            if (
                leftType is EnumType e
                && booleanExpressionNode.Right is VariableUsagePlainNode v
                && e.Variants.Contains(v.Name)
            )
            {
                if (v.ExtensionType != VariableUsagePlainNode.VrnExtType.None)
                {
                    throw new SemanticErrorException(
                        $"ambiguous variable name {v.Name}",
                        expression
                    );
                }

                return new BooleanType();
            }

            var rightType = GetTypeOfExpression(booleanExpressionNode.Right); //, variables);
            return leftType.Equals(rightType)
                ? new BooleanType()
                : throw new SemanticErrorException(
                    $"could not compare expressions of different types {leftType} to {rightType}",
                    booleanExpressionNode
                );
        }

        Type GetTypeOfMathOp(
            MathOpNode mathOpNode,
            Dictionary<string, VariableDeclarationNode> variableNodes
        )
        {
            var lhs = GetTypeOfExpression(mathOpNode.Left); //, variables);
            var rhs = GetTypeOfExpression(mathOpNode.Right); //, variables);
            // TODO: preserver ranges
            return (lhs, rhs) switch
            {
                (StringType or CharacterType, StringType or CharacterType)
                    => mathOpNode.Op == MathOpNode.MathOpType.Plus
                        ? new StringType()
                        : throw new SemanticErrorException(
                            $"cannot {mathOpNode.Op} two strings",
                            mathOpNode
                        ),
                (RealType, RealType) => lhs,
                (IntegerType, IntegerType) => lhs,
                (
                    StringType
                        or CharacterType
                        or IntegerType
                        or RealType,
                    RealType
                        or StringType
                        or CharacterType
                        or IntegerType
                )
                    => throw new SemanticErrorException(
                        $"{lhs} and {rhs} are not the same so you cannot perform math operations on them",
                        mathOpNode
                    ),
                (StringType or CharacterType or IntegerType or RealType, _)
                    => throw new SemanticErrorException(
                        $"the right hand side of this math expression is not able to be used in math expression",
                        mathOpNode
                    ),
                (_, StringType or CharacterType or IntegerType or RealType)
                    => throw new SemanticErrorException(
                        $"the left hand side of this math expression is not able to be used in math expression",
                        mathOpNode
                    ),
                _
                    => throw new SemanticErrorException(
                        "the expression used in this math expression are not valid in math expressions",
                        mathOpNode
                    )
            };
        }

        Type GetTypeOfVariableUsage(
            VariableUsagePlainNode variableReferenceNode,
            Dictionary<string, VariableDeclarationNode> variableNodes
        )
        {
            var variable =
                variables.GetValueOrDefault(variableReferenceNode.Name)
                ?? throw new SemanticErrorException(
                    $"Variable {variableReferenceNode.Name} not found",
                    variableReferenceNode
                );
            variableReferenceNode.ReferencesGlobalVariable = variable.IsGlobal;
            return (variableReferenceNode.ExtensionType, NewType: variable.Type) switch
            {
                (ExtensionType: VariableUsagePlainNode.VrnExtType.None, _) => variable.Type,
                (
                    ExtensionType: VariableUsagePlainNode.VrnExtType.RecordMember,
                    NewType: InstantiatedType r
                )
                    => GetTypeRecursive(r, variableReferenceNode) ?? variable.Type,
                (
                    ExtensionType: VariableUsagePlainNode.VrnExtType.RecordMember,
                    NewType: ReferenceType
                    (InstantiatedType r)
                )
                    => GetTypeRecursive(r, variableReferenceNode) ?? variable.Type,
                (ExtensionType: VariableUsagePlainNode.VrnExtType.ArrayIndex, NewType: ArrayType a)
                    => GetTypeOfExpression(variableReferenceNode.Extension!) //, variables)
                    is IntegerType
                        ? a.Inner
                        : throw new SemanticErrorException(
                            "Array indexer does not resolve to a number",
                            variableReferenceNode
                        ),
                (ExtensionType: VariableUsagePlainNode.VrnExtType.ArrayIndex, _)
                    => throw new SemanticErrorException(
                        "Invalid array index, tried to index into non array",
                        variableReferenceNode
                    ),
                (ExtensionType: VariableUsagePlainNode.VrnExtType.RecordMember, NewType: { } t)
                    => throw new SemanticErrorException(
                        $"Invalid member access, tried to access non record {t}",
                        variableReferenceNode
                    ),
            };
        }

        Type NewGetTypeOfVariableUsage(
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
            var vtVis = new VunTypeGettingVisitor(vdn.Type, vdnByName);
            vun.Accept(vtVis);
            return vtVis.VunType;
        }
    }

    private static Type? GetTypeRecursive(
        // ModuleNode parentModule,
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

        //     /*else
        //         return GetRecordTypeRecursive(
        //                 parentModule,
        //                 (RecordNode)
        //                     GetRecordsAndImports(parentModule.Records, parentModule.Imported)[
        //                         targetDefinition
        //                             .GetFromMembersByNameSafe(
        //                                 ((VariableUsageNode)targetUsage.GetExtensionSafe()).Name
        //                             )
        //                             .GetUnknownTypeSafe()
        //                     ],
        //                 (VariableReferenceNode)targetUsage.GetExtensionSafe()
        //             */
    }

    // private Type GetTypeOfExpression(ExpressionNode node)
    // {
    //     if (node is VariableUsagePlainNode variableUsagePlainNode)
    //     {
    //         var name = variableUsagePlainNode.Name;
    //         var value = variableStack.Peek()[name];
    //         switch (value.Type)
    //         {
    //             case IntegerType:
    //                 return new IntegerType();
    //             case RealType:
    //                 return new RealType();
    //             case StringType:
    //                 return new StringType();
    //             case CharacterType:
    //                 return new CharacterType();
    //             case BooleanType:
    //                 return new BooleanType();
    //             case ArrayType:
    //                 return GetTypeOfExpression(
    //                     (ExpressionNode)variableUsagePlainNode.GetExtensionSafe()
    //                 );
    //             case RecordType:
    //                 return GetTypeOfExpression(
    //                     (ExpressionNode)variableUsagePlainNode.GetExtensionSafe()
    //                 );
    //             default:
    //                 throw new Exception();
    //         }
    //     }
    //     switch (node)
    //     {
    //         case IntNode:
    //             return new IntegerType();
    //         case FloatNode:
    //             return new RealType();
    //         case StringNode:
    //             return new StringType();
    //         case CharNode:
    //             return new CharacterType();
    //         case BoolNode:
    //             return new BooleanType();
    //         case MathOpNode mathVal:
    //             switch (mathVal.Left)
    //             {
    //                 case IntNode:
    //                     return new IntegerType();
    //                 case FloatNode:
    //                     return new RealType();
    //                 case StringNode:
    //                     return new StringType();
    //                 case CharNode:
    //                     return new CharacterType();
    //                 case VariableUsagePlainNode variableUsage:
    //                     return GetTypeOfExpression(variableUsage);
    //                 default:
    //                     throw new Exception();
    //             }
    //             break;
    //         default:
    //             throw new Exception();
    //     }
    // }
}
