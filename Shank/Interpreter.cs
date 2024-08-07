using System.Diagnostics;
using System.Text;
using Shank.ASTNodes;
using Shank.MathOppable;

namespace Shank;

public class Interpreter
{
    public static Dictionary<string, ModuleNode>? Modules { get; set; }

    public delegate bool AddVunToPassedTrier(
        ExpressionNode e,
        Dictionary<string, InterpreterDataType> variables,
        List<InterpreterDataType> passed
    );

    public static ModuleNode? StartModule { get; set; }
    public static StringBuilder testOutput = new StringBuilder();
    public static InterpretOptions? ActiveInterpretOptions { get; set; }

    public static bool GetVuopTestFlag()
    {
        return ActiveInterpretOptions?.VuOpTest ?? false;
    }

    public static Dictionary<string, ModuleNode> GetModulesSafe() =>
        Modules ?? throw new InvalidOperationException("Expected Modules to not be null.");

    public static List<ModuleNode> GetModulesAsList() =>
        GetModulesSafe().Select(kvp => kvp.Value).ToList();

    private static Dictionary<string, InterpreterDataType> GetVariablesDictionary(
        FunctionNode fn,
        IReadOnlyList<InterpreterDataType> args
    )
    {
        return fn.VariablesInScope.Select(kvp =>
        {
            // If the current VariablesInScope element is in ParameterVariables, paramIdx stores its
            // index in ParameterVariables, or else it stores -1.
            var paramIdx = Enumerable
                .Range(0, fn.ParameterVariables.Count)
                .FirstOrDefault(i => kvp.Key.Equals(fn.ParameterVariables[i].GetNameSafe()), -1);

            // We assume there is an IDT in args at the same index position as its corresponding VDN
            // in ParameterVariables.
            if (paramIdx >= 0)
                return new KeyValuePair<string, InterpreterDataType>(kvp.Key, args[paramIdx]);

            return new KeyValuePair<string, InterpreterDataType>(
                kvp.Key,
                kvp.Value.Type.ToIdt(kvp.Value.InitialValue)
            );
        })
            .ToDictionary();
    }

    private static ModuleNode GetStartModuleSafe() =>
        StartModule
        ?? throw new InvalidOperationException("Expected Interpreter._startModule to not be Null.");

    /// <summary>
    /// Interpret the given function.
    /// </summary>
    /// <param name="fn">The function being interpreted</param>
    /// <param name="parametersIDTs">Parameters passed in (already in IDT form)</param>
    /// <param name="maybeModule">The function's module</param>
    /// <exception cref="Exception"></exception>
    public static void InterpretFunction(
        FunctionNode fn,
        List<InterpreterDataType> parametersIDTs,
        ModuleNode? maybeModule = null
    )
    {
        var variables = GetVariablesDictionary(fn, parametersIDTs);

        if (fn is TestNode)
        {
            bool foundTestResult = false;
            foreach (var testResult in Program.UnitTestResults)
            {
                if (testResult.parentFunctionName == (((TestNode)fn).targetFunctionName))
                {
                    foundTestResult = true;
                    break;
                }
            }
            if (!foundTestResult)
            {
                Program.UnitTestResults.AddLast(
                    new TestResult(((TestNode)fn).Name, ((TestNode)fn).targetFunctionName)
                );
                Program.UnitTestResults.Last().lineNum = fn.LineNum;
            }
        }
        // Interpret instructions
        if (GetVuopTestFlag())
        {
            NewInterpretBlock(fn.Statements, variables, fn);
        }
        else
        {
            InterpretBlock(fn.Statements, variables, fn);
        }
    }

    // public static bool InterpretLoop(WhileNode loopNode, Dictionary<string, InterpreterDataType> variables)
    // {
    //     //look at the first statment node in while node. childern make sure is a builtinfunction node of name
    //     //times ( parse time) then execute
    //     //call ProcessFunctionCall
    //     if (!(loopNode.Expression is VariableUsageNodeTemp variable))
    //     {
    //         throw new ArgumentException("InterpretLoop, Loop count must be an integer.");
    //     }
    //     int loopCount = NewResolveInt(variable, variables);
    //     //ProcessFunctionCall(loopCount)
    //     var iterator = new IteratorDataType(loopCount);
    //     List<InterpreterDataType> parameters = new List<InterpreterDataType>
    //     {
    //         new IntDataType(loopCount),
    //         iterator
    //     };
    //     BuiltInFunctions.Times(parameters);
    //     var iteratorDataType = parameters[1] as IteratorDataType;
    //     return iteratorDataType.Value.MoveNext();
    // }

    private static void InterpretBlock(
        List<StatementNode> fnStatements,
        Dictionary<string, InterpreterDataType> variables,
        CallableNode callingFunction
    )
    {
        foreach (var stmt in fnStatements)
        {
            if (stmt is AssignmentNode an)
            {
                var target = variables[an.Target.Name];
                switch (target)
                {
                    case IntDataType it:
                        it.Value = ResolveInt(an.Expression, variables);
                        break;
                    case ArrayDataType at:
                        AssignToArray(at, an, variables);
                        break;
                    case FloatDataType ft:
                        ft.Value = ResolveFloat(an.Expression, variables);
                        break;
                    case StringDataType st:
                        st.Value = ResolveString(an.Expression, variables);
                        break;
                    case CharDataType ct:
                        ct.Value = ResolveChar(an.Expression, variables);
                        break;
                    case BooleanDataType bt:
                        bt.Value = ResolveBool(an.Expression, variables);
                        break;
                    case RecordDataType rt:
                        AssignToRecord(rt, an, variables);
                        break;
                    case EnumDataType et:
                        et.Value = ResolveEnum(et, an.Expression, variables);
                        break;
                    case ReferenceDataType rt:
                        if (rt.Record == null)
                            throw new Exception(
                                $"{an.Target.Name} must be allocated before it can be addressed."
                            );
                        AssignToRecord(rt.Record, an, variables);
                        break;
                    default:
                        throw new Exception("Unknown type in assignment");
                }
            }
            else if (stmt is FunctionCallNode fc)
            {
                ProcessFunctionCall(variables, fc, callingFunction);
            }
            else if (stmt is IfNode ic)
            {
                var theIc = ic;
                while (theIc?.Expression != null)
                {
                    if (
                        theIc.Expression != null
                        && ResolveBool(theIc.Expression, variables)
                        && theIc?.Children != null
                    )
                    {
                        InterpretBlock(theIc.Children, variables, callingFunction);
                        theIc = null;
                    }
                    else
                        theIc = theIc?.NextIfNode;
                }

                if (theIc?.Children != null)
                    InterpretBlock(theIc.Children, variables, callingFunction);
            }
            else if (stmt is WhileNode wn)
            {
                while (ResolveBool(wn.Expression, variables))
                {
                    InterpretBlock(wn.Children, variables, callingFunction);
                }
            }
            else if (stmt is RepeatNode rn)
            {
                do
                {
                    InterpretBlock(rn.Children, variables, callingFunction);
                } while (!ResolveBool(rn.Expression, variables));
            }
            else if (stmt is ForNode fn)
            {
                var target = variables[fn.Variable.Name];
                if (target is not IntDataType index)
                    throw new Exception(
                        $"For loop has a non-integer index called {fn.Variable.Name}. This is not allowed."
                    );
                var start = ResolveInt(fn.From, variables);
                var end = ResolveInt(fn.To, variables);
                for (var i = start; i <= end; i++)
                {
                    index.Value = i;
                    InterpretBlock(fn.Children, variables, callingFunction);
                }
            }
        }
    }

