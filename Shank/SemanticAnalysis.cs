using System.Text;
using Shank.ASTNodes;

namespace Shank;

public class SemanticAnalysis
{
    public static Dictionary<string, ModuleNode>? Modules { get; set; }
    public static ModuleNode? StartModule { get; set; }

    private static Dictionary<string, ModuleNode> GetModulesSafe() =>
        Modules ?? throw new InvalidOperationException("Expected Modules to not be null.");

    private static ModuleNode GetStartModuleSafe() =>
        StartModule ?? throw new InvalidOperationException("Expected StartModule to not be null.");

    /// <summary>
    /// Checks the given functions for semantic issues.
    /// </summary>
    /// <param name="functions">A function-by-name dictionary of the functions to check</param>
    /// <param name="parentModule">The parent module of the given functions</param>
    /// <remarks>Author: Tim Gudlewski</remarks>
    public static void CheckFunctions(
        Dictionary<string, CallableNode> functions,
        ModuleNode parentModule
    )
    {
        functions
            .Where(kvp => kvp.Value is FunctionNode)
            .Select(kvp => (FunctionNode)kvp.Value)
            .ToList()
            .ForEach(fn =>
            {
                var variables = GetLocalAndGlobalVariables(
                    parentModule,
                    [.. fn.LocalVariables.Concat(fn.ParameterVariables)]
                );
                CheckBlock(
                    fn.Statements,
                    variables,
                    variables.ToDictionary(kvp => kvp.Key, _ => false),
                    parentModule
                );
            });
    }

