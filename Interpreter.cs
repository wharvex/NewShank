using System.Net.Http.Headers;

namespace Shank
{
    public class Interpreter
    {
        //public static Dictionary<string, CallableNode> Functions = new();
        public static Dictionary<string, ModuleNode> Modules = new Dictionary<string, ModuleNode>();
        private static ModuleNode? startModule;

        /// <summary>
        /// Convert the given FunctionNode and its contents into their associated InterpreterDataTypes.
        /// </summary>
        /// <param name="fn">The FunctionNode being converted</param>
        /// <param name="ps">Parameters passed in</param>
        /// <exception cref="Exception"></exception>
        public static void InterpretFunction(FunctionNode fn, List<InterpreterDataType> ps)
        {
            var variables = new Dictionary<string, InterpreterDataType>();
            if (ps.Count != fn.ParameterVariables.Count)
                throw new Exception(
                    $"Function {fn.Name}, {ps.Count} parameters passed in, {fn.ParameterVariables.Count} required"
                );
            for (var i = 0; i < fn.ParameterVariables.Count; i++)
            { // Create the parameters as "locals"
                variables[fn.ParameterVariables[i].Name ?? string.Empty] = ps[i];
            }

            foreach (var l in fn.LocalVariables)
            { // set up the declared variables as locals
                variables[l.Name ?? string.Empty] = VariableNodeToActivationRecord(l);
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
                            switch (at.ArrayContentsType)
                            {
                                case VariableNode.DataType.Integer:
                                    at.AddElement(
                                        ResolveInt(an.expression, variables),
                                        ResolveInt(an.target.Index, variables)
                                    );
                                    break;
                                case VariableNode.DataType.Real:
                                    at.AddElement(
                                        ResolveFloat(an.expression, variables),
                                        ResolveInt(an.target.Index, variables)
                                    );
                                    break;
                                case VariableNode.DataType.String:
                                    at.AddElement(
                                        ResolveString(an.expression, variables),
                                        ResolveInt(an.target.Index, variables)
                                    );
                                    break;
                                case VariableNode.DataType.Character:
                                    at.AddElement(
                                        ResolveChar(an.expression, variables),
                                        ResolveInt(an.target.Index, variables)
                                    );
                                    break;
                                case VariableNode.DataType.Boolean:
                                    at.AddElement(
                                        ResolveBool(an.expression, variables),
                                        ResolveInt(an.target.Index, variables)
                                    );
                                    break;
                                default:
                                    throw new Exception("Invalid ArrayContentsType");
                            }
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

        private static bool ResolveBool(
            ASTNode node,
            Dictionary<string, InterpreterDataType> variables
        )
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
            ASTNode? calledFunction;
            if (startModule == null)
            {
                throw new Exception("Interpreter error, start function could not be found.");
            }
            if (startModule.getFunctions().ContainsKey(fc.Name))
            {
                calledFunction = startModule.getFunctions()[fc.Name]; // find the function
            }
            else if (startModule.getImports().ContainsKey(fc.Name))
            {
                calledFunction = startModule.getImports()[fc.Name];
            }
            else
            {
                throw new Exception(
                    "Could not find the function "
                        + fc.Name
                        + " in the module "
                        + startModule.getName()
                        + ". It may not have been exported."
                );
            }
            // TODO: fix single file calling another function causing parentModuleName to be null
            if (callingFunction.parentModuleName != null)
            {
                bool callingModuleCanAccessFunction = false;
                foreach (
                    string? moduleName in Modules[callingFunction.parentModuleName]
                        .getImportDict()
                        .Keys
                )
                {
                    if (
                        Modules[callingFunction.parentModuleName]
                            .getFunctions()
                            .ContainsKey(fc.Name)
                    )
                    {
                        callingModuleCanAccessFunction = true;
                    }
                    else if (
                        Modules[callingFunction.parentModuleName]
                            .getImportDict()
                            .ContainsKey(moduleName)
                    )
                    {
                        if (
                            Modules[callingFunction.parentModuleName]
                                .getImportDict()[moduleName]
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
                                    .getImportDict()[moduleName]
                                    .Contains(fc.Name)
                            )
                            {
                                callingModuleCanAccessFunction = true;
                                break;
                            }
                        }
                    }
                    if (startModule.getFunctions().ContainsKey(fc.Name))
                    {
                        if (startModule.getFunctions()[fc.Name] is BuiltInFunctionNode)
                        {
                            callingModuleCanAccessFunction = true;
                        }
                    }
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
                            // Check if fcp.Variable has an index. If it does, then we are passing
                            // an element of the array. If it doesn't, then we are passing the
                            // entire array.
                            if (fcp.Variable.Index != null)
                            {
                                var index = ResolveInt(fcp.Variable.Index, variables);
                                switch (arrayVal.ArrayContentsType)
                                {
                                    case VariableNode.DataType.Integer:
                                        passed.Add(
                                            new IntDataType((int)arrayVal.GetElement(index))
                                        );
                                        break;
                                    case VariableNode.DataType.Real:
                                        passed.Add(
                                            new FloatDataType((float)arrayVal.GetElement(index))
                                        );
                                        break;
                                    case VariableNode.DataType.String:
                                        passed.Add(
                                            new StringDataType((string)arrayVal.GetElement(index))
                                        );
                                        break;
                                    case VariableNode.DataType.Character:
                                        passed.Add(
                                            new CharDataType((char)arrayVal.GetElement(index))
                                        );
                                        break;
                                    case VariableNode.DataType.Boolean:
                                        passed.Add(
                                            new BooleanDataType((bool)arrayVal.GetElement(index))
                                        );
                                        break;
                                    default:
                                        throw new Exception("Invalid ArrayContentsType");
                                }
                            }
                            else
                                passed.Add(
                                    new ArrayDataType(arrayVal.Value, arrayVal.ArrayContentsType)
                                );
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

        private static InterpreterDataType VariableNodeToActivationRecord(VariableNode vn)
        {
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
                    if (vn.To is null)
                        throw new ArgumentException(
                            "Something went wrong internally. Every array variable declaration"
                                + " should have a range expression, and the Parser should have"
                                + " checked this already."
                        );
                    return new ArrayDataType(ResolveIntBeforeVarDecs(vn.To), vn.ArrayType);
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
                    BooleanExpressionNode.OpType.lt => lf < rf,
                    BooleanExpressionNode.OpType.le => lf <= rf,
                    BooleanExpressionNode.OpType.gt => lf > rf,
                    BooleanExpressionNode.OpType.ge => lf >= rf,
                    BooleanExpressionNode.OpType.eq => lf == rf,
                    BooleanExpressionNode.OpType.ne => lf != rf,
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
                    BooleanExpressionNode.OpType.lt => lf < rf,
                    BooleanExpressionNode.OpType.le => lf <= rf,
                    BooleanExpressionNode.OpType.gt => lf > rf,
                    BooleanExpressionNode.OpType.ge => lf >= rf,
                    BooleanExpressionNode.OpType.eq => lf == rf,
                    BooleanExpressionNode.OpType.ne => lf != rf,
                    _ => throw new Exception("Unknown boolean operation")
                };
            }
            catch { } // It might not have been an int operation

            throw new Exception("Unable to calculate truth of expression.");
        }

        public static string ResolveString(
            ASTNode node,
            Dictionary<string, InterpreterDataType> variables
        )
        {
            if (node is MathOpNode mon)
            {
                var left = ResolveString(mon.Left, variables);
                var right = ResolveString(mon.Right, variables);
                switch (mon.Op)
                {
                    case MathOpNode.OpType.plus:
                        return left + right;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else if (node is StringNode fn)
                return fn.Value;
            else if (node is CharNode cn)
                return "" + cn.Value;
            else if (node is VariableReferenceNode vr)
            {
                if (vr.Index != null)
                {
                    var index = ResolveInt(vr.Index, variables);
                    return ((variables[vr.Name] as ArrayDataType)?.GetElement(index))?.ToString()
                        ?? string.Empty;
                }
                return ((variables[vr.Name] as StringDataType)?.Value) ?? string.Empty;
            }
            else
                throw new ArgumentException(nameof(node));
        }

        public static char ResolveChar(
            ASTNode node,
            Dictionary<string, InterpreterDataType> variables
        )
        {
            if (node is CharNode cn)
                return cn.Value;
            else if (node is VariableReferenceNode vr)
            {
                if (vr.Index != null)
                {
                    var index = ResolveInt(vr.Index, variables);
                    return ((variables[vr.Name] as ArrayDataType)?.GetElement(index))
                            ?.ToString()
                            ?.ToCharArray()[0] ?? '0';
                }
                return ((variables[vr.Name] as CharDataType)?.Value) ?? '0';
            }
            else
                throw new ArgumentException(nameof(node));
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
                    case MathOpNode.OpType.plus:
                        return left + right;
                    case MathOpNode.OpType.minus:
                        return left - right;
                    case MathOpNode.OpType.times:
                        return left * right;
                    case MathOpNode.OpType.divide:
                        return left / right;
                    case MathOpNode.OpType.modulo:
                        return left % right;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else if (node is FloatNode fn)
                return fn.Value;
            else if (node is VariableReferenceNode vr)
            {
                if (vr.Index != null)
                {
                    var index = ResolveInt(vr.Index, variables);
                    return (float)(variables[vr.Name] as ArrayDataType)?.GetElement(index);
                }
                return ((variables[vr.Name] as FloatDataType)?.Value) ?? 0.0F;
            }
            else
                throw new ArgumentException(nameof(node));
        }

        public static int ResolveInt(
            ASTNode node,
            Dictionary<string, InterpreterDataType> variables
        )
        {
            if (node is MathOpNode mon)
            {
                var left = ResolveInt(mon.Left, variables);
                var right = ResolveInt(mon.Right, variables);
                switch (mon.Op)
                {
                    case MathOpNode.OpType.plus:
                        return left + right;
                    case MathOpNode.OpType.minus:
                        return left - right;
                    case MathOpNode.OpType.times:
                        return left * right;
                    case MathOpNode.OpType.divide:
                        return left / right;
                    case MathOpNode.OpType.modulo:
                        return left % right;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else if (node is IntNode fn)
                return fn.Value;
            else if (node is VariableReferenceNode vr)
            {
                if (vr.Index != null)
                {
                    var index = ResolveInt(vr.Index, variables);
                    return (int)(variables[vr.Name] as ArrayDataType)?.GetElement(index);
                }
                return ((variables[vr.Name] as IntDataType)?.Value) ?? 0;
            }
            else
                throw new ArgumentException(nameof(node));
        }

        public static void handleImports()
        {
            foreach (string currentImport in startModule.getImportDict().Keys)
            {
                if (Modules.ContainsKey(currentImport))
                {
                    recursiveImportCheck(Modules[currentImport]);
                }
                else
                {
                    throw new Exception(
                        "Could not find " + currentImport + " in the list of modules."
                    );
                }
            }
            foreach (var currentModule in startModule.getImportDict())
            {
                if (startModule.getImportDict()[currentModule.Key].Count == 0)
                {
                    var tempList = new LinkedList<string>();
                    foreach (string s in Modules[currentModule.Key].getExportList())
                    {
                        tempList.AddLast(s);
                    }
                    startModule.getImportDict()[currentModule.Key] = tempList;
                }
            }
        }

        public static void recursiveImportCheck(ModuleNode m)
        {
            startModule.updateImports(
                Modules[m.getName()].getFunctions(),
                Modules[m.getName()].getExports()
            );

            if (Modules[m.getName()].getImportDict().Count > 0)
            {
                foreach (string? moduleToBeImported in Modules[m.getName()].getImportDict().Keys)
                {
                    if (Modules.ContainsKey(moduleToBeImported))
                    {
                        m.updateImports(
                            Modules[moduleToBeImported].getFunctions(),
                            Modules[moduleToBeImported].getExports()
                        );
                        foreach (var currentModule in m.getImportDict())
                        {
                            if (m.getImportDict()[currentModule.Key].Count == 0)
                            {
                                var tempList = new LinkedList<string>();
                                foreach (string s in Modules[currentModule.Key].getExportList())
                                {
                                    tempList.AddLast(s);
                                }
                                m.getImportDict()[currentModule.Key] = tempList;
                            }
                        }
                        recursiveImportCheck(Modules[moduleToBeImported]);
                    }
                }
            }
        }

        public static void handleExports()
        {
            foreach (KeyValuePair<string, ModuleNode> currentModule in Modules)
            {
                currentModule.Value.updateExports();
            }
        }

        public static void setStartModule()
        {
            foreach (KeyValuePair<string, ModuleNode> currentModule in Modules)
            {
                if (currentModule.Value.getFunctions().ContainsKey("start"))
                {
                    startModule = currentModule.Value;
                    return;
                }
            }
        }

        public static int ResolveIntBeforeVarDecs(ASTNode node)
        {
            if (node is MathOpNode mon)
            {
                var left = ResolveIntBeforeVarDecs(mon.Left);
                var right = ResolveIntBeforeVarDecs(mon.Right);
                switch (mon.Op)
                {
                    case MathOpNode.OpType.plus:
                        return left + right;
                    case MathOpNode.OpType.minus:
                        return left - right;
                    case MathOpNode.OpType.times:
                        return left * right;
                    case MathOpNode.OpType.divide:
                        return left / right;
                    case MathOpNode.OpType.modulo:
                        return left % right;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(mon.Op),
                            "Invalid operation type"
                        );
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
    }
}