    private static void NewInterpretBlock(
        List<StatementNode> fnStatements,
        Dictionary<string, InterpreterDataType> variables,
        CallableNode callingFunction
    )
    {
        foreach (var stmt in fnStatements)
        {
            if (stmt is AssignmentNode an)
            {
                var idtTarget = GetIdtFromVun(variables, an.NewTarget);
                switch (idtTarget)
                {
                    case IntDataType it:
                        it.Value = NewResolveInt(an.Expression, variables);
                        break;
                    case ArrayDataType at:
                        NewAssignToArray(
                            at,
                            an.Expression,
                            an.NewTarget as VariableUsageIndexNode
                                ?? throw new InterpreterErrorException(
                                    "Somehow we have an ArrayDataType idtTarget but a non-VariableUsageIndexNode ASTNode target.",
                                    an.NewTarget
                                ),
                            variables
                        );
                        break;
                    case FloatDataType ft:
                        ft.Value = NewResolveFloat(an.Expression, variables);
                        break;
                    case StringDataType st:
                        st.Value = ResolveString(an.Expression, variables);
                        break;
                    case CharDataType ct:
                        ct.Value = ResolveChar(an.Expression, variables);
                        break;
                    case BooleanDataType bt:
                        bt.Value = ResolveBool(an.Expression, variables);
                        break;
                    case RecordDataType rt:
                        NewAssignToRecord(
                            rt,
                            an.Expression,
                            an.NewTarget as VariableUsageMemberNode
                                ?? throw new InterpreterErrorException(
                                    "Somehow we have a RecordDataType idtTarget but a non-VariableUsageMemberNode ASTNode target.",
                                    an.NewTarget
                                ),
                            variables
                        );
                        break;
                    case EnumDataType et:
                        et.Value = ResolveEnum(et, an.Expression, variables);
                        break;
                    case ReferenceDataType rt:
                        if (rt.Record is null)
                            throw new InterpreterErrorException(
                                "References must be allocated before they can be addressed.",
                                an.NewTarget
                            );
                        NewAssignToRecord(
                            rt.Record,
                            an.Expression,
                            an.NewTarget as VariableUsageMemberNode
                                ?? throw new InterpreterErrorException(
                                    "Somehow we have a ReferenceDataType idtTarget but a non-VariableUsageMemberNode ASTNode target.",
                                    an.NewTarget
                                ),
                            variables
                        );
                        break;
                    default:
                        throw new Exception("Unknown type in assignment");
                }
            }
            else if (stmt is FunctionCallNode fc)
            {
                ProcessFunctionCall(variables, fc, callingFunction);
            }
            else if (stmt is IfNode ic)
            {
                var theIc = ic;
                while (theIc?.Expression != null)
                {
                    if (
                        theIc.Expression != null
                        && ResolveBool(theIc.Expression, variables)
                        && theIc?.Children != null
                    )
                    {
                        NewInterpretBlock(theIc.Children, variables, callingFunction);
                        theIc = null;
                    }
                    else
                        theIc = theIc?.NextIfNode;
                }

                if (theIc?.Children != null)
                    NewInterpretBlock(theIc.Children, variables, callingFunction);
            }
            else if (stmt is WhileNode wn)
            {
                while (ResolveBool(wn.Expression, variables))
                {
                    NewInterpretBlock(wn.Children, variables, callingFunction);
                }
            }
            else if (stmt is RepeatNode rn)
            {
                do
                {
                    NewInterpretBlock(rn.Children, variables, callingFunction);
                } while (!ResolveBool(rn.Expression, variables));
            }
            else if (stmt is ForNode fn)
            {
                var target = GetIdtFromVun(variables, fn.NewVariable);
                if (target is not IntDataType index)
                    throw new Exception(
                        $"For loop has a non-integer index called {fn.Variable.Name}. This is not allowed."
                    );
                var start = NewResolveInt(fn.From, variables);
                var end = NewResolveInt(fn.To, variables);
                for (var i = start; i <= end; i++)
                {
                    index.Value = i;
                    NewInterpretBlock(fn.Children, variables, callingFunction);
                }
            }
        }
    }

    private static void AssignToRecord(
        RecordDataType rdt,
        AssignmentNode an,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        if (an.Target.GetExtensionSafe() is VariableUsagePlainNode vrn)
        {
            var t = rdt.GetMemberType(vrn.Name);
            rdt.Value[vrn.Name] = t switch
            {
                BooleanType => ResolveBool(an.Expression, variables),
                StringType => ResolveString(an.Expression, variables),
                RealType => ResolveFloat(an.Expression, variables),
                IntegerType => ResolveInt(an.Expression, variables),
                CharacterType => ResolveChar(an.Expression, variables),
                InstantiatedType => ResolveRecord(an.Expression, variables),
                ReferenceType => ResolveReference(an.Expression, variables),
                EnumType => ResolveEnum(an.Expression, variables),

                _
                    => throw new NotImplementedException(
                        "Assigning a value of type "
                            + t
                            + " to a record variable member is not implemented yet."
                    )
            };
        }
        else
        {
            throw new NotImplementedException(
                "Assigning any value to a record variable base is not implemented yet."
            );
        }
    }

    private static void NewAssignToRecord(
        RecordDataType rdt,
        ExpressionNode e,
        VariableUsageMemberNode target,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        //if (!rdt.Value.TryAdd(target.Right.Name, GetIdtFromExpr(e, variables)))
        //    throw new InterpreterErrorException("Could not add the value to the record.", target);
        rdt.Value[target.Right.Name] = GetIdtFromExpr(e, variables);

        //var t = rdt.GetMemberType(mev.Contents.Name);

        //rdt.Value[mev.Contents.Name] = t switch
        //{
        //    BooleanType => ResolveBool(an.Expression, variables),
        //    StringType => ResolveString(an.Expression, variables),
        //    RealType => NewResolveFloat(an.Expression, variables),
        //    IntegerType => NewResolveInt(an.Expression, variables),
        //    CharacterType => ResolveChar(an.Expression, variables),
        //    InstantiatedType => ResolveRecord(an.Expression, variables),
        //    ReferenceType => ResolveReference(an.Expression, variables),
        //    EnumType => ResolveEnum(an.Expression, variables),

        //    _
        //        => throw new NotImplementedException(
        //            "Assigning a value of type "
        //                + t
        //                + " to a record variable member is not implemented yet."
        //        )
        //};
    }