    private static Dictionary<string, VariableNode> GetLocalAndGlobalVariables(
        ModuleNode module,
        List<VariableNode> localVariables
    )
    {
        var ret = new Dictionary<string, VariableNode>(module.GlobalVariables);
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
        Dictionary<string, VariableNode> variables,
        Dictionary<string, bool> variablesSet,
        ModuleNode parentModule
    )
    {
        // This foreach loop goes through the statements in order, starting with the outermost
        // scope (i.e. a function block) in which the variables passed in are visible.

        // Therefore, the following should be sufficient to ensure variables are set before use.

        // When we encounter a variable x as the target of an assignment, we determine if
        // the assignment is valid, and if it is valid, then we set the Value of x's Key in
        // variablesSet to true. (If it's not valid, throw an exception.)

        // When we encounter a variable x NOT as the target of an assignment, then we look x up
        // in variablesSet, and if its Value is false, then we throw an exception.

        // TODO: How to lookup/store the name of a "complex" VRN in variablesSet, i.e. one with
        // an Extension?

        foreach (var s in statements)
        {
            var foundFunction = false;
            if (s is AssignmentNode an)
            {
                if (variables.TryGetValue(an.Target.Name, out var targetDeclaration))
                {
                    var targetTypeNull = GetTargetTypeForAssignmentCheck(
                        targetDeclaration,
                        an.Target,
                        parentModule
                    );
                    if (targetDeclaration.IsConstant)
                    {
                        throw new SemanticErrorException(
                            $"Variable {an.Target.Name} is not mutable, you cannot assign to it.",
                            an
                        );
                    }

                    //GetTargetTypeForAssignmentCheck can now maybe return null, we catch it here
                    if (targetTypeNull == null)
                        throw new Exception("Couldn't find target type");
                    var targetType = (IType)targetTypeNull;

                    CheckNode(
                        targetType,
                        an.Expression,
                        variables,
                        parentModule,
                        targetDeclaration
                    );

                    CheckRange(an, variables, targetType, parentModule);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Unrecognized variable name " + an.Target.Name
                    );
                }
            }
            else if (s is FunctionCallNode fn)
            {
                var overloadNameExt = "";
                //fn.Parameters.ForEach(
                //    pn =>
                //        overloadNameExt += pn.ToStringForOverload(
                //            parentModule.GetImportedSafe(),
                //            parentModule.Records,
                //            dict
                //        )
                //);
                fn.OverloadNameExt = overloadNameExt;

                if (parentModule.getFunctions().ContainsKey(fn.Name))
                {
                    foundFunction = true;
                    if (parentModule.getFunctions()[fn.Name] is not BuiltInFunctionNode)
                        fn.InstiatedGenerics = CheckFunctionCall(
                            fn.Parameters,
                            variables,
                            (FunctionNode)parentModule.getFunctions()[fn.Name],
                            fn
                        );
                }
                else
                {
                    foreach (var import in parentModule.getImportNames())
                    {
                        if (Modules[import.Key].getExportNames().Contains(fn.Name))
                        {
                            foundFunction = true;
                            fn.InstiatedGenerics = CheckFunctionCall(
                                fn.Parameters,
                                variables,
                                (FunctionNode)parentModule.Imported[fn.Name],
                                fn
                            );
                        }
                    }
                }

                if (
                    GetStartModuleSafe().getFunctions().ContainsKey((string)fn.Name)
                    && GetStartModuleSafe().getFunctions()[(string)fn.Name] is BuiltInFunctionNode
                )
                    foundFunction = true;
                if (!foundFunction)
                {
                    throw new Exception(
                        $"Could not find a definition for the function {fn.Name}."
                        + $" Make sure it was defined and properly exported if it was imported."
                    );
                }
            }
            else if (s is RepeatNode repNode)
            {
                CheckComparison(repNode.Expression, variables, parentModule);
            }
            else if (s is WhileNode whileNode)
            {
                CheckComparison(whileNode.Expression, variables, parentModule);
            }
            else if (s is IfNode ifNode)
            {
                CheckComparison(ifNode.Expression, variables, parentModule);
            }
        }
    }

    // This is why something like type usage/IType is useful, because when we just return the datatype
    // for instiation we do lose type information, such as what type of record, or enum, what is the inner type of the array?
    // and IType is the best because it only stores whats neccesary for a givern type
    private static Dictionary<string, IType> CheckFunctionCall(
        List<ParameterNode> param,
        Dictionary<string, VariableNode> variables,
        FunctionNode fn,
        FunctionCallNode functionCallNode
    )
    {
        // TODO: overloads and default parameters might have different arrity
        return fn.ParameterVariables.Zip(param)
            .SelectMany(paramAndArg =>
            {
                var param = paramAndArg.First;
                var arguement = paramAndArg.Second;

                var actualArguement =
                    arguement.Variable == null
                        ? arguement.Constant
                        : variables[arguement.Variable.Name];
                // assumption ranges are already checked to be only on types that allow them
                CheckParameterRange(param, arguement, variables, fn);
                CheckParameterMutability(param, arguement, variables, functionCallNode);
                return TypeCheckAndInstiateGenericParameter(param, arguement, variables, fn);
            })
            .ToDictionary();
    }

    public static IEnumerable<(String, IType)> TypeCheckAndInstiateGenericParameter(
        VariableNode param,
        ParameterNode argument,
        Dictionary<string, VariableNode> variables,
        FunctionNode fn
    )
    {
        // check that the arguement passed in has the right type for its parameter
        // and also if the parameter has any generics try to instiate them
        return [];
    }

    // assumptions if the arguement is a variable it assumed to be there already from previous check in check function call
    private static void CheckParameterMutability(
        VariableNode param,
        ParameterNode argument,
        Dictionary<string, VariableNode> variables,
        FunctionCallNode fn
    )
    {
        // check that the arguement passed in has the right type of mutablility for its parameter
        if (argument.IsVariable)
        {
            VariableNode? lookedUpArguement = variables.GetValueOrDefault(
                (
                    argument.Variable
                    ?? throw new SemanticErrorException(
                        $"Cannot pass a non variable as being var to a function call",
                        fn
                    )
                ).Name
            );
            if (lookedUpArguement.IsConstant)
            {
                throw new SemanticErrorException(
                    $"cannot pass non var argument when you annotate an argument var",
                    fn
                );
            }

            if (param.IsConstant)
            {
                // TODO: warning of unused var annotation
            }
        }
        else if (!param.IsConstant)
        {
            throw new SemanticErrorException(
                $"cannot pass non var argument when function is expecting it to be var",
                fn
            );
        }
    }

    // TODO: combine with check range we don't need 2 seperate methods for range checking
    private static void CheckParameterRange(
        VariableNode param,
        ParameterNode argument,
        Dictionary<string, VariableNode> variables,
        FunctionNode fn
    )
    {
            if (param.NewType is IRangeType t)
            {
                if (argument.Variable is not null)
                {
                    // assumed that type checking already done
                    var variableType = variables[argument.Variable.Name].NewType as IRangeType;
                    
                    var actualFrom = variableType.Range.From;
                    var actualTo = variableType.Range.To;
                    var targetFrom = t.Range.From;
                    var targetTo = t.Range.To;
                        if (
                                                targetFrom > actualFrom
                                                || targetTo < actualTo) {
                                                throw new Exception($"Mismatched range in a call to {fn.Name}");
                        }
                }
                else
                {
                    var actualFrom = GetMaxRange(argument.Constant, variables);
                    var actualTo = GetMinRange(argument.Constant, variables);
                    var targetFrom = t.Range.From;
                    var targetTo = t.Range.To;
                    if (
                            targetFrom > actualFrom
                            || targetTo < actualTo) {
                            throw new Exception($"Mismatched range in a call to {fn.Name}");
                    }
                }
            }
    }

    private static void CheckRange(
        AssignmentNode an,
        Dictionary<string, VariableNode> variablesLookup,
        IType targetType,
        ModuleNode parentModule
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
                if
                          (targetType is IRangeType i) // all other i range type are bounded by integers
                {
                    var from = i.Range.From;
                    var to = i.Range.To;
                    int upper = (int)GetMaxRange(an.Expression, variablesLookup);
                    int lower = (int)GetMinRange(an.Expression, variablesLookup);

                    if (lower < from || upper > to)
                        throw new Exception(
                            $"The variable {an.Target.Name} can only be assigned expressions that wont overstep its range."
                        );
                }
            }
            /*catch (InvalidCastException e)
            {
                throw new Exception("Incorrect type of range.");
            }*/
        }
    }

    private static float GetMaxRange(ASTNode node, Dictionary<string, VariableNode> variables)
    {
        if (node is MathOpNode mon)
        {
            switch (mon.Op)
            {
                case ASTNode.MathOpType.plus:
                    return GetMaxRange(mon.Left, variables) + GetMaxRange(mon.Right, variables);
                case ASTNode.MathOpType.minus:
                    return GetMaxRange(mon.Left, variables) - GetMinRange(mon.Right, variables);
                case ASTNode.MathOpType.times:
                    return GetMaxRange(mon.Left, variables) * GetMaxRange(mon.Right, variables);
                case ASTNode.MathOpType.divide:
                    return GetMinRange(mon.Left, variables) / GetMaxRange(mon.Right, variables);
                case ASTNode.MathOpType.modulo:
                    return GetMaxRange(mon.Right, variables) - 1;
            }
        }

        if (node is IntNode i)
            return i.Value;
        if (node is FloatNode f)
            return f.Value;
        if (node is StringNode s)
            return s.Value.Length;
        if (node is VariableReferenceNode vrn)
        {
            var dataType = variables[vrn.Name].NewType;
            if (dataType is IRangeType t)
               return t.Range.To;
            throw new Exception(
                "Ranged variables can only be assigned variables with a range."
            );
        }
        throw new Exception(
            "Unrecognized node type on line "
                + node.Line
                + " in math expression while checking range"
        );
    }

    private static float GetMinRange(ASTNode node, Dictionary<string, VariableNode> variables)
    {
        if (node is MathOpNode mon)
        {
            switch (mon.Op)
            {
                case ASTNode.MathOpType.plus:
                    return GetMinRange(mon.Left, variables) + GetMinRange(mon.Right, variables);
                case ASTNode.MathOpType.minus:
                    return GetMinRange(mon.Left, variables) - GetMaxRange(mon.Right, variables);
                case ASTNode.MathOpType.times:
                    return GetMinRange(mon.Left, variables) * GetMinRange(mon.Right, variables);
                case ASTNode.MathOpType.divide:
                    return GetMaxRange(mon.Left, variables) / GetMinRange(mon.Right, variables);
                case ASTNode.MathOpType.modulo:
                    return 0;
            }
        }

        if (node is IntNode i)
            return i.Value;
        if (node is FloatNode f)
            return f.Value;
        if (node is StringNode s)
            return s.Value.Length;
        if (node is VariableReferenceNode vrn)
        {
              var dataType = variables[vrn.Name].NewType;
            if (dataType is IRangeType t)
               return t.Range.From;
            throw new Exception(
                "Ranged variables can only be assigned variables with a range."
            );
        }


        throw new Exception("Unrecognized node type in math expression while checking range");
    }

    private static void CheckComparison(
        BooleanExpressionNode? ben,
        Dictionary<string, VariableNode> variables,
        ModuleNode parentModule
    )
    {
        if (ben == null)
            return;
        if (ben.Left is IntNode)
        {
            if (ben.Right is not IntNode)
                throw new Exception("Can only compare integers to other integers.");
        }
        else if (ben.Left is FloatNode)
        {
            if (ben.Right is not FloatNode)
                throw new Exception("Can only compare floats to other floats.");
        }
        else if (ben.Left is VariableReferenceNode vrn)
        {
            VariableNode variable;

            //checking whether the left or right side of the equation is the variable
            //this allows for the value of an enum to be on either side
            if (variables.ContainsKey(vrn.Name))
                variable = variables[vrn.Name];
            else
                // TODO: This will produce an InvalidCastException if ben.Right is not a VRN.
                variable = variables[((VariableReferenceNode)ben.Right).Name];
            switch (variable.NewType)
            {
                case IntegerType:
                    if (
                        ben.Right is not IntNode
                        || (
                            ben.Right is VariableReferenceNode vrn2
                            && variables[vrn2.Name].NewType is not IntegerType
                        )
                    )
                        throw new SemanticErrorException(
                            "Integers can only be compared to integers or integer variables.", ben
                        );
                    break;
                case RealType:
                    if (
                        ben.Right is not FloatNode
                        || (
                            ben.Right is VariableReferenceNode vrn3
                            && variables[vrn3.Name].NewType is not RealType
                        )
                    )
                        throw new Exception(
                            "Floats can only be compared to floats or float variables."
                        );
                    break;
                case EnumType e:
                    var right = ben.Right as VariableReferenceNode;
                    if (!e.Variants.Contains(right!.Name) && (!variables.TryGetValue(right.Name, out var variableNode) ||
                        variableNode.NewType.Equals(variable.NewType)))
                    {
                        throw new SemanticErrorException(
                            "Enums can only be compared to enums or enum variables of the same type.", ben
                        );
                    }
                    /*Dictionary<string, EnumNode> enums;
                    if (parentModule.getEnums().ContainsKey(variable.UnknownType))
                        enums = parentModule.getEnums();
                    else
                        enums = Modules[
                            ((EnumNode)parentModule.Imported[variable.UnknownType]).ParentModuleName
                        ].getEnums();
                    if (
                        !enums[variable.UnknownType].EnumElements.Contains(
                            ((VariableReferenceNode)ben.Right).Name
                        )
                    )
                    {
                        if ((variables.ContainsKey(((VariableReferenceNode)ben.Right).Name)))
                        {
                            if (
                                (
                                    variables[((VariableReferenceNode)ben.Right).Name].UnknownType
                                    != variable.UnknownType
                                )
                            )
                                throw new Exception(
                                    "Enums can only be compared to enums or enum variables of the same type."
                                );
                            break;
                        }

                        throw new Exception(
                            "Enums can only be compared to enums or enum variables of the same type."
                        );
                    }
                    */

                    break;
            }
        }
    }

    //may return null as GetRecordTypeRecursive can sometimes return null, although it should never reach here
    private static IType GetTargetTypeForAssignmentCheck(
        VariableNode targetDefinition,
        VariableReferenceNode targetUsage,
        ModuleNode parentModule
    ) =>
        targetDefinition.NewType switch
        {
            ArrayType a
                => targetUsage.ExtensionType == ASTNode.VrnExtType.None
                    ? throw new NotImplementedException(
                        "It is not implemented yet to assign to the base of an array variable."
                    )
                    : a.Inner,
            RecordType r
                => GetRecordTypeRecursive(
                    parentModule,
                    r,
                    targetUsage
                    
                ) ?? r,
            ReferenceType (RecordType r)
                => GetRecordTypeRecursive(
                    parentModule,
                    r,
                    targetUsage
                    
                ) ?? r,
            /*
            /*
            ReferenceType r
                => GetRecordTypeRecursive(
                    parentModule,
                    (RecordNode)
                        GetRecordsAndImports(parentModule.Records, parentModule.Imported)[
                            targetDefinition.GetUnknownTypeSafe()
                        ],
                    targetUsage
                ),
                */

            UnknownType u => throw new InvalidOperationException($"type should have fully resolved by this point {u}"),
            _ => targetDefinition.NewType
        };

    /*private static IType GetSpecificRecordType(
        ModuleNode parentModule,
        VariableNode targetDefinition,
        VariableReferenceNode targetUsage
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
    private static IType? GetRecordTypeRecursive(
        ModuleNode parentModule,
        RecordType targetDefinition,
        VariableReferenceNode targetUsage
    )
    {
        if (targetUsage.Extension is null)
            return null;
        var vndt = targetDefinition.Fields[targetUsage.GetRecordMemberReferenceSafe().Name];

        var innerVndt = vndt switch
        {
            RecordType r => r,
            ReferenceType(RecordType r1) => r1,
            _ => (RecordType?)null
        };
        return innerVndt is { } recordType
            ? GetRecordTypeRecursive(parentModule, recordType, (VariableReferenceNode)targetUsage.GetExtensionSafe()) ??
              vndt
            : vndt; 
        

        /*else
            return GetRecordTypeRecursive(
                    parentModule,
                    (RecordNode)
                        GetRecordsAndImports(parentModule.Records, parentModule.Imported)[
                            targetDefinition
                                .GetFromMembersByNameSafe(
                                    ((VariableReferenceNode)targetUsage.GetExtensionSafe()).Name
                                )
                                .GetUnknownTypeSafe()
                        ],
                    (VariableReferenceNode)targetUsage.GetExtensionSafe()
                */
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

    private static void CheckNode(
        IType targetType,
        ASTNode anExpression,
        IReadOnlyDictionary<string, VariableNode> variables,
        ModuleNode parentModule,
        VariableNode target
    )
    {
        switch (anExpression)
        {
            case BooleanExpressionNode booleanExpressionNode:
                if (targetType is not BooleanType)
                    throw new Exception(
                        "Boolean expressions have to be assigned to boolean variables"
                    );
                break;
            case BoolNode boolNode:
                if (targetType is not BooleanType)
                    throw new Exception(
                        "true and false must be assigned to boolean variables; found "
                        + boolNode.Value
                        + " assigned to "
                        + targetType
                    );
                break;
            case CharNode charNode:
                if (
                    targetType is not StringType or CharacterType
                )
                    throw new Exception("Characters have to be assigned to character variables");
                break;
            case FloatNode floatNode:
                if (targetType is not RealType)
                    throw new Exception("Real numbers have to be assigned to real variables");
                break;
            case IntNode intNode:
                if (targetType is not IntegerType)
                    throw new Exception(
                        "Integer numbers have to be assigned to integer variables. Found "
                        + targetType
                        + " "
                        + intNode.Value
                    );
                break;
            case MathOpNode mathOpNode:
                CheckNode(targetType, mathOpNode.Left, variables, parentModule, target);
                CheckNode(targetType, mathOpNode.Right, variables, parentModule, target);
                break;
            case StringNode stringNode:
                if (
                    targetType is not StringType or CharacterType
                )
                    throw new Exception("strings have to be assigned to string variables");
                break;
            case VariableReferenceNode vrn:
                if (targetType is EnumType t)
                {
                    // two cases enum constant foo.bar, or just plain variable
                    // TODO: make ast nodes for record access, enum access, and array index
                    if (!t.Variants.Contains(vrn.Name))
                    {
                        throw new SemanticErrorException("Could not assign assign to a variant of a different enum",
                            vrn);
                    }
                    //     EnumNode? enumDefinition = null;
                    //     foreach (var e in parentModule.getEnums())
                    //     {
                    //         foreach (var v in variables)
                    //         {
                    //             if (v.Value.InitialValue is null)
                    //                 continue;
                    //             if (
                    //                 v.Value.IsConstant
                    //                 && e.Value.EnumElements.Contains(v.Value.InitialValue.ToString())
                    //             )
                    //             {
                    //                 enumDefinition = e.Value;
                    //                 break;
                    //             }
                    //         }
                    //
                    //         if (e.Value.EnumElements.Contains(vrn.Name))
                    //         {
                    //             enumDefinition = e.Value;
                    //             break;
                    //         }
                    //     }
                    //
                    //     foreach (var e in parentModule.Imported)
                    //     {
                    //         if (e.Value is not EnumNode)
                    //             continue;
                    //         var Enum = (EnumNode)e.Value;
                    //         if (Enum.EnumElements.Contains(vrn.Name))
                    //         {
                    //             enumDefinition = Enum;
                    //             break;
                    //         }
                    //     }
                    //
                    //     if (enumDefinition == null)
                    //         throw new Exception(
                    //             $"Could not find the definition for an enum containing the element {vrn.Name}."
                    //         );
                    //     if (
                    //         enumDefinition.NewType != target.UnknownType
                    //         && (
                    //             target.NewType != VariableNode.DataType.Record
                    //             && target.NewType != VariableNode.DataType.Reference
                    //         )
                    //     )
                    //         throw new Exception(
                    //             $"Cannot assign an enum of type {enumDefinition.NewType} to the enum element {target.UnknownType}."
                    //         );
                }
                else
                {
                    var vn = variables[vrn.Name];
                    if (!vn.GetSpecificType(parentModule, vrn).Equals(targetType))
                        throw new Exception(
                            vrn.Name
                            + " is a "
                            + variables[vrn.Name].NewType
                            + " and can't be assigned to a "
                            + targetType
                        );
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(anExpression));
        }
    }

    // Only used by ShankUnitTests project.
    public static void CheckModules()
    {
        Modules = Interpreter.getModules();
        setStartModule();
        HandleExports();
        HandleImports();
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

            CheckFunctions(module.Value.getFunctions(), module.Value);
        }
    }

    public static void CheckModules(ProgramNode pn)
    {
        Modules = pn.Modules;
        StartModule = pn.GetStartModuleSafe();
        HandleExports();
        HandleImports();
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

            CheckFunctions(module.Value.getFunctions(), module.Value);
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

    public static void handleUnknownTypes()
    {
        foreach (KeyValuePair<string, ModuleNode> currentModule in Modules)
        {
            foreach (
                KeyValuePair<string, CallableNode> function in currentModule.Value.getFunctions()
            )
            {
                if (function.Value is BuiltInFunctionNode)
                {
                    continue;
                }

                FunctionNode currentFunction = (FunctionNode)function.Value;
                foreach (VariableNode variable in currentFunction.LocalVariables)
                {
                    var enumsAndImports = GetEnumsAndImports(
                        currentModule.Value.getEnums(),
                        currentModule.Value.Imported
                    );
                    var recordsAndImports = GetRecordsAndImports(
                        currentModule.Value.Records,
                        currentModule.Value.Imported
                    );
                    if (variable.NewType is UnknownType unknownType)
                    {
                        if (enumsAndImports.TryGetValue(unknownType.TypeName, out var enumType)
                            && enumType is EnumNode e
                           )
                        {
                            variable.NewType = e.NewType;
                            if (unknownType.TypeParameters.Count != 0)
                            {
                                throw new SemanticErrorException("type parameter are not allowed for enums", variable);
                            }
                        }
                        else if (
                            variable.NewType is UnknownType u
                            && recordsAndImports.TryGetValue(u.TypeName, out var type)
                            && type is RecordNode r
                        )
                        {
                            variable.NewType = r.NewType;
                            var allRecords = GetRecordsAndImports(
                                currentModule.Value.Records,
                                currentModule.Value.Imported
                            );

                            AssignNestedRecordTypes(r.Members, currentModule.Value);
                        }
                        else
                            throw new Exception(
                                "Could not find a definition for the unknown type "
                                + unknownType.TypeName
                            );
                    }
                }

                foreach (var statement in currentFunction.Statements)
                {
                    if (statement is AssignmentNode assignment)
                    {

                        if (currentFunction.LocalVariables.Find(node =>
                                node.NewType is EnumType && assignment.Target.Name == node.Name) is not null)
                        {
                            assignment.Target.ExtensionType = ASTNode.VrnExtType.Enum;
                        }
                    }
                }
            }
        }
    }

    private static bool Lookup<K, U, V>(Dictionary<K, V> dictionary, K key, ref U result) where U : class?
    {
        return dictionary.TryGetValue(key, out var value) && (value is U v && (result = v) == v);
    }

    public static void AssignNestedRecordTypes(
        List<StatementNode> statements,
        ModuleNode parentModule
    )
    {
        foreach (var statement in statements)
        {
            var rmn = (RecordMemberNode)statement;
            if (rmn.NewType is UnknownType u)
            {
                if (
                    parentModule.Records.TryGetValue(u.TypeName, out var record) ||
                    Lookup(parentModule.Imported, u.TypeName, ref record)
                )
                {
                    rmn.NewType = record!.NewType;
                }
                else if (
                    parentModule.Enums.TryGetValue(u.TypeName, out var enumType) ||
                    Lookup(parentModule.Imported, u.TypeName, ref enumType)
                )
                {
                    rmn.NewType = enumType!.NewType;
                }
            }
        }
    }

    public static void Experimental()
    {
        var a = GetStartModuleSafe()
            .GetContents<ModuleNode>(AstNodeContentsCollectors.ContentsCollector);
        OutputHelper.DebugPrintTxt(
            string.Join("\n", a.Select((n, i) => i + ": " + n.NodeName + " `" + n + "'")),
            "getContents"
        );
    }

    public static void reset()
    {
        Modules = new Dictionary<string, ModuleNode>();
        StartModule = null;
        Interpreter.testOutput = new StringBuilder();
        Program.UnitTestResults = new();
    }
}