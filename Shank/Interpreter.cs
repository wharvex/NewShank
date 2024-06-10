using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using LLVMSharp;
using Shank.ASTNodes;

namespace Shank;

public class Interpreter
{
    public static Dictionary<string, ModuleNode>? Modules { get; set; }

    public static ModuleNode? StartModule { get; set; }
    public static StringBuilder testOutput = new StringBuilder();

    public static Dictionary<string, ModuleNode> GetModulesSafe() =>
        Modules ?? throw new InvalidOperationException("Expected Modules to not be null.");

    public static List<ModuleNode> GetModulesAsList() =>
        GetModulesSafe().Select(kvp => kvp.Value).ToList();

    /// <summary>
    /// Get an IDT-by-name dictionary of all the variables which a function can access.
    /// </summary>
    /// <param name="fn">The function</param>
    /// <param name="parameters">A list of IDTs of the arguments that were passed in when calling
    /// the function</param>
    /// <param name="maybeModule">The module to which the function belongs (optional)</param>
    /// <returns>A dictionary for getting IDTs by name of all the variables which a function can
    /// access</returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <remarks>Author: Tim Gudlewski</remarks>
    private static Dictionary<string, InterpreterDataType> GetVariablesDictionary(
        FunctionNode fn,
        List<InterpreterDataType> parameters,
        ModuleNode? maybeModule = null
    )
    {
        Dictionary<string, InterpreterDataType> ret = [];

        // Ensure the args count matches the params count.
        if (parameters.Count != fn.ParameterVariables.Count)
        {
            throw new InvalidOperationException(
                "For function "
                    + fn.Name
                    + ", "
                    + parameters.Count
                    + " parameters were passed in, but "
                    + fn.ParameterVariables.Count
                    + " are required."
            );
        }

        // Populate ret with entries with param name keys and arg IDT values.
        fn.ParameterVariables.Select((vn, i) => new { i, vn })
            .ToList()
            .ForEach(vni => ret.Add(vni.vn.GetNameSafe(), parameters[vni.i]));

        // If module was passed in, add its global variables to ret.
        maybeModule
            ?.GlobalVariables
            .ToList()
            .ForEach(kvp =>
            {
                if (!ret.TryAdd(kvp.Key, VariableNodeToActivationRecord(kvp.Value)))
                {
                    throw new InvalidOperationException(
                        "Uncaught namespace conflict with name " + kvp.Key
                    );
                }
            });

        // Add local variables to ret.
        fn.LocalVariables.ForEach(lv =>
        {
            if (!ret.TryAdd(lv.GetNameSafe(), VariableNodeToActivationRecord(lv)))
            {
                throw new InvalidOperationException(
                    "Uncaught namespace conflict with name " + lv.GetNameSafe()
                );
            }
        });

        // Return the dictionary of all the variables which the function can access.
        return ret;
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
        var variables = GetVariablesDictionary(fn, parametersIDTs, maybeModule);

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
        InterpretBlock(fn.Statements, variables, fn);
    }

    /// <summary>
    /// Convert the given FunctionNode and its contents into their associated InterpreterDataTypes.
    /// </summary>
    /// <param name="fn">The FunctionNode being converted</param>
    /// <param name="ps">Parameters passed in (already in IDT form)</param>
    /// <exception cref="Exception"></exception>
    public static void InterpretFunction2(FunctionNode fn, List<InterpreterDataType> ps)
    {
        var variables = new Dictionary<string, InterpreterDataType>();
        if (ps.Count != fn.ParameterVariables.Count)
            throw new Exception(
                $"Function {fn.Name}, {ps.Count} parameters passed in, {fn.ParameterVariables.Count} required"
            );
        for (var i = 0; i < fn.ParameterVariables.Count; i++)
        {
            // Create the parameters as "locals"
            variables[fn.ParameterVariables[i].Name ?? string.Empty] = ps[i];
        }

        foreach (var l in fn.LocalVariables)
        {
            // TODO: When would the Name of a local variable be null? When would the Name of any VariableNode be null?
            // set up the declared variables as locals
            variables[l.Name ?? string.Empty] = VariableNodeToActivationRecord(l);
        }
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
        InterpretBlock(fn.Statements, variables, fn);
    }

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
                // target is the left side of the assignment statement
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
                        et.Value = ResolveEnum((EnumDataType)target, an.Expression, variables);
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
                for (var i = start; i < end; i++)
                {
                    index.Value = i;
                    InterpretBlock(fn.Children, variables, callingFunction);
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
        if (an.Target.GetExtensionSafe() is VariableUsageNode vrn)
        {
            var t = rdt.MemberTypes[vrn.Name];
            rdt.Value[vrn.Name] = t switch
            {
                VariableNode.DataType.Boolean => ResolveBool(an.Expression, variables),
                VariableNode.DataType.String => ResolveString(an.Expression, variables),
                VariableNode.DataType.Real => ResolveFloat(an.Expression, variables),
                VariableNode.DataType.Integer => ResolveInt(an.Expression, variables),
                VariableNode.DataType.Character => ResolveChar(an.Expression, variables),
                VariableNode.DataType.Record => ResolveRecord(an.Expression, variables),
                VariableNode.DataType.Reference => ResolveReference(an.Expression, variables),
                VariableNode.DataType.Enum => ResolveEnum(an.Expression, variables),

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
                    VariableNode.DataType.Integer => ResolveInt(an.Expression, variables),
                    VariableNode.DataType.Real => ResolveFloat(an.Expression, variables),
                    VariableNode.DataType.String => ResolveString(an.Expression, variables),
                    VariableNode.DataType.Character => ResolveChar(an.Expression, variables),
                    VariableNode.DataType.Boolean => ResolveBool(an.Expression, variables),
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

    private static bool ResolveBool(ASTNode node, Dictionary<string, InterpreterDataType> variables)
    {
        if (node is BooleanExpressionNode ben)
            return EvaluateBooleanExpressionNode(ben, variables);
        else if (node is BoolNode bn)
            return bn.Value;
        else if (node is VariableUsageNode vrn)
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
        if (node is VariableUsageNode variable)
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
        if (node is VariableUsageNode vrn)
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

        if (
            fc.Parameters.Count != ((CallableNode)calledFunction).ParameterVariables.Count
            && calledFunction is BuiltInFunctionNode { IsVariadic: false }
        ) // make sure that the counts match
            throw new Exception(
                $"Call of {((CallableNode)calledFunction).Name}, parameter count doesn't match."
            );
        // make the list of parameters
        var passed = new List<InterpreterDataType>();
        foreach (var fcp in fc.Parameters)
        {
            if (fcp.Variable != null)
            {
                var name = fcp.Variable.Name;
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
                        AddToParamsArray(arrayVal, fcp, passed, variables);
                        break;
                    case RecordDataType recordVal:
                        AddToParamsRecord(recordVal, fcp, passed);
                        break;
                    case EnumDataType enumVal:
                        passed.Add(new EnumDataType(enumVal.Type, enumVal.Value));
                        break;
                    case ReferenceDataType referenceVal:
                        if (fcp.Variable.Extension != null)
                        {
                            var vrn = ((VariableUsageNode)fcp.Variable.Extension);
                            if (referenceVal.Record is null)
                                throw new Exception($"{fcp.Variable.Name} was never allocated.");
                            if (referenceVal.Record.Value[vrn.Name] is int)
                                passed.Add(
                                    new IntDataType((int)referenceVal.Record.Value[vrn.Name])
                                );
                            else if (referenceVal.Record.Value[vrn.Name] is float)
                                passed.Add(
                                    new FloatDataType((float)referenceVal.Record.Value[vrn.Name])
                                );
                            else if (referenceVal.Record.Value[vrn.Name] is char)
                                passed.Add(
                                    new CharDataType((char)referenceVal.Record.Value[vrn.Name])
                                );
                            else if (referenceVal.Record.Value[vrn.Name] is string)
                                passed.Add(
                                    new StringDataType((string)referenceVal.Record.Value[vrn.Name])
                                );
                            else if (referenceVal.Record.Value[vrn.Name] is float)
                                passed.Add(
                                    new BooleanDataType((bool)referenceVal.Record.Value[vrn.Name])
                                );
                        }
                        else
                            passed.Add(new ReferenceDataType(referenceVal));
                        break;
                }
            }
            else
            {
                var value = fcp.Constant;
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
                    case EnumNode enumVal:
                        // passed.Add(new EnumDataType(enumVal.Type, enumVal.Value));
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
        ((CallableNode)calledFunction).Execute?.Invoke(passed);
        // update the variable parameters and return
        for (var i = 0; i < passed.Count; i++)
        {
            if (
                (
                    (calledFunction is BuiltInFunctionNode { IsVariadic: true })
                    || !((CallableNode)calledFunction).ParameterVariables[i].IsConstant
                )
                && fc.Parameters[i].Variable != null
                && fc.Parameters[i].IsVariable
            )
            {
                // if this parameter is a "var", then copy the new value back to the parameter holder
                variables[fc.Parameters[i]?.Variable?.Name ?? ""] = passed[i];
            }
        }
    }

    // TODO
    private static void AddToParamsArray(
        ArrayDataType adt,
        ParameterNode pn,
        List<InterpreterDataType> paramsList,
        Dictionary<string, InterpreterDataType> variables
    )
    {
        if ((pn.Variable ?? throw new InvalidOperationException()).Extension is { } i)
        {
            // Passing in one element of the array.
            var index = ResolveInt(i, variables);
            switch (adt.ArrayContentsType)
            {
                case VariableNode.DataType.Integer:
                    paramsList.Add(new IntDataType(adt.GetElementInteger(index)));
                    break;
                case VariableNode.DataType.Real:
                    paramsList.Add(new FloatDataType(adt.GetElementReal(index)));
                    break;
                case VariableNode.DataType.String:
                    paramsList.Add(new StringDataType(adt.GetElementString(index)));
                    break;
                case VariableNode.DataType.Character:
                    paramsList.Add(new CharDataType(adt.GetElementCharacter(index)));
                    break;
                case VariableNode.DataType.Boolean:
                    paramsList.Add(new BooleanDataType(adt.GetElementBoolean(index)));
                    break;
                default:
                    throw new Exception("Invalid ArrayContentsType");
            }
        }
        else
        {
            // Passing in the whole array as a new ADT.
            paramsList.Add(new ArrayDataType(adt.Value, adt.ArrayContentsType));
        }
    }

    private static void AddToParamsRecord(
        RecordDataType rdt,
        ParameterNode pn,
        List<InterpreterDataType> paramsList
    )
    {
        var pnVrn = pn.GetVariableSafe();
        if (pnVrn.ExtensionType == VariableUsageNode.VrnExtType.RecordMember)
        {
            var rmVrn = pnVrn.GetRecordMemberReferenceSafe();
            paramsList.Add(
                rdt.MemberTypes[rmVrn.Name] switch
                {
                    VariableNode.DataType.Character
                        => new CharDataType(rdt.GetValueCharacter(rmVrn.Name)),
                    VariableNode.DataType.Boolean
                        => new BooleanDataType(rdt.GetValueBoolean(rmVrn.Name)),
                    VariableNode.DataType.String
                        => new StringDataType(rdt.GetValueString(rmVrn.Name)),
                    VariableNode.DataType.Integer
                        => new IntDataType(rdt.GetValueInteger(rmVrn.Name)),
                    VariableNode.DataType.Real => new FloatDataType(rdt.GetValueReal(rmVrn.Name)),
                    VariableNode.DataType.Reference
                        => new ReferenceDataType(rdt.GetValueReference(rmVrn.Name)),
                    VariableNode.DataType.Record
                        => GetNestedParam(
                            rdt,
                            pn.Variable
                                ?? throw new Exception("Could not find extension for nested record")
                        ),
                    VariableNode.DataType.Enum => rdt.GetValueEnum(rmVrn.Name),
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

    public static InterpreterDataType GetNestedParam(RecordDataType rdt, VariableUsageNode vn)
    {
        var temp = rdt.Value[((VariableUsageNode)vn.GetExtensionSafe()).Name];
        if (temp is RecordDataType && ((VariableUsageNode)vn.GetExtensionSafe()).Extension is null)
        {
            return GetNestedParam((RecordDataType)temp, (VariableUsageNode)vn.GetExtensionSafe());
        }
        if (
            temp is ReferenceDataType
            && ((VariableUsageNode)vn.GetExtensionSafe()).Extension is null
        )
        {
            return GetNestedParam(
                ((ReferenceDataType)temp).Record
                    ?? throw new Exception($"Reference was never allocated, {vn.ToString()}"),
                (VariableUsageNode)vn.GetExtensionSafe()
            );
        }
        return temp switch
        {
            int => new IntDataType((int)temp),
            float => new FloatDataType((float)temp),
            string => new StringDataType((string)temp),
            char => new CharDataType((char)temp),
            bool => new BooleanDataType((bool)temp),
            EnumDataType => new EnumDataType((EnumDataType)temp),
            RecordDataType => new RecordDataType((RecordDataType)temp),
            ReferenceDataType => new ReferenceDataType((ReferenceDataType)temp),
            _ => throw new Exception("Could not find nested type.")
        };
        throw new Exception("Could not get nested param");
    }

    private static InterpreterDataType VariableNodeToActivationRecord(VariableNode vn)
    {
        var parentModule = Modules[vn.GetModuleNameSafe()];
        switch (vn.Type)
        {
            case VariableNode.DataType.Real:
                return new FloatDataType(((vn.InitialValue as FloatNode)?.Value) ?? 0.0F);
            case VariableNode.DataType.Integer:
                return new IntDataType(((vn.InitialValue as IntNode)?.Value) ?? 0);
            case VariableNode.DataType.String:
                return new StringDataType(((vn.InitialValue as StringNode)?.Value) ?? "");
            case VariableNode.DataType.Character:
                return new CharDataType(((vn.InitialValue as CharNode)?.Value) ?? ' ');
            case VariableNode.DataType.Boolean:
                return new BooleanDataType(((vn.InitialValue as BoolNode)?.Value) ?? true);
            case VariableNode.DataType.Array:
            {
                return new ArrayDataType(vn.GetArrayTypeSafe());
            }
            case VariableNode.DataType.Record:
            {
                if (parentModule.Records.ContainsKey(vn.GetUnknownTypeSafe()))
                    return new RecordDataType(
                        parentModule.Records[vn.GetUnknownTypeSafe()].Members2
                    );
                if (
                    parentModule.Imported.ContainsKey(vn.GetUnknownTypeSafe())
                    && !parentModule
                        .ImportTargetNames[
                            (
                                (RecordNode)parentModule.GetImportedSafe()[vn.GetUnknownTypeSafe()]
                            ).GetParentModuleSafe()
                        ]
                        .Contains(vn.GetUnknownTypeSafe())
                )
                    throw new Exception(
                        $"Could not find definition for the record {vn.GetUnknownTypeSafe()}."
                    );
                return new RecordDataType(
                    ((RecordNode)parentModule.Imported[vn.GetUnknownTypeSafe()]).Members
                );
            }
            case VariableNode.DataType.Enum:
                if (parentModule.getEnums().ContainsKey(vn.GetUnknownTypeSafe()))
                    return new EnumDataType(parentModule.getEnums()[vn.GetUnknownTypeSafe()]);
                else if (
                    vn.IsConstant && !parentModule.Imported.ContainsKey(vn.GetUnknownTypeSafe())
                )
                {
                    EnumNode? enmNode = null;
                    string? s = null;
                    foreach (var enm in parentModule.getEnums())
                    {
                        if (enm.Value.EnumElements.Contains(vn.GetUnknownTypeSafe()))
                        {
                            s = vn.GetUnknownTypeSafe();
                            enmNode = enm.Value;
                            break;
                        }
                    }
                    return new EnumDataType(
                        enmNode
                            ?? throw new Exception(
                                "Could not find a constant enums base declaration."
                            ),
                        s ?? throw new Exception("Could not find constant enum assignment.")
                    );
                }
                else if (!((EnumNode)parentModule.Imported[vn.GetUnknownTypeSafe()]).IsPublic)
                    throw new Exception(
                        $"Cannot create an enum of type {vn.GetUnknownTypeSafe()} as it was never exported"
                    );
                return new EnumDataType((EnumNode)parentModule.Imported[vn.GetUnknownTypeSafe()]);

            case VariableNode.DataType.Reference:
                if (parentModule.Records.ContainsKey(vn.GetUnknownTypeSafe()))
                    return new ReferenceDataType(parentModule.Records[vn.GetUnknownTypeSafe()]);
                else
                {
                    if (!parentModule.Imported.ContainsKey(vn.GetUnknownTypeSafe()))
                        throw new Exception(
                            $"Could not find definition for the record {vn.GetUnknownTypeSafe()}."
                        );
                    return new ReferenceDataType(
                        ((RecordNode)parentModule.Imported[vn.GetUnknownTypeSafe()])
                    );
                }

            case VariableNode.DataType.Unknown:
            {
                return vn.ResolveUnknownType(parentModule) switch
                {
                    VariableNode.UnknownTypeResolver.Record
                        => new RecordDataType(
                            parentModule.Records[vn.GetUnknownTypeSafe()].Members2
                        ),
                    VariableNode.UnknownTypeResolver.None
                        => throw new InvalidOperationException(
                            "The VariableNode's type was set to Unknown, but neither a"
                                + " RecordNode nor an EnumNode could be found in its parent module "
                                + "for it."
                        ),
                    VariableNode.UnknownTypeResolver.Multiple
                        => throw new InvalidOperationException(
                            "An EnumNode and a RecordNode were found with this "
                                + "VariableNode's type name, so we don't know which one to use."
                        ),
                    VariableNode.UnknownTypeResolver.Enum
                        => throw new NotImplementedException(
                            "Bret, you can put an enum arm in this switch expression if you"
                                + " want the interpreter to be able to process Unknowns as enums."
                        )
                };
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
            if (rf is VariableUsageNode right)
            {
                if (lf is VariableUsageNode left)
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
        var enumElements = left.Type.EnumElements.ToArray();
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
        var enumElements = left.Type.EnumElements.ToArray();
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
            VariableUsageNode vrn
                => vrn.ExtensionType switch
                {
                    VariableUsageNode.VrnExtType.ArrayIndex
                        => ((ArrayDataType)variables[vrn.Name]).GetElementString(
                            ResolveInt(vrn.GetExtensionSafe(), variables)
                        ),
                    VariableUsageNode.VrnExtType.RecordMember
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

        if (node is VariableUsageNode vrn)
        {
            return vrn.ExtensionType switch
            {
                VariableUsageNode.VrnExtType.ArrayIndex
                    => ((ArrayDataType)variables[vrn.Name]).GetElementCharacter(
                        ResolveInt(vrn.GetExtensionSafe(), variables)
                    ),
                VariableUsageNode.VrnExtType.RecordMember
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
        else if (node is VariableUsageNode vrn)
        {
            return vrn.ExtensionType switch
            {
                VariableUsageNode.VrnExtType.ArrayIndex
                    => ((ArrayDataType)variables[vrn.Name]).GetElementReal(
                        ResolveInt(vrn.GetExtensionSafe(), variables)
                    ),
                VariableUsageNode.VrnExtType.RecordMember
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
        else if (node is VariableUsageNode vrn)
        {
            return vrn.ExtensionType switch
            {
                VariableUsageNode.VrnExtType.ArrayIndex
                    => ((ArrayDataType)variables[vrn.Name]).GetElementInteger(
                        ResolveInt(vrn.GetExtensionSafe(), variables)
                    ),
                VariableUsageNode.VrnExtType.RecordMember
                    => ((RecordDataType)variables[vrn.Name]).GetValueInteger(
                        vrn.GetRecordMemberReferenceSafe().Name
                    ),
                _ => ((IntDataType)variables[vrn.Name]).Value
            };
        }
        else
            throw new ArgumentException(nameof(node));
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
            case VariableUsageNode v:
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
        else if (node is VariableUsageNode vr)
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
}