    private static void AssignToArray(
        ArrayDataType adt,
        AssignmentNode an,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        if (an.Target.Extension is { } idx)
        {
            adt.AddElement(
                adt.ArrayContentsType switch
                {
                    IntegerType => ResolveInt(an.Expression, variables),
                    RealType => ResolveFloat(an.Expression, variables),
                    StringType => ResolveString(an.Expression, variables),
                    CharacterType => ResolveChar(an.Expression, variables),
                    BooleanType => ResolveBool(an.Expression, variables),
                    _
                        => throw new NotImplementedException(
                            "Assigning a value of type "
                                + adt.ArrayContentsType
                                + " to an array index is not implemented yet."
                        )
                },
                ResolveInt(idx, variables)
            );
        }
        else
        {
            throw new NotImplementedException(
                "Assigning any value to an array variable base is not implemented yet."
            );
        }
    }

    private static void NewAssignToArray(
        ArrayDataType adt,
        ExpressionNode e,
        VariableUsageIndexNode target,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        adt.AddElement(GetIdtFromExpr(e, variables), NewResolveInt(target.Right, variables));

        //adt.AddElement(
        //    adt.ArrayContentsType switch
        //    {
        //        IntegerType => NewResolveInt(e, variables),
        //        RealType => NewResolveFloat(e, variables),
        //        StringType => ResolveString(e, variables),
        //        CharacterType => ResolveChar(e, variables),
        //        BooleanType => ResolveBool(e, variables),
        //        _
        //            => throw new NotImplementedException(
        //                "Assigning a value of type "
        //                    + adt.ArrayContentsType
        //                    + " to an array index is not implemented yet."
        //            )
        //    },
        //    NewResolveInt(target.Right, variables)
        //);
    }

    private static bool ResolveBool(ASTNode node, Dictionary<string, InterpreterDataType> variables)
    {
        if (node is BooleanExpressionNode ben)
            return EvaluateBooleanExpressionNode(ben, variables);
        else if (node is BoolNode bn)
            return bn.Value;
        else if (node is VariableUsagePlainNode vrn)
            return ((BooleanDataType)variables[vrn.Name]).Value;
        else
            throw new ArgumentException(nameof(node));
    }

    private static string ResolveEnum(
        EnumDataType target,
        ASTNode node,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        if (node is VariableUsagePlainNode variable)
        {
            //if the variable is a variable and not an enum reference
            if (variables.ContainsKey(variable.Name))
            {
                return ((EnumDataType)variables[variable.Name]).Value;
            }
            else
            {
                return variable.Name;
            }
        }
        throw new Exception("Enums must be assigned Enums");
    }

    private static EnumDataType ResolveEnum(
        ASTNode node,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        if (node is VariableUsagePlainNode vrn)
        {
            return new EnumDataType(vrn.Name);
        }
        throw new Exception("Enums must be assigned Enums");
    }

    // TODO: Clean up this method
    private static void ProcessFunctionCall(
        Dictionary<string, InterpreterDataType> variables,
        FunctionCallNode fc,
        CallableNode callingFunction
    )
    {
        ASTNode? calledFunction = null;
        bool callingModuleCanAccessFunction = false;

        if (StartModule == null)
            throw new Exception("Interpreter error, start function could not be found.");

        if (StartModule.getFunctions().ContainsKey(fc.Name))
            calledFunction = StartModule.getFunctions()[fc.Name]; // find the function
        else if (StartModule.getImportedFunctions().ContainsKey(fc.Name))
            calledFunction = StartModule.getImportedFunctions()[fc.Name];
        else
        {
            //this loop allows tests to search their own module for functions
            if (callingFunction is TestNode)
            {
                var module = Modules[callingFunction.parentModuleName];
                foreach (var function in module.getFunctions())
                {
                    if (function.Key == fc.Name)
                    {
                        calledFunction = function.Value;
                        callingModuleCanAccessFunction = true;
                    }
                }
            }
            if (calledFunction == null)
            {
                throw new Exception(
                    "Could not find the function "
                        + fc.Name
                        + " in the module "
                        + StartModule.getName()
                        + ". It may not have been exported."
                );
            }
        }

        //if the function was defined in this module, this module can access it
        if (Modules[callingFunction.parentModuleName].getFunctions().ContainsKey(fc.Name))
            callingModuleCanAccessFunction = true;
        //check the whole dictonary that correlates a module name to the list of functions that should be useabe in this module
        foreach (
            string? moduleName in Modules[callingFunction.parentModuleName].getImportNames().Keys
        )
        {
            //if we find the function, it means that the other module exported it, and this module imported it
            if (
                Modules[callingFunction.parentModuleName]
                    .getImportNames()[moduleName]
                    .Contains(fc.Name)
            )
            {
                callingModuleCanAccessFunction = true;
                break;
            }
        }
        //check if the function is a builtIn, which all modules should have access to,
        //but they're only stored in the module with the start function to prevent collisions
        if (StartModule.getFunctions().ContainsKey(fc.Name))
            if (StartModule.getFunctions()[fc.Name] is BuiltInFunctionNode)
                callingModuleCanAccessFunction = true;

        //if we haven't found the function name, its an error at this point
        if (!callingModuleCanAccessFunction)
            throw new Exception(
                "Cannot access the private function "
                    + ((CallableNode)calledFunction).Name
                    + " from module "
                    + callingFunction.parentModuleName
            );

        int requiredArgumentCount = 0;
        foreach (var variableDeclarationNode in ((CallableNode)calledFunction).ParameterVariables)
        {
            if (variableDeclarationNode.IsDefaultValue)
                requiredArgumentCount++;
        }

        if (
            fc.Arguments.Count < requiredArgumentCount
            || fc.Arguments.Count > ((CallableNode)calledFunction).ParameterVariables.Count //!= ((CallableNode)calledFunction).ParameterVariables.Count
                && calledFunction is not BuiltInVariadicFunctionNode
        ) // make sure that the counts match
            throw new Exception(
                $"Call of {((CallableNode)calledFunction).Name}, parameter count doesn't match."
            );
        // make the list of parameters
        var passed = new List<InterpreterDataType>();
        foreach (var fcp in fc.Arguments)
        {
            AddVunToPassedTrier addVunToPassedTrier = GetVuopTestFlag()
                ? NewTryAddVunToPassed
                : TryAddVunToPassed;
            if (!addVunToPassedTrier(fcp, variables, passed))
            {
                var value = fcp;
                switch (value)
                {
                    // is a constant
                    case IntNode intVal:
                        passed.Add(new IntDataType(intVal.Value));
                        break;
                    case FloatNode floatVal:
                        passed.Add(new FloatDataType(floatVal.Value));
                        break;
                    case StringNode stringVal:
                        passed.Add(new StringDataType(stringVal.Value));
                        break;
                    case CharNode charVal:
                        passed.Add(new CharDataType(charVal.Value));
                        break;
                    case BoolNode boolVal:
                        passed.Add(new BooleanDataType(boolVal.Value));
                        break;
                    case BooleanExpressionNode booleanExpressionVal:
                        passed.Add(
                            new BooleanDataType(ResolveBool(booleanExpressionVal, variables))
                        );
                        break;
                    case MathOpNode mathVal:
                        switch (mathVal.Left)
                        {
                            case IntNode:
                                passed.Add(new IntDataType(ResolveInt(value, variables)));
                                break;
                            case FloatNode:
                                passed.Add(new FloatDataType(ResolveFloat(value, variables)));
                                break;
                            case StringNode:
                                passed.Add(new StringDataType(ResolveString(value, variables)));
                                break;
                            case VariableUsagePlainNode variableUsage:
                                switch (variables[variableUsage.Name])
                                {
                                    case IntDataType:
                                        passed.Add(new IntDataType(ResolveInt(value, variables)));
                                        break;
                                    case FloatDataType:
                                        passed.Add(
                                            new FloatDataType(ResolveFloat(value, variables))
                                        );
                                        break;
                                    case StringDataType:
                                        passed.Add(
                                            new StringDataType(ResolveString(value, variables))
                                        );
                                        break;
                                    case CharDataType:
                                        passed.Add(new CharDataType(ResolveChar(value, variables)));
                                        break;
                                    default:
                                        throw new Exception(
                                            $"Call of {((CallableNode)calledFunction).Name}, constant parameter of unknown type."
                                        );
                                }
                                break;
                            default:
                                throw new Exception(
                                    $"Call of {((CallableNode)calledFunction).Name}, constant parameter of unknown type."
                                );
                        }
                        break;
                    default:
                        throw new Exception(
                            $"Call of {((CallableNode)calledFunction).Name}, constant parameter of unknown type."
                        );
                }
            }
        }
        if (fc.Name == "assertIsEqual")
        {
            AssertResult ar = new AssertResult(callingFunction.Name);
            Program
                .UnitTestResults.ElementAt(Program.UnitTestResults.Count - 1)
                .Asserts.AddLast(ar);
            Program
                .UnitTestResults.ElementAt(Program.UnitTestResults.Count - 1)
                .Asserts.Last()
                .lineNum = fc.LineNum;
        }

        // Execute the callee if its `Execute` field is not null.
        ((CallableNode)calledFunction).Execute?.Invoke(passed);

        //// update the variable parameters and return
        //for (var i = 0; i < passed.Count; i++)
        //{
        //    // TODO: It's hard to follow what the logic is here.
        //    if (
        //        (
        //            (calledFunction is BuiltInVariadicFunctionNode)
        //            || !((CallableNode)calledFunction).ParameterVariables[i].IsConstant
        //        ) && fc.Arguments[i] is VariableUsagePlainNode variableUsagePlainNode
        //    )
        //    {
        //        // if this parameter is a "var", then copy the new value back to the parameter holder
        //        if (variableUsagePlainNode.IsInFuncCallWithVar)
        //            variables[variableUsagePlainNode.Name] = passed[i];
        //    }
        //}
        if (GetVuopTestFlag())
            NewUpdatePassedParamsPostCall(passed, (CallableNode)calledFunction, fc, variables);
        else
            UpdatePassedParamsPostCall(passed, (CallableNode)calledFunction, fc, variables);
    }

