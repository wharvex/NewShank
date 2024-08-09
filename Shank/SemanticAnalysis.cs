using System.Text;
using Shank.ASTNodes;

namespace Shank;

public class SemanticAnalysis
{
    public static ProgramNode? AstRoot { get; set; }
    public static Dictionary<string, ModuleNode>? Modules { get; set; }
    public static ModuleNode? StartModule { get; set; }
    public static InterpretOptions? ActiveInterpretOptions { get; set; }
    public static bool AreSimpleUnknownTypesDone;
    public static bool AreNestedUnknownTypesDone;
    public static bool AreTestsDone;
    public static bool AreExportsDone;
    public static bool AreImportsDone;

    public static bool GetVuopTestFlag()
    {
        return ActiveInterpretOptions?.VuOpTest ?? false;
    }

    public static void DoIfNotDoneAndSetAreDone(Action a, ref bool areDone)
    {
        if (areDone)
            return;
        a();
        areDone = true;
    }

    private static ProgramNode GetAstRootSafe() =>
        AstRoot ?? throw new InvalidOperationException("Expected AstRoot to not be null");

    private static Dictionary<string, ModuleNode> GetModulesSafe() =>
        Modules ?? throw new InvalidOperationException("Expected Modules to not be null.");

    private static ModuleNode GetStartModuleSafe() =>
        StartModule ?? throw new InvalidOperationException("Expected StartModule to not be null.");

