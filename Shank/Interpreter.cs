using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace Shank;

public class Interpreter
{
    //public static Dictionary<string, CallableNode> Functions = new();
    public static Dictionary<string, ModuleNode> Modules = new Dictionary<string, ModuleNode>();

    private static ModuleNode? startModule;
    public static StringBuilder testOutput = new StringBuilder();

    /// <summary>
    /// Convert the given FunctionNode and its contents into their associated InterpreterDataTypes.
    /// </summary>
    /// <param name="fn">The FunctionNode being converted</param>
    /// <param name="ps">Parameters passed in (already in IDT form)</param>
    /// <exception cref="Exception"></exception>
    public static void InterpretFunction(FunctionNode fn, List<InterpreterDataType> ps)
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
            foreach (var testResult in Program.unitTestResults)
            {
                if (testResult.parentFunctionName == (((TestNode)fn).targetFunctionName))
                {
                    foundTestResult = true;
                    break;
                }
            }
            if (!foundTestResult)
            {
                Program.unitTestResults.AddLast(
                    new TestResult(((TestNode)fn).Name, ((TestNode)fn).targetFunctionName)
                );
                Program.unitTestResults.Last().lineNum = fn.LineNum;
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
                var target = variables[an.target.Name];
                switch (target)
                {
                    case IntDataType it:
                        it.Value = ResolveInt(an.expression, variables);
                        break;
                    case ArrayDataType at:
                        AssignToArray(at, an, variables);
                        break;
                    case FloatDataType ft:
                        ft.Value = ResolveFloat(an.expression, variables);
                        break;
                    case StringDataType st:
                        st.Value = ResolveString(an.expression, variables);
                        break;
                    case CharDataType ct:
                        ct.Value = ResolveChar(an.expression, variables);
                        break;
                    case BooleanDataType bt:
                        bt.Value = ResolveBool(an.expression, variables);
                        break;
                    case RecordDataType rt:
                        AssignToRecord(rt, an, variables);
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
        if (an.target.GetExtensionSafe() is VariableReferenceNode vrn)
        {
            var t = rdt.MemberTypes[vrn.Name];
            rdt.Value[vrn.Name] = t switch
            {
                VariableNode.DataType.Boolean => ResolveBool(an.expression, variables),
                VariableNode.DataType.String => ResolveString(an.expression, variables),
                VariableNode.DataType.Real => ResolveFloat(an.expression, variables),
                VariableNode.DataType.Integer => ResolveInt(an.expression, variables),
                VariableNode.DataType.Character => ResolveChar(an.expression, variables),
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
        if (an.target.Extension is { } idx)
        {
            adt.AddElement(
                adt.ArrayContentsType switch
                {
                    VariableNode.DataType.Integer => ResolveInt(an.expression, variables),
                    VariableNode.DataType.Real => ResolveFloat(an.expression, variables),
                    VariableNode.DataType.String => ResolveString(an.expression, variables),
                    VariableNode.DataType.Character => ResolveChar(an.expression, variables),
                    VariableNode.DataType.Boolean => ResolveBool(an.expression, variables),
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
        else
            throw new ArgumentException(nameof(node));
    }

    private static void ProcessFunctionCall(
        Dictionary<string, InterpreterDataType> variables,
        FunctionCallNode fc,
        CallableNode callingFunction
    )
    {
        ASTNode? calledFunction = null;
        bool callingModuleCanAccessFunction = false;

        if (startModule == null)
        {
            throw new Exception("Interpreter error, start function could not be found.");
        }

        if (startModule.getFunctions().ContainsKey(fc.Name))
        {
            calledFunction = startModule.getFunctions()[fc.Name]; // find the function
        }
        else if (startModule.getImportedFunctions().ContainsKey(fc.Name))
        {
            calledFunction = startModule.getImportedFunctions()[fc.Name];
        }
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
                        + startModule.getName()
                        + ". It may not have been exported."
                );
            }
        }

        // TODO: fix single file calling another function causing parentModuleName to be null
        if (callingFunction.parentModuleName != null)
        {
            if (Modules[callingFunction.parentModuleName].getFunctions().ContainsKey(fc.Name))
            {
                callingModuleCanAccessFunction = true;
            }
            foreach (
                string? moduleName in Modules[callingFunction.parentModuleName]
                    .getImportNames()
                    .Keys
            )
            {
                if (
                    Modules[callingFunction.parentModuleName]
                        .getImportNames()
                        .ContainsKey(moduleName)
                )
                {
                    if (
                        Modules[callingFunction.parentModuleName]
                            .getImportNames()[moduleName]
                            .Contains(fc.Name)
                    )
                    {
                        callingModuleCanAccessFunction = true;
                        break;
                    }
                    else
                    {
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
                }
            }

            if (startModule.getFunctions().ContainsKey(fc.Name))
            {
                if (startModule.getFunctions()[fc.Name] is BuiltInFunctionNode)
                    callingModuleCanAccessFunction = true;
            }

            if (!callingModuleCanAccessFunction)
                throw new Exception(
                    "Cannot access the private function "
                        + ((CallableNode)calledFunction).Name
                        + " from module "
                        + callingFunction.parentModuleName
                );
        }

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
                .unitTestResults.ElementAt(Program.unitTestResults.Count - 1)
                .Asserts.AddLast(ar);
            Program
                .unitTestResults.ElementAt(Program.unitTestResults.Count - 1)
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
        if (pnVrn.ExtensionType == ASTNode.VrnExtType.RecordMember)
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
                return new RecordDataType(parentModule.Records[vn.GetUnknownTypeSafe()].Members);
            }
            case VariableNode.DataType.Unknown:
            {
                return vn.ResolveUnknownType(parentModule) switch
                {
                    VariableNode.UnknownTypeResolver.Record
                        => new RecordDataType(
                            parentModule.Records[vn.GetUnknownTypeSafe()].Members
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
                    _
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
                ASTNode.BooleanExpressionOpType.lt => lf < rf,
                ASTNode.BooleanExpressionOpType.le => lf <= rf,
                ASTNode.BooleanExpressionOpType.gt => lf > rf,
                ASTNode.BooleanExpressionOpType.ge => lf >= rf,
                ASTNode.BooleanExpressionOpType.eq => lf == rf,
                ASTNode.BooleanExpressionOpType.ne => lf != rf,
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
                ASTNode.BooleanExpressionOpType.lt => lf < rf,
                ASTNode.BooleanExpressionOpType.le => lf <= rf,
                ASTNode.BooleanExpressionOpType.gt => lf > rf,
                ASTNode.BooleanExpressionOpType.ge => lf >= rf,
                ASTNode.BooleanExpressionOpType.eq => lf == rf,
                ASTNode.BooleanExpressionOpType.ne => lf != rf,
                _ => throw new Exception("Unknown boolean operation")
            };
        }
        catch { } // It might not have been an int operation

        throw new Exception("Unable to calculate truth of expression.");
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
                => mon.Op == ASTNode.MathOpType.plus
                    ? ResolveString(mon.Left, variables) + ResolveString(mon.Right, variables)
                    : throw new NotImplementedException(
                        "It has not been implemented to perform any math operation on"
                            + " strings other than addition."
                    ),
            VariableReferenceNode vrn
                => vrn.ExtensionType switch
                {
                    ASTNode.VrnExtType.ArrayIndex
                        => ((ArrayDataType)variables[vrn.Name]).GetElementString(
                            ResolveInt(vrn.GetExtensionSafe(), variables)
                        ),
                    ASTNode.VrnExtType.RecordMember
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

        if (node is VariableReferenceNode vrn)
        {
            return vrn.ExtensionType switch
            {
                ASTNode.VrnExtType.ArrayIndex
                    => ((ArrayDataType)variables[vrn.Name]).GetElementCharacter(
                        ResolveInt(vrn.GetExtensionSafe(), variables)
                    ),
                ASTNode.VrnExtType.RecordMember
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
                case MathOpNode.MathOpType.plus:
                    return left + right;
                case MathOpNode.MathOpType.minus:
                    return left - right;
                case MathOpNode.MathOpType.times:
                    return left * right;
                case MathOpNode.MathOpType.divide:
                    return left / right;
                case MathOpNode.MathOpType.modulo:
                    return left % right;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else if (node is FloatNode fn)
            return fn.Value;
        else if (node is VariableReferenceNode vrn)
        {
            return vrn.ExtensionType switch
            {
                ASTNode.VrnExtType.ArrayIndex
                    => ((ArrayDataType)variables[vrn.Name]).GetElementReal(
                        ResolveInt(vrn.GetExtensionSafe(), variables)
                    ),
                ASTNode.VrnExtType.RecordMember
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
                case MathOpNode.MathOpType.plus:
                    return left + right;
                case MathOpNode.MathOpType.minus:
                    return left - right;
                case MathOpNode.MathOpType.times:
                    return left * right;
                case MathOpNode.MathOpType.divide:
                    return left / right;
                case MathOpNode.MathOpType.modulo:
                    return left % right;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else if (node is IntNode fn)
            return fn.Value;
        else if (node is VariableReferenceNode vrn)
        {
            return vrn.ExtensionType switch
            {
                ASTNode.VrnExtType.ArrayIndex
                    => ((ArrayDataType)variables[vrn.Name]).GetElementInteger(
                        ResolveInt(vrn.GetExtensionSafe(), variables)
                    ),
                ASTNode.VrnExtType.RecordMember
                    => ((RecordDataType)variables[vrn.Name]).GetValueInteger(
                        vrn.GetRecordMemberReferenceSafe().Name
                    ),
                _ => ((IntDataType)variables[vrn.Name]).Value
            };
        }
        else
            throw new ArgumentException(nameof(node));
    }

    public static ModuleNode? setStartModule()
    {
        foreach (KeyValuePair<string, ModuleNode> currentModule in Modules)
        {
            if (currentModule.Value.getFunctions().ContainsKey("start"))
            {
                startModule = currentModule.Value;
                return startModule;
            }
        }

        return null;
    }

    public static ModuleNode? getStartModule()
    {
        return startModule;
    }

    public static int ResolveIntBeforeVarDecs(ASTNode node)
    {
        if (node is MathOpNode mon)
        {
            var left = ResolveIntBeforeVarDecs(mon.Left);
            var right = ResolveIntBeforeVarDecs(mon.Right);
            switch (mon.Op)
            {
                case MathOpNode.MathOpType.plus:
                    return left + right;
                case MathOpNode.MathOpType.minus:
                    return left - right;
                case MathOpNode.MathOpType.times:
                    return left * right;
                case MathOpNode.MathOpType.divide:
                    return left / right;
                case MathOpNode.MathOpType.modulo:
                    return left % right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mon.Op), "Invalid operation type");
            }
        }
        else if (node is IntNode fn)
            return fn.Value;
        else if (node is VariableReferenceNode vr)
            throw new Exception(
                "Variable references not allowed before all variables are declared"
            );
        else
            throw new ArgumentException("Invalid node type for integer", nameof(node));
    }

    //used for reseting the single static interpreter in between unit tests
    public static void reset()
    {
        Modules = new Dictionary<string, ModuleNode>();
        startModule = null;
        testOutput = new StringBuilder();
        Program.unitTestResults = new();
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