    private static void UpdatePassedParamsPostCall(
        List<InterpreterDataType> passed,
        CallableNode calledFunc,
        FunctionCallNode theCallItself,
        Dictionary<string, InterpreterDataType> iDTs
    )
    {
        for (var i = 0; i < passed.Count; i++)
        {
            // IF ((our called function is a variadic builtin) OR (this param on the called function
            // is declared with `var`)) AND (this arg on the call itself is a variable)
            // With this logic, we're allowing variadic builtins to bypass the requirement that in
            // order to have its params updated after the call, a callable must have those params
            // declared with their IsConstant properties set to false.
            // We're (probably) doing this because variadic builtins don't have declared parameters.
            if (
                (
                    (calledFunc is BuiltInVariadicFunctionNode)
                    || !calledFunc.ParameterVariables[i].IsConstant
                ) && theCallItself.Arguments[i] is VariableUsagePlainNode variableUsagePlainNode
            )
            {
                // if this parameter is a "var", then copy the new value back to the parameter holder
                if (variableUsagePlainNode.IsInFuncCallWithVar)
                    iDTs[variableUsagePlainNode.Name] = passed[i];
            }
        }
    }

    private static void NewUpdatePassedParamsPostCall(
        List<InterpreterDataType> passed,
        CallableNode calledFunc,
        FunctionCallNode theCallItself,
        Dictionary<string, InterpreterDataType> iDTs
    )
    {
        for (var i = 0; i < passed.Count; i++)
        {
            // IF ((our called function is a variadic builtin) OR (this param on the called function
            // is declared with `var`)) AND (this arg on the call itself is a variable)
            // With this logic, we're allowing variadic builtins to bypass the requirement that in
            // order to have its params updated after the call, a called function must declare those
            // params with their IsConstant properties set to false.
            // We're (probably) doing this because variadic builtins don't have declared parameters.
            if (
                (
                    calledFunc is BuiltInVariadicFunctionNode
                    || !calledFunc.ParameterVariables[i].IsConstant
                ) && theCallItself.Arguments[i] is VariableUsageNodeTemp vun
            )
            {
                // TODO: We need a way to update only part of an IDT if this passed param is only
                // part of a VUN.
                if (vun.NewIsInFuncCallWithVar)
                    iDTs[vun.GetPlain().Name] = passed[i];
            }
        }
    }

    private static void AddToParamsArray(
        ArrayDataType adt,
        VariableUsagePlainNode args,
        List<InterpreterDataType> paramsList,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        if (args.Extension is { } i)
        {
            // Passing in one element of the array.
            var index = ResolveInt(i, variables);
            switch (adt.ArrayContentsType)
            {
                case IntegerType:
                    paramsList.Add(new IntDataType(adt.GetElementInteger(index)));
                    break;
                case RealType:
                    paramsList.Add(new FloatDataType(adt.GetElementReal(index)));
                    break;
                case StringType:
                    paramsList.Add(new StringDataType(adt.GetElementString(index)));
                    break;
                case CharacterType:
                    paramsList.Add(new CharDataType(adt.GetElementCharacter(index)));
                    break;
                case BooleanType:
                    paramsList.Add(new BooleanDataType(adt.GetElementBoolean(index)));
                    break;
                default:
                    throw new Exception("Invalid ArrayContentsType");
            }
        }
        else
        {
            // Passing in the whole array as a new ADT.
            paramsList.Add(new ArrayDataType(adt.Value, adt.Type));
        }
    }