    /// <summary>
    /// Checks the given functions for semantic issues.
    /// </summary>
    /// <param name="functions">A function-by-name dictionary of the functions to check</param>
    /// <param name="parentModule">The parent module of the given functions</param>
    public static void CheckFunctions(
        Dictionary<string, CallableNode> functions,
        ModuleNode parentModule
    )
    {
        functions
            .Where(kvp => kvp.Value is FunctionNode or OverloadedFunctionNode)
            .SelectMany(
                kvp =>
                    kvp.Value is FunctionNode f
                        ? [f]
                        : ((OverloadedFunctionNode)kvp.Value).Overloads.Values.Select(
                            overload => (FunctionNode)overload
                        )
            )
            .ToList()
            .ForEach(fn =>
            {
                var variables = GetLocalAndGlobalVariables(
                    parentModule,
                    [.. fn.LocalVariables.Concat(fn.ParameterVariables)]
                );
                CheckBlock(fn.Statements, variables, parentModule);
            });
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

    private static void CheckBlock(
        List<StatementNode> statements,
        Dictionary<string, VariableDeclarationNode> variables,
        ModuleNode parentModule
    )
    {
        foreach (var s in statements)
        {
            switch (s)
            {
                // Control flow reroute for vuop testing.
                case AssignmentNode an when GetVuopTestFlag():
                {
                    if (
                        variables.TryGetValue(
                            an.NewTarget.GetPlain().Name,
                            out var targetDeclaration
                        )
                    )
                    {
                        if (targetDeclaration.IsConstant)
                        {
                            throw new SemanticErrorException(
                                $"Variable {an.Target.Name} is not mutable, you cannot assign to it.",
                                an
                            );
                        }

                        an.NewTarget.GetPlain().ReferencesGlobalVariable =
                            targetDeclaration.IsGlobal;
                        an.Target.NewReferencesGlobalVariable = targetDeclaration.IsGlobal;
                    }

                    var targetType = variables[an.NewTarget.GetPlain().Name].Type;
                    NewCheckAssignment(
                        an.NewTarget.GetPlain().Name,
                        targetType,
                        an.Expression,
                        variables,
                        an.NewTarget
                    );
                    break;
                }
                case AssignmentNode an:
                {
                    if (variables.TryGetValue(an.Target.Name, out var targetDeclaration))
                    {
                        if (targetDeclaration.IsConstant)
                        {
                            throw new SemanticErrorException(
                                $"Variable {an.Target.Name} is not mutable, you cannot assign to it.",
                                an
                            );
                        }

                        an.Target.ReferencesGlobalVariable = targetDeclaration.IsGlobal;
                        an.Target.NewReferencesGlobalVariable = targetDeclaration.IsGlobal;
                    }

                    var targetType = GetTypeOfExpression(an.Target, variables);
                    CheckAssignment(an.Target.Name, targetType, an.Expression, variables);
                    break;
                }
                case FunctionCallNode fn
                    when parentModule.getFunctions().GetValueOrDefault(fn.Name) is { } function:
                    NewFunctionCallVisitor.CheckFunctionCall(fn, function, variables);
                    break;
                case FunctionCallNode fn
                    when parentModule
                        .getImportNames()
                        .Select(
                            module =>
                                Modules[module.Key].getExportNames().Contains(fn.Name)
                                    ? (CallableNode)parentModule.Imported[fn.Name]
                                    : null
                        )
                        .SingleOrDefault(function1 => function1 is not null)
                        is { } function:
                    NewFunctionCallVisitor.CheckFunctionCall(fn, function, variables);
                    break;
                case FunctionCallNode fn
                    when GetStartModuleSafe().getFunctions().GetValueOrDefault(fn.Name)
                        is BuiltInFunctionNode builtInFunctionNode:
                    NewFunctionCallVisitor.CheckFunctionCall(fn, builtInFunctionNode, variables);
                    break;
                case FunctionCallNode fn:
                    throw new Exception(
                        $"Could not find a definition for the function {fn.Name}."
                            + $" Make sure it was defined and properly exported if it was imported."
                    );
                case ForNode forNode:
                {
                    var iterationVariable = GetVuopTestFlag()
                        ? forNode.NewVariable
                        : forNode.Variable;
                    var typeOfIterationVariable = GetVuopTestFlag()
                        ? GetTypeOfExpression(forNode.NewVariable, variables)
                        : GetTypeOfExpression(forNode.Variable, variables);
                    if (variables[iterationVariable.GetPlain().Name].IsConstant)
                    {
                        throw new SemanticErrorException(
                            $"cannot iterate in a for loop with variable {iterationVariable} as it is not declared mutable",
                            iterationVariable
                        );
                    }

                    switch (typeOfIterationVariable)
                    {
                        case RealType:
                        {
                            var from = GetTypeOfExpression(forNode.From, variables);
                            var to = GetTypeOfExpression(forNode.To, variables);
                            if (
                                to is not IntegerType or RealType
                                || from is not IntegerType or RealType
                            )
                            {
                                throw new SemanticErrorException(
                                    $"For loop with iteration variable {iterationVariable} which is an real, must have ranges to and from being integers, or reals, but found ranges \"from {from} to {to}\"",
                                    iterationVariable
                                );
                            }

                            break;
                        }
                        case IntegerType:
                        {
                            var from = GetTypeOfExpression(forNode.From, variables);
                            var to = GetTypeOfExpression(forNode.To, variables);
                            if (to is not IntegerType || from is not IntegerType)
                            {
                                throw new SemanticErrorException(
                                    $"For loop with iteration variable {iterationVariable} which is an integer, must have ranges to and from being integers, but found ranges \"from {from} to {to}\"",
                                    iterationVariable
                                );
                            }

                            break;
                        }
                        default:
                            throw new SemanticErrorException(
                                // what about char?
                                $"for loop iteration variable {iterationVariable} is not a real or integer, but rather a {typeOfIterationVariable}, shank cannot auto increment this type",
                                iterationVariable
                            );
                    }

                    CheckBlock(forNode.Children, variables, parentModule);
                    break;
                }
                // for the rest of the cases of statements doing: GetTypeOfExpression(Node.Expression, variables);, is sufficient (no need to check that the type returned is a boolean), because Expression is already known to be BooleanExpressionNode
                // the reason we do it to make sure the underlying types of the boolean expression are fine (to disallow 1 > "5")
                // but I think boolean expression is not sufficient for these statements, because they do not allow plain variable access (foo, foo.bar ...)
                case RepeatNode repNode:
                    GetTypeOfExpression(repNode.Expression, variables);
                    CheckBlock(repNode.Children, variables, parentModule);
                    break;
                case WhileNode whileNode:
                    GetTypeOfExpression(whileNode.Expression, variables);
                    CheckBlock(whileNode.Children, variables, parentModule);
                    break;
                case IfNode ifNode:
                {
                    var nextNode = ifNode;
                    while (nextNode is not null and not ElseNode)
                    {
                        GetTypeOfExpression(nextNode.Expression, variables);
                        CheckBlock(nextNode.Children, variables, parentModule);
                        nextNode = nextNode.NextIfNode;
                    }

                    if (nextNode is ElseNode)
                    {
                        CheckBlock(nextNode.Children, variables, parentModule);
                    }

                    break;
                }
            }
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
            v.NewReferencesGlobalVariable = true;
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

        return [];
    }

    private static void NewCheckAssignment(
        string targetName,
        Type baseTargetType,
        ExpressionNode expression,
        Dictionary<string, VariableDeclarationNode> vDecs,
        VariableUsageNodeTemp target
    )
    {
        if (!target.Type.Equals(expression.Type))
        {
            throw new SemanticErrorException(
                "Type mismatch; cannot assign `"
                    + expression
                    + " : "
                    + expression.Type
                    + "' to `"
                    + targetName
                    + " : "
                    + target.Type
                    + "'.",
                expression
            );
        }
    }

    public static void CheckRange(
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
                            $"The variable {variable!} can only be assigned expressions that wont overstep its range ({from}..{to}), but attempted to assign to expression {expression} with range ({lower}..{upper}."
                        );
                }
            }
            /*catch (InvalidCastException e)
            {
                throw new Exception("Incorrect type of range.");
            }*/
        }
    }

    private static void NewCheckRange(
        VariableUsageNodeTemp target,
        Type targetType,
        ExpressionNode expression,
        Dictionary<string, VariableDeclarationNode> vdnByName
    )
    {
        if (targetType is not RangeType rt)
        {
            return;
        }

        // This is the result of accepting the "merge into pattern" ReSharper autocorrect.
        // Equivalent to: if (targetType is ArrayType at && at.Inner is RangeType irt)
        if (targetType is ArrayType { Inner: RangeType irt })
        {
            rt = irt;
        }

        var targetFrom = rt.Range.From;
        var targetTo = rt.Range.To;
        var expFrom = GetMinRange(expression, vdnByName);
        var expTo = GetMaxRange(expression, vdnByName);

        if (expFrom < targetFrom || expTo > targetTo)
        {
            throw new SemanticErrorException(
                target
                    + " invalid as the LHS of an assignment whose RHS exceeds the range of "
                    + target
                    + "\n\ntarget from: "
                    + targetFrom
                    + "\ntarget to: "
                    + targetTo
                    + "\nexpression from: "
                    + expFrom
                    + "\nexpression to: "
                    + expTo
                    + "\n\n"
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

        throw new SemanticErrorException(
            $"Unrecognized node type {node} on line"
                + node.Line
                + " in math expression while checking range",
            node
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

    public static Type GetTypeOfExpression(
        ExpressionNode expression,
        Dictionary<string, VariableDeclarationNode> variables
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
            var leftType = GetTypeOfExpression(booleanExpressionNode.Left, variables);
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

            var rightType = GetTypeOfExpression(booleanExpressionNode.Right, variables);
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
            var lhs = GetTypeOfExpression(mathOpNode.Left, variables);
            var rhs = GetTypeOfExpression(mathOpNode.Right, variables);
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
                    => GetTypeOfExpression(variableReferenceNode.Extension!, variables)
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
                (ExtensionType: VariableUsagePlainNode.VrnExtType.Enum, _) => variable.Type
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
            return vun.GetMyType(vdnByName, GetTypeOfExpression);
        }
    }

    /*private static IType GetSpecificRecordType(
        ModuleNode parentModule,
        VariableNode targetDefinition,
        VariableUsageNode targetUsage
    ) =>
        (
            (RecordNode)
                GetRecordsAndImports(parentModule.Records, parentModule.GetImportedSafe())[
                    targetDefinition.GetUnknownTypeSafe()
                ]
        )
            .GetFromMembersByNameSafe(targetUsage.GetRecordMemberReferenceSafe().Name)
            .NewType;*/

    //can return null as we may need to step backwards once recursive loop
    //this is if there is a nested record or reference, in which case the type that we want to return to be checked
    //should be variablenode.datatype.reference
    //if we checked for an extension when the target should be one of these types, an error is thrown, so if there is no extension
    //on the variable reference node, we return null to the previous recursive pass, which returns either VariableNode.DataType.Record
    //or Reference depending on what the previous loop found
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

    public static Dictionary<string, ASTNode> GetRecordsAndImports(
        Dictionary<string, RecordNode> records,
        Dictionary<string, ASTNode> imports
    )
    {
        var ret = new Dictionary<string, ASTNode>();
        records.ToList().ForEach(r => ret.Add(r.Key, r.Value));
        imports
            .ToList()
            .ForEach(i =>
            {
                if (!ret.TryAdd(i.Key, i.Value))
                {
                    throw new InvalidOperationException(
                        "Uncaught namespace conflict with record: " + i.Key
                    );
                }
            });
        return ret;
    }

    public static Dictionary<string, ASTNode> GetEnumsAndImports(
        Dictionary<string, EnumNode> enums,
        Dictionary<string, ASTNode> imports
    )
    {
        var ret = new Dictionary<string, ASTNode>();
        enums.ToList().ForEach(r => ret.Add(r.Key, r.Value));
        imports
            .ToList()
            .ForEach(i =>
            {
                if (!ret.TryAdd(i.Key, i.Value))
                {
                    throw new InvalidOperationException(
                        "Uncaught namespace conflict with enum: " + i.Key
                    );
                }
            });
        return ret;
    }

    public static Dictionary<string, ASTNode> GetRecordsAndEnumsAndImports(ModuleNode module)
    {
        return GetRecordsAndImports(
            module.Records,
            GetEnumsAndImports(module.Enums, module.GetImportedSafe())
        );
    }

    // Only used by ShankUnitTests project.
    // TODO: Convert all calls of this overload to calls of the overload that accepts a ProgramNode
    // argument, and then delete this overload.
    public static void CheckModules()
    {
        Modules = Interpreter.getModules();
        setStartModule();
        HandleExports();
        HandleImports();
        AssignNestedTypes();
        HandleUnknownTypes();
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

            CheckFunctions(module.Value.getFunctions(), module.Value);
        }
    }

    public static void CheckModules(ProgramNode pn)
    {
        Modules = pn.Modules;
        StartModule = pn.GetStartModuleSafe();
        DoIfNotDoneAndSetAreDone(HandleExports, ref AreExportsDone);
        DoIfNotDoneAndSetAreDone(HandleImports, ref AreImportsDone);
        DoIfNotDoneAndSetAreDone(AssignNestedTypes, ref AreNestedUnknownTypesDone);
        DoIfNotDoneAndSetAreDone(HandleUnknownTypes, ref AreSimpleUnknownTypesDone);
        DoIfNotDoneAndSetAreDone(handleTests, ref AreTestsDone);
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
            CheckFunctions(module.Value.getFunctions(), module.Value);
            CheckFunctions(
                module.Value.getTests().Select(u => (u.Key, (CallableNode)u.Value)).ToDictionary(),
                module.Value
            );
        }
    }

    public static ModuleNode? setStartModule()
    {
        if (Modules == null || Modules.Count == 0)
            Modules = Interpreter.Modules;
        if (StartModule != null)
            return StartModule;
        foreach (KeyValuePair<string, ModuleNode> currentModule in Modules)
        {
            if (currentModule.Value.getFunctions().ContainsKey("start"))
            {
                StartModule = currentModule.Value;
                return StartModule;
            }
        }

        return null;
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
        StartModule.UpdateImports(
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
                    m.UpdateImports(
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

    public static void HandleExports()
    {
        foreach (KeyValuePair<string, ModuleNode> currentModule in Modules)
        {
            currentModule.Value.UpdateExports();
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

    public static void HandleUnknownTypes()
    {
        foreach (var currentModule in Modules.Values)
        {
            foreach (var function in currentModule.getFunctions().Values)
            {
                CheckVariables(
                    currentModule.GlobalVariables.Values.ToList(),
                    currentModule,
                    [],
                    new DummyGenericContext()
                );
                if (function is OverloadedFunctionNode overloads)
                {
                    foreach (var overload in overloads.Overloads.Values)
                    {
                        GetUnknownTypesForFunction(overload, currentModule);
                    }
                }
                else
                {
                    GetUnknownTypesForFunction(function, currentModule);
                }
            }
        }
    }

    private static void GetUnknownTypesForFunction(CallableNode function, ModuleNode currentModule)
    {
        if (function is BuiltInFunctionNode)
        {
            return;
        }

        var currentFunction = (FunctionNode)function;
        var genericContext = new FunctionGenericContext(
            currentFunction.Name,
            currentModule.Name,
            currentFunction.Overload
        );
        CheckVariables(
            currentFunction.LocalVariables,
            currentModule,
            currentFunction.GenericTypeParameterNames,
            genericContext
        );
        currentFunction.LocalVariables.ForEach(
            vdn => OutputHelper.DebugPrintJson(vdn, vdn.Name ?? "null")
        );
        var generics = currentFunction.GenericTypeParameterNames;
        List<string> usedGenerics = [];
        foreach (var variable in currentFunction.ParameterVariables)
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

        // if not all generics are used in the parameters that means those generics cannot be infered, but they could be used for variables which is bad
        if (!usedGenerics.Distinct().SequenceEqual(generics))
        {
            throw new SemanticErrorException(
                $"Generic Type parameter(s) {string.Join(", ", generics.Except(usedGenerics.Distinct()))}  cannot be infered for function {currentFunction.Name}",
                currentFunction
            );
        }
    }

    private static void CheckVariables(
        List<VariableDeclarationNode> variables,
        ModuleNode currentModule,
        List<string> generics,
        GenericContext genericInfo
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
                variable.Type = ResolveType(
                    variable.Type,
                    currentModule,
                    generics,
                    generic => new GenericType(generic, genericInfo)
                );
            }
        }
    }

    private static bool Lookup<K, U, V>(Dictionary<K, V> dictionary, K key, ref U result)
        where U : class?
    {
        return dictionary.TryGetValue(key, out var value) && (value is U v && (result = v) == v);
    }

    public static void AssignNestedTypes()
    {
        foreach (var module in Modules.Values)
        {
            foreach (var record in module.Records.Values)
            {
                List<string> usedGenerics = [];
                var genericContext = new RecordGenericContext(
                    record.Name,
                    record.GetParentModuleSafe()
                );
                record.Type.Fields = record
                    .Type.Fields.Select(field =>
                    {
                        return KeyValuePair.Create(
                            field.Key,
                            ResolveType(
                                field.Value,
                                module,
                                record.GenericTypeParameterNames,
                                generic =>
                                {
                                    usedGenerics.Add(generic);
                                    return new GenericType(generic, genericContext);
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

    public static Type ResolveType(
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
            if (resolvedType is not (RecordType or InstantiatedType or GenericType))
            {
                throw new SemanticErrorException(
                    $"tried to use refersTo (dynamic memory management) on a non record type {resolvedType}",
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

    private static Type ResolveType(
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

    public static void reset()
    {
        Modules = new Dictionary<string, ModuleNode>();
        StartModule = null;
        Interpreter.testOutput = new StringBuilder();
        Program.UnitTestResults = new();
    }
}