    private static void NewAddToParamsArray(
        ArrayDataType adt,
        VariableUsageIndexNode vi,
        List<InterpreterDataType> paramsList,
        Dictionary<string, InterpreterDataType> variables,
        FunctionNode callingFunction
    )
    {
        paramsList.Add(GetIdtFromVun(variables, vi));
    }

    private static void AddToParamsRecord(
        RecordDataType rdt,
        VariableUsagePlainNode args, //ParameterNode pn,
        List<InterpreterDataType> paramsList
    )
    {
        var pnVrn = args; //pn.GetVariableSafe();
        if (pnVrn.ExtensionType == VariableUsagePlainNode.VrnExtType.RecordMember)
        {
            var rmVrn = pnVrn.GetRecordMemberReferenceSafe();
            paramsList.Add(
                rdt.GetMemberType(rmVrn.Name) switch
                {
                    CharacterType => new CharDataType(rdt.GetValueCharacter(rmVrn.Name)),
                    BooleanType => new BooleanDataType(rdt.GetValueBoolean(rmVrn.Name)),
                    StringType => new StringDataType(rdt.GetValueString(rmVrn.Name)),
                    IntegerType => new IntDataType(rdt.GetValueInteger(rmVrn.Name)),
                    RealType => new FloatDataType(rdt.GetValueReal(rmVrn.Name)),
                    ReferenceType => new ReferenceDataType(rdt.GetValueReference(rmVrn.Name)),
                    RecordType
                        => GetNestedParam(
                            rdt,
                            args
                                ?? throw new Exception("Could not find extension for nested record")
                        ),
                    EnumType => rdt.GetValueEnum(rmVrn.Name),
                    _
                        => throw new NotImplementedException(
                            "It has not been implemented yet to pass a complex Record member"
                                + " type into a function."
                        )
                }
            );
        }
        else
        {
            paramsList.Add(new RecordDataType(rdt));
        }
    }

    private static void NewAddToParamsRecord(
        RecordDataType rdt,
        VariableUsagePlainNode args, //ParameterNode pn,
        List<InterpreterDataType> paramsList
    )
    {
        var pnVrn = args; //pn.GetVariableSafe();
        if (pnVrn.ExtensionType == VariableUsagePlainNode.VrnExtType.RecordMember)
        {
            var rmVrn = pnVrn.GetRecordMemberReferenceSafe();
            paramsList.Add(
                rdt.GetMemberType(rmVrn.Name) switch
                {
                    CharacterType => new CharDataType(rdt.GetValueCharacter(rmVrn.Name)),
                    BooleanType => new BooleanDataType(rdt.GetValueBoolean(rmVrn.Name)),
                    StringType => new StringDataType(rdt.GetValueString(rmVrn.Name)),
                    IntegerType => new IntDataType(rdt.GetValueInteger(rmVrn.Name)),
                    RealType => new FloatDataType(rdt.GetValueReal(rmVrn.Name)),
                    ReferenceType => new ReferenceDataType(rdt.GetValueReference(rmVrn.Name)),
                    RecordType
                        => GetNestedParam(
                            rdt,
                            args
                                ?? throw new Exception("Could not find extension for nested record")
                        ),
                    EnumType => rdt.GetValueEnum(rmVrn.Name),
                    _
                        => throw new NotImplementedException(
                            "It has not been implemented yet to pass a complex Record member"
                                + " type into a function."
                        )
                }
            );
        }
        else
        {
            paramsList.Add(new RecordDataType(rdt));
        }
    }

    public static InterpreterDataType GetNestedParam(RecordDataType rdt, VariableUsagePlainNode vn)
    {
        var temp = rdt.Value[((VariableUsagePlainNode)vn.GetExtensionSafe()).Name];
        if (
            temp is RecordDataType
            && ((VariableUsagePlainNode)vn.GetExtensionSafe()).Extension is null
        )
        {
            return GetNestedParam(
                (RecordDataType)temp,
                (VariableUsagePlainNode)vn.GetExtensionSafe()
            );
        }
        if (
            temp is ReferenceDataType
            && ((VariableUsagePlainNode)vn.GetExtensionSafe()).Extension is null
        )
        {
            return GetNestedParam(
                ((ReferenceDataType)temp).Record
                    ?? throw new Exception($"Reference was never allocated, {vn.ToString()}"),
                (VariableUsagePlainNode)vn.GetExtensionSafe()
            );
        }
        return temp switch
        {
            int => new IntDataType((int)temp),
            float => new FloatDataType((float)temp),
            string => new StringDataType((string)temp),
            char => new CharDataType((char)temp),
            bool => new BooleanDataType((bool)temp),
            EnumDataType type => new EnumDataType(type),
            RecordDataType type => new RecordDataType(type),
            ReferenceDataType type => new ReferenceDataType(type),
            _ => throw new Exception("Could not find nested type.")
        };
        throw new Exception("Could not get nested param");
    }

    private static bool Lookup<TK, TU, TV>(Dictionary<TK, TV> dictionary, TK key, ref TU result)
        where TU : class?
        where TK : notnull
    {
        return dictionary.TryGetValue(key, out var value) && (value is TU v && (result = v) == v);
    }

    // assumptions: already type checked/resolved all custom types
    private static InterpreterDataType VariableNodeToActivationRecord(VariableDeclarationNode vn)
    {
        var parentModule = GetModulesSafe()[vn.GetModuleNameSafe()];
        switch (vn.Type)
        {
            case RealType:
                return new FloatDataType(((vn.InitialValue as FloatNode)?.Value) ?? 0.0F);
            case IntegerType:
                return new IntDataType(((vn.InitialValue as IntNode)?.Value) ?? 0);
            case StringType:
                return new StringDataType(((vn.InitialValue as StringNode)?.Value) ?? "");
            case CharacterType:
                return new CharDataType(((vn.InitialValue as CharNode)?.Value) ?? ' ');
            case BooleanType:
                return new BooleanDataType(((vn.InitialValue as BoolNode)?.Value) ?? true);
            case ArrayType a:
            {
                return new ArrayDataType([], a);
            }
            // TODO: merge record type and record node into one
            case InstantiatedType r:
            {
                _ = (
                    parentModule.Records.TryGetValue(r.Inner.Name, out var record)
                    || Lookup(parentModule.Imported, r.Inner.Name, ref record)
                );
                return new RecordDataType(r);
            }
            case EnumType e:
            {
                _ = (
                    parentModule.Enums.TryGetValue(e.Name, out var enumNode)
                    || Lookup(parentModule.Imported, e.Name, ref enumNode)
                );
                return new EnumDataType(enumNode!);
            }

            case ReferenceType(InstantiatedType r):
            {
                _ = (
                    parentModule.Records.TryGetValue(r.Inner.Name, out var record)
                    || Lookup(parentModule.Imported, r.Inner.Name, ref record)
                );
                return new ReferenceDataType(r!);
            }
            default:
                throw new Exception($"Unknown local variable type");
        }
    }

    private static bool EvaluateBooleanExpressionNode(
        BooleanExpressionNode ben,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        try
        {
            var lf = ResolveFloat(ben.Left, variables);
            var rf = ResolveFloat(ben.Right, variables);
            return ben.Op switch
            {
                BooleanExpressionNode.BooleanExpressionOpType.lt => lf < rf,
                BooleanExpressionNode.BooleanExpressionOpType.le => lf <= rf,
                BooleanExpressionNode.BooleanExpressionOpType.gt => lf > rf,
                BooleanExpressionNode.BooleanExpressionOpType.ge => lf >= rf,
                BooleanExpressionNode.BooleanExpressionOpType.eq => lf == rf,
                BooleanExpressionNode.BooleanExpressionOpType.ne => lf != rf,
                _ => throw new Exception("Unknown boolean operation")
            };
        }
        catch { } // It might not have been a float operation

        try
        {
            var lf = ResolveInt(ben.Left, variables);
            var rf = ResolveInt(ben.Right, variables);
            return ben.Op switch
            {
                BooleanExpressionNode.BooleanExpressionOpType.lt => lf < rf,
                BooleanExpressionNode.BooleanExpressionOpType.le => lf <= rf,
                BooleanExpressionNode.BooleanExpressionOpType.gt => lf > rf,
                BooleanExpressionNode.BooleanExpressionOpType.ge => lf >= rf,
                BooleanExpressionNode.BooleanExpressionOpType.eq => lf == rf,
                BooleanExpressionNode.BooleanExpressionOpType.ne => lf != rf,
                _ => throw new Exception("Unknown boolean operation")
            };
        }
        catch { } // It might not have been an int operation

        try
        {
            var lf = ben.Left;
            var rf = ben.Right;
            if (rf is VariableUsagePlainNode right)
            {
                if (lf is VariableUsagePlainNode left)
                {
                    if (variables.ContainsKey(left.Name) && variables.ContainsKey(right.Name))
                    {
                        return ben.Op switch
                        {
                            BooleanExpressionNode.BooleanExpressionOpType.eq
                                => variables[left.Name].ToString()
                                    == variables[right.Name].ToString(),
                            BooleanExpressionNode.BooleanExpressionOpType.ne
                                => variables[left.Name].ToString()
                                    != variables[right.Name].ToString(),
                            BooleanExpressionNode.BooleanExpressionOpType.lt
                                => EnumLessThan(
                                    (EnumDataType)variables[left.Name],
                                    (EnumDataType)variables[right.Name]
                                ),
                            BooleanExpressionNode.BooleanExpressionOpType.gt
                                => !EnumLessThan(
                                    (EnumDataType)variables[left.Name],
                                    (EnumDataType)variables[right.Name]
                                ),
                            BooleanExpressionNode.BooleanExpressionOpType.le
                                => EnumLessThan(
                                    (EnumDataType)variables[left.Name],
                                    (EnumDataType)variables[right.Name]
                                )
                                    || variables[left.Name].ToString()
                                        == variables[right.Name].ToString(),
                            BooleanExpressionNode.BooleanExpressionOpType.ge
                                => !EnumLessThan(
                                    (EnumDataType)variables[left.Name],
                                    (EnumDataType)variables[right.Name]
                                )
                                    || variables[left.Name].ToString()
                                        == variables[right.Name].ToString(),
                            _ => throw new Exception("Enums can only be compared with <> and =.")
                        };
                    }
                    else if (!variables.ContainsKey(left.Name) && variables.ContainsKey(right.Name))
                    {
                        return ben.Op switch
                        {
                            BooleanExpressionNode.BooleanExpressionOpType.eq
                                => left.Name == variables[right.Name].ToString(),
                            BooleanExpressionNode.BooleanExpressionOpType.ne
                                => left.Name != variables[right.Name].ToString(),
                            BooleanExpressionNode.BooleanExpressionOpType.lt
                                => EnumLessThan((EnumDataType)variables[right.Name], left.Name),
                            BooleanExpressionNode.BooleanExpressionOpType.gt
                                => !EnumLessThan((EnumDataType)variables[right.Name], left.Name),
                            BooleanExpressionNode.BooleanExpressionOpType.le
                                => EnumLessThan((EnumDataType)variables[right.Name], left.Name)
                                    || left.Name == variables[right.Name].ToString(),
                            BooleanExpressionNode.BooleanExpressionOpType.ge
                                => !EnumLessThan((EnumDataType)variables[right.Name], left.Name)
                                    || left.Name == variables[right.Name].ToString(),
                            _ => throw new Exception("Enums can only be compared with <> and =.")
                        };
                    }
                    else if (variables.ContainsKey(left.Name) && !variables.ContainsKey(right.Name))
                    {
                        return ben.Op switch
                        {
                            BooleanExpressionNode.BooleanExpressionOpType.eq
                                => variables[left.Name].ToString() == right.Name,
                            BooleanExpressionNode.BooleanExpressionOpType.ne
                                => variables[left.Name].ToString() != right.Name,
                            BooleanExpressionNode.BooleanExpressionOpType.lt
                                => EnumLessThan((EnumDataType)variables[left.Name], right.Name),
                            BooleanExpressionNode.BooleanExpressionOpType.gt
                                => !EnumLessThan((EnumDataType)variables[left.Name], right.Name),
                            BooleanExpressionNode.BooleanExpressionOpType.le
                                => EnumLessThan((EnumDataType)variables[left.Name], right.Name)
                                    || right.Name == variables[left.Name].ToString(),
                            BooleanExpressionNode.BooleanExpressionOpType.ge
                                => !EnumLessThan((EnumDataType)variables[left.Name], right.Name)
                                    || right.Name == variables[left.Name].ToString(),
                            _ => throw new Exception("Enums can only be compared with <> and =.")
                        };
                    }
                }
            }
        }
        catch { } // It might not have been an enum to enum

        throw new Exception("Unable to calculate truth of expression.");
    }

    private static bool EnumLessThan(EnumDataType left, EnumDataType right)
    {
        var enumElements = left.Type.EType.Variants.ToArray();
        int leftIndex = 0,
            rightIndex = 0;
        for (int i = 0; i < enumElements.Length; i++)
        {
            if (enumElements[i] == left.Value)
                leftIndex = i;
            if (enumElements[i] == right.Value)
                rightIndex = i;
        }
        return leftIndex < rightIndex;
    }

    private static bool EnumLessThan(EnumDataType left, string right)
    {
        var enumElements = left.Type.EType.Variants.ToArray();
        int leftIndex = 0,
            rightIndex = 0;
        for (int i = 0; i < enumElements.Length; i++)
        {
            if (enumElements[i] == left.Value)
                leftIndex = i;
            if (enumElements[i] == right)
                rightIndex = i;
        }
        return leftIndex < rightIndex;
    }

    public static string ResolveString(
        ASTNode node,
        Dictionary<string, InterpreterDataType> variables
    ) =>
        node switch
        {
            StringNode sn => sn.Value,
            CharNode cn => cn.Value.ToString(),
            MathOpNode mon
                => mon.Op == MathOpNode.MathOpType.Plus
                    ? ResolveString(mon.Left, variables) + ResolveString(mon.Right, variables)
                    : throw new NotImplementedException(
                        "It has not been implemented to perform any math operation on"
                            + " strings other than addition."
                    ),
            VariableUsagePlainNode vrn
                => vrn.ExtensionType switch
                {
                    VariableUsagePlainNode.VrnExtType.ArrayIndex
                        => ((ArrayDataType)variables[vrn.Name]).GetElementString(
                            ResolveInt(vrn.GetExtensionSafe(), variables)
                        ),
                    VariableUsagePlainNode.VrnExtType.RecordMember
                        => ((RecordDataType)variables[vrn.Name]).GetValueString(
                            vrn.GetRecordMemberReferenceSafe().Name
                        ),
                    _ => ((StringDataType)variables[vrn.Name]).Value
                },
            _
                => throw new ArgumentOutOfRangeException(
                    nameof(node),
                    "The given ASTNode cannot be resolved to a string"
                )
        };

    public static char ResolveChar(ASTNode node, Dictionary<string, InterpreterDataType> variables)
    {
        if (node is CharNode cn)
        {
            return cn.Value;
        }

        if (node is VariableUsagePlainNode vrn)
        {
            return vrn.ExtensionType switch
            {
                VariableUsagePlainNode.VrnExtType.ArrayIndex
                    => ((ArrayDataType)variables[vrn.Name]).GetElementCharacter(
                        ResolveInt(vrn.GetExtensionSafe(), variables)
                    ),
                VariableUsagePlainNode.VrnExtType.RecordMember
                    => ((RecordDataType)variables[vrn.Name]).GetValueCharacter(
                        vrn.GetRecordMemberReferenceSafe().Name
                    ),
                _ => ((CharDataType)variables[vrn.Name]).Value
            };
        }

        throw new ArgumentException(
            "Can only resolve a CharNode or a VariableReferenceNode to a char.",
            nameof(node)
        );
    }

    public static float ResolveFloat(
        ASTNode node,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        if (node is MathOpNode mon)
        {
            var left = ResolveFloat(mon.Left, variables);
            var right = ResolveFloat(mon.Right, variables);
            switch (mon.Op)
            {
                case MathOpNode.MathOpType.Plus:
                    return left + right;
                case MathOpNode.MathOpType.Minus:
                    return left - right;
                case MathOpNode.MathOpType.Times:
                    return left * right;
                case MathOpNode.MathOpType.Divide:
                    return left / right;
                case MathOpNode.MathOpType.Modulo:
                    return left % right;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else if (node is FloatNode fn)
            return fn.Value;
        else if (node is VariableUsagePlainNode vrn)
        {
            return vrn.ExtensionType switch
            {
                VariableUsagePlainNode.VrnExtType.ArrayIndex
                    => ((ArrayDataType)variables[vrn.Name]).GetElementReal(
                        ResolveInt(vrn.GetExtensionSafe(), variables)
                    ),
                VariableUsagePlainNode.VrnExtType.RecordMember
                    => ((RecordDataType)variables[vrn.Name]).GetValueReal(
                        vrn.GetRecordMemberReferenceSafe().Name
                    ),
                _ => ((FloatDataType)variables[vrn.Name]).Value
            };
        }
        else
            throw new ArgumentException(nameof(node));
    }

    public static int ResolveInt(ASTNode node, Dictionary<string, InterpreterDataType> variables)
    {
        if (node is MathOpNode mon)
        {
            var left = ResolveInt(mon.Left, variables);
            var right = ResolveInt(mon.Right, variables);
            switch (mon.Op)
            {
                case MathOpNode.MathOpType.Plus:
                    return left + right;
                case MathOpNode.MathOpType.Minus:
                    return left - right;
                case MathOpNode.MathOpType.Times:
                    return left * right;
                case MathOpNode.MathOpType.Divide:
                    return left / right;
                case MathOpNode.MathOpType.Modulo:
                    return left % right;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else if (node is IntNode fn)
            return fn.Value;
        else if (node is VariableUsagePlainNode vrn)
        {
            return vrn.ExtensionType switch
            {
                VariableUsagePlainNode.VrnExtType.ArrayIndex
                    => ((ArrayDataType)variables[vrn.Name]).GetElementInteger(
                        ResolveInt(vrn.GetExtensionSafe(), variables)
                    ),
                VariableUsagePlainNode.VrnExtType.RecordMember
                    => ((RecordDataType)variables[vrn.Name]).GetValueInteger(
                        vrn.GetRecordMemberReferenceSafe().Name
                    ),
                _ => ((IntDataType)variables[vrn.Name]).Value
            };
        }
        else
            throw new ArgumentException(nameof(node));
    }

    public static int NewResolveInt(ASTNode node, Dictionary<string, InterpreterDataType> variables)
    {
        switch (node)
        {
            case MathOpNode m:
            {
                var left = NewResolveInt(m.Left, variables);
                var right = NewResolveInt(m.Right, variables);
                return m.GetResultOfOp(left, right);
            }
            case IntNode i:
                return i.Value;
            case VariableUsageNodeTemp v:
                return ((IntDataType)GetIdtFromVun(variables, v)).Value;
            default:
                throw new ArgumentException(
                    "Unsupported node type for resolving to int: " + node.GetType(),
                    nameof(node)
                );
        }
    }

    public static float NewResolveFloat(
        ASTNode node,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        switch (node)
        {
            case MathOpNode m:
            {
                var left = NewResolveFloat(m.Left, variables);
                var right = NewResolveFloat(m.Right, variables);
                return m.Op switch
                {
                    MathOpNode.MathOpType.Plus => left + right,
                    MathOpNode.MathOpType.Minus => left - right,
                    MathOpNode.MathOpType.Times => left * right,
                    MathOpNode.MathOpType.Divide => left / right,
                    MathOpNode.MathOpType.Modulo => left % right,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            case FloatNode f:
                return f.Value;
            case VariableUsageNodeTemp v:
                return ((FloatDataType)GetIdtFromVun(variables, v)).Value;
            default:
                throw new ArgumentException(
                    "Unsupported node type for resolving to float: " + node.GetType(),
                    nameof(node)
                );
        }
    }

    public static object ResolveRecord(
        ASTNode node,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        return ResolveReference(node, variables);
    }

    public static object ResolveReference(
        ASTNode node,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        switch (node)
        {
            case IntNode i:
                return ResolveInt(node, variables);
            case FloatNode f:
                return ResolveFloat(node, variables);
            case StringNode s:
                return ResolveString(node, variables);
            case CharNode c:
                return ResolveChar(node, variables);
            case BoolNode b:
                return ResolveBool(node, variables);
            case VariableUsagePlainNode v:
                return variables[v.Name];
            default:
                throw new Exception(
                    $"Error when assigning {node.ToString()} to a reference or record."
                );
        }
    }

    public static ModuleNode? setStartModule()
    {
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

    public static void SetStartModule()
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

    public static ModuleNode? getStartModule()
    {
        return StartModule;
    }

    public static int ResolveIntBeforeVarDecs(ASTNode node)
    {
        if (node is MathOpNode mon)
        {
            var left = ResolveIntBeforeVarDecs(mon.Left);
            var right = ResolveIntBeforeVarDecs(mon.Right);
            switch (mon.Op)
            {
                case MathOpNode.MathOpType.Plus:
                    return left + right;
                case MathOpNode.MathOpType.Minus:
                    return left - right;
                case MathOpNode.MathOpType.Times:
                    return left * right;
                case MathOpNode.MathOpType.Divide:
                    return left / right;
                case MathOpNode.MathOpType.Modulo:
                    return left % right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mon.Op), "Invalid operation type");
            }
        }
        else if (node is IntNode fn)
            return fn.Value;
        else if (node is VariableUsagePlainNode vr)
            throw new Exception(
                "Variable references not allowed before all variables are declared"
            );
        else
            throw new ArgumentException("Invalid node type for integer", nameof(node));
    }

    // Used for resetting the single static interpreter between unit tests.
    public static void Reset()
    {
        Modules = [];
        StartModule = null;
        testOutput = new StringBuilder();
        Program.UnitTestResults = [];
    }

    public static void setModules(Dictionary<string, ModuleNode> modules)
    {
        Modules = modules;
    }

    public static Dictionary<string, ModuleNode> getModules()
    {
        return Modules;
    }

    public static InterpreterDataType GetIdtFromVun(
        Dictionary<string, InterpreterDataType> idts,
        VariableUsageNodeTemp v
    )
    {
        (VariableUsageNodeTemp vc, var d) = v.GetPlainAndDepth();
        var ic = idts[((VariableUsagePlainNode)vc).Name];
        while (true)
        {
            d--;
            if (d < 0)
                break;
            vc = v.GetVunAtDepth(d);
            ic = ic.GetInnerIdt(vc, NewResolveInt, idts);
            if (ic is null)
                break;
        }

        return ic ?? throw new InvalidOperationException();
    }

    public static InterpreterDataType GetIdtFromExpr(
        ExpressionNode e,
        Dictionary<string, InterpreterDataType> idts
    )
    {
        switch (e)
        {
            case IntNode i:
                return new IntDataType(i.Value);
            case FloatNode f:
                return new FloatDataType(f.Value);
            case StringNode s:
                return new StringDataType(s.Value);
            case CharNode c:
                return new CharDataType(c.Value);
            case BoolNode b:
                return new BooleanDataType(b.Value);
            case VariableUsageNodeTemp v:
                return GetIdtFromVun(idts, v);
            case MathOpNode m:
            {
                var left = GetIdtFromExpr(m.Left, idts);
                var right = GetIdtFromExpr(m.Right, idts);
                var opResult = m.GetResultOfOp(left, right);
                switch (opResult)
                {
                    case MathOppableInt mi:
                        return new IntDataType(mi.Contents);
                    case MathOppableFloat fi:
                        return new FloatDataType(fi.Contents);
                    case MathOppableString si:
                        return new StringDataType(si.Contents);
                    default:
                        throw new UnreachableException();
                }
            }
            default:
                throw new ArgumentException("Unsupported expression type", nameof(e));
        }
    }

    public static bool TryAddVunToPassed(
        ExpressionNode e,
        Dictionary<string, InterpreterDataType> variables,
        List<InterpreterDataType> passed
    )
    {
        if (e is not VariableUsagePlainNode variableUsagePlainNode)
        {
            return false;
        }

        var name = variableUsagePlainNode.Name;
        var value = variables[name];
        switch (value)
        {
            case IntDataType intVal:
                passed.Add(new IntDataType(intVal.Value));
                break;
            case FloatDataType floatVal:
                passed.Add(new FloatDataType(floatVal.Value));
                break;
            case StringDataType stringVal:
                passed.Add(new StringDataType(stringVal.Value));
                break;
            case CharDataType charVal:
                passed.Add(new CharDataType(charVal.Value));
                break;
            case BooleanDataType boolVal:
                passed.Add(new BooleanDataType(boolVal.Value));
                break;
            case ArrayDataType arrayVal:
                AddToParamsArray(arrayVal, variableUsagePlainNode, passed, variables);
                break;
            case RecordDataType recordVal:
                AddToParamsRecord(recordVal, variableUsagePlainNode, passed);
                break;
            case EnumDataType enumVal:
                passed.Add(new EnumDataType(enumVal.Type, enumVal.Value));
                break;
            case ReferenceDataType referenceVal:
                if (variableUsagePlainNode.Extension != null)
                {
                    var vrn = ((VariableUsagePlainNode)variableUsagePlainNode.Extension);
                    if (referenceVal.Record is null)
                        throw new Exception($"{variableUsagePlainNode.Name} was never allocated.");
                    if (referenceVal.Record.Value[vrn.Name] is int)
                        passed.Add(new IntDataType((int)referenceVal.Record.Value[vrn.Name]));
                    else if (referenceVal.Record.Value[vrn.Name] is float)
                        passed.Add(new FloatDataType((float)referenceVal.Record.Value[vrn.Name]));
                    else if (referenceVal.Record.Value[vrn.Name] is char)
                        passed.Add(new CharDataType((char)referenceVal.Record.Value[vrn.Name]));
                    else if (referenceVal.Record.Value[vrn.Name] is string)
                        passed.Add(new StringDataType((string)referenceVal.Record.Value[vrn.Name]));
                    else if (referenceVal.Record.Value[vrn.Name] is float)
                        passed.Add(new BooleanDataType((bool)referenceVal.Record.Value[vrn.Name]));
                }
                else
                    passed.Add(new ReferenceDataType(referenceVal));
                break;
        }

        return true;
    }

    public static bool NewTryAddVunToPassed(
        ExpressionNode ex,
        Dictionary<string, InterpreterDataType> variables,
        List<InterpreterDataType> passed
    )
    {
        if (ex is not VariableUsageNodeTemp vun)
        {
            return false;
        }

        var val = GetIdtFromVun(variables, vun);

        passed.Add(
            val switch
            {
                IntDataType i => i.CopyAs<IntDataType>(),
                FloatDataType f => f.CopyAs<FloatDataType>(),
                StringDataType s => s.CopyAs<StringDataType>(),
                CharDataType c => c.CopyAs<CharDataType>(),
                BooleanDataType b => b.CopyAs<BooleanDataType>(),
                ArrayDataType a => a.CopyAs<ArrayDataType>(),
                RecordDataType r => r.CopyAs<RecordDataType>(),
                EnumDataType e => e.CopyAs<EnumDataType>(),
                ReferenceDataType r => r.CopyAs<ReferenceDataType>(),
                _ => throw new Exception("Unknown data type")
            }
        );

        return true;
    }
}
