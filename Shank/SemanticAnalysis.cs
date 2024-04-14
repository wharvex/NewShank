using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Shank
{
    public class SemanticAnalysis
    {
        private static Dictionary<string, ModuleNode> Modules;
        private static ModuleNode startModule;

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

            // When we encounter a variable x as the target of a valid assignment, we determine if
            // the assignment is valid, and if it is valid, then we set the Value of x's Key in
            // variablesSet to true. (If it's not valid, throw an exception.)

            // When we encounter a variable x NOT as the target of an assignment, then we look x up
            // in variablesSet, and if its Value is false, then we throw an exception.

            foreach (var s in statements)
            {
                bool foundFunction = false;
                if (s is AssignmentNode an)
                {
                    if (variables.TryGetValue(an.Target.Name, out var targetDeclaration))
                    {
                        var targetType = GetTargetTypeForAssignmentCheck(
                            targetDeclaration,
                            an.Target,
                            parentModule
                        );

                        CheckNode(
                            targetType,
                            an.Expression,
                            variables,
                            parentModule,
                            targetDeclaration
                        );

                        CheckRange(an, variables, targetDeclaration, parentModule);
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
                        foundFunction = true;
                    else
                    {
                        foreach (
                            KeyValuePair<
                                string,
                                LinkedList<string>
                            > import in parentModule.getImportNames()
                        )
                        {
                            if (Modules[import.Key].getExportNames().Contains(fn.Name))
                                foundFunction = true;
                        }
                    }
                    if (
                        Interpreter.getStartModule().getFunctions().ContainsKey((string)fn.Name)
                        && Interpreter.getStartModule().getFunctions()[(string)fn.Name]
                            is BuiltInFunctionNode
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

        private static void CheckRange(
            AssignmentNode an,
            Dictionary<string, VariableNode> variablesLookup,
            VariableNode targetDefinition,
            ModuleNode parentModule
        )
        {
            //if(an.target.ExtensionType is ASTNode.VrnExtType.RecordMember)
            //{
            //    //arrays should be handled down in int or float
            //    var recordMemberVRN = (VariableReferenceNode)an.target.Extension;
            //    var dict = GetRecordsAndImports(parentModule.Records, parentModule.GetImportedSafe());
            //    var recordMember = ((RecordNode)dict[an.target.Name]).GetFromMembersByNameSafe(recordMemberVRN.Name);
            //    if (recordMember.Type is VariableNode.DataType.Integer)
            //    {
            //        var from = (IntNode)recordMember.From;
            //        var to = (IntNode)recordMember.To;
            //        if (from is null || to is null)
            //            return;
            //        var rhs = (IntNode)an.expression;
            //        try
            //        {
            //            if (rhs.Value < from.Value || rhs.Value > to.Value)
            //            {
            //                throw new Exception($"The variable {an.target.Name} can only be assigned values from {from.ToString()} to {to.ToString()}.");
            //            }
            //        }
            //        catch (InvalidCastException e)
            //        {
            //            throw new Exception("Integer types can only be assigned a range of two integers.");
            //        }
            //    }
            //    if (recordMember.Type is VariableNode.DataType.Real)
            //    {
            //        var from = (IntNode)recordMember.From;
            //        var to = (IntNode)recordMember.To;
            //        if (from is null || to is null)
            //            return;
            //        var rhs = (IntNode)an.expression;
            //        try
            //        {
            //            if (rhs.Value < from.Value || rhs.Value > to.Value)
            //            {
            //                throw new Exception($"The variable {an.target.Name} can only be assigned values from {from.ToString()} to {to.ToString()}.");
            //            }
            //        }
            //        catch (InvalidCastException e)
            //        {
            //            throw new Exception("Integer types can only be assigned a range of two integers.");
            //        }
            //    }

            //}
            if (an.Expression is IntNode i)
            {
                try
                {
                    var from = (IntNode)variablesLookup[an.Target.Name].From;
                    var to = (IntNode)variablesLookup[an.Target.Name].To;
                    if (from is null || to is null)
                        return;
                    if (i.Value < from.Value || i.Value > to.Value)
                        throw new Exception(
                            $"The variable {an.Target.Name} can only be assigned values from {from.ToString()} to {to.ToString()}."
                        );
                }
                catch (InvalidCastException e)
                {
                    throw new Exception(
                        "Integer types can only be assigned a range of two integers."
                    );
                }
            }
            if (an.Expression is FloatNode f)
            {
                try
                {
                    var from = (FloatNode)variablesLookup[an.Target.Name].From;
                    var to = (FloatNode)variablesLookup[an.Target.Name].To;
                    if (from is null || to is null)
                        return;
                    if (f.Value < from.Value || f.Value > to.Value)
                        throw new Exception(
                            $"The variable {an.Target.Name} can only be assigned values from {from.ToString()} to {to.ToString()}."
                        );
                }
                catch (InvalidCastException e)
                {
                    throw new Exception("Real types can only be assigned a range of two reals.");
                }
            }
            if (an.Expression is StringNode s)
            {
                try
                {
                    var from = (IntNode)variablesLookup[an.Target.Name].From;
                    var to = (IntNode)variablesLookup[an.Target.Name].To;
                    if (from is null || to is null)
                        return;
                    if (from.Value != 0)
                        throw new Exception("Strings must have a range starting at 0.");
                    if (s.Value.Length < from.Value || s.Value.Length > to.Value)
                        throw new Exception(
                            $"The variable {an.Target.Name} can only be a length from {from.ToString()} to {to.ToString()}."
                        );
                }
                catch (InvalidCastException e)
                {
                    throw new Exception(
                        "String types can only be assigned a range of two integers."
                    );
                }
            }
        }

        private static RecordMemberNode GetNestedRecordMemberNode(
            RecordNode rn,
            AssignmentNode an,
            ModuleNode parentModule
        )
        {
            RecordMemberNode rmn = rn.GetFromMembersByNameSafe(an.Target.Name);
            while (rmn.UnknownType is not null)
            {
                //rmn = ((RecordNode)GetRecordsAndImports(parentModule.Records, parentModule.Imported)[rmn.UnknownType]).GetFromMembersByNameSafe(rmn.)
            }

            return rmn;
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
            else if (ben.Left is FloatNode rn)
            {
                if (ben.Right is not FloatNode)
                    throw new Exception("Can only compare floats to other floats.");
            }
            else if (ben.Left is VariableReferenceNode vrn)
            {
                if (
                    !variables.ContainsKey(vrn.Name)
                    && !variables.ContainsKey(((VariableReferenceNode)ben.Right).Name)
                )
                    throw new Exception("Cannot compare two enum elements.");
                var variable = variables[vrn.Name];
                switch (variable.Type)
                {
                    case VariableNode.DataType.Integer:
                        if (
                            ben.Right is not IntNode
                            || variables[((VariableReferenceNode)ben.Right).Name].Type
                                != VariableNode.DataType.Integer
                        )
                            throw new Exception(
                                "Integers can only be compared to integers or integer variables."
                            );
                        break;
                    case VariableNode.DataType.Real:
                        if (
                            ben.Right is not FloatNode
                            || variables[((VariableReferenceNode)ben.Right).Name].Type
                                != VariableNode.DataType.Real
                        )
                            throw new Exception(
                                "Floats can only be compared to floats or float variables."
                            );
                        break;
                    case VariableNode.DataType.Enum:
                        Dictionary<string, EnumNode> enums;
                        if (parentModule.getEnums().ContainsKey(variable.UnknownType))
                            enums = parentModule.getEnums();
                        else
                            enums = Modules[
                                (
                                    (EnumNode)parentModule.Imported[variable.UnknownType]
                                ).ParentModuleName
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
                                        variables[
                                            ((VariableReferenceNode)ben.Right).Name
                                        ].UnknownType != variable.UnknownType
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
                        break;
                }
            }
        }

        private static VariableNode.DataType GetTargetTypeForAssignmentCheck(
            VariableNode targetDefinition,
            VariableReferenceNode targetUsage,
            ModuleNode parentModule
        ) =>
            targetDefinition.Type switch
            {
                VariableNode.DataType.Array
                    => targetUsage.ExtensionType == ASTNode.VrnExtType.None
                        ? throw new NotImplementedException(
                            "It is not implemented yet to assign to the base of an array variable."
                        )
                        : targetDefinition.GetArrayTypeSafe(),
                VariableNode.DataType.Record
                    => GetRecordTypeRecursive(
                        parentModule,
                        (RecordNode)
                            GetRecordsAndImports(parentModule.Records, parentModule.Imported)[
                                targetDefinition.GetUnknownTypeSafe()
                            ],
                        targetUsage
                    ),
                VariableNode.DataType.Reference
                    => GetRecordTypeRecursive(
                        parentModule,
                        (RecordNode)
                            GetRecordsAndImports(parentModule.Records, parentModule.Imported)[
                                targetDefinition.GetUnknownTypeSafe()
                            ],
                        targetUsage
                    ),

                VariableNode.DataType.Unknown => throw new InvalidOperationException("hi"),
                _ => targetDefinition.Type
            };

        private static VariableNode.DataType GetSpecificRecordType(
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
                .Type;

        private static VariableNode.DataType GetRecordTypeRecursive(
            ModuleNode parentModule,
            RecordNode targetDefinition,
            VariableReferenceNode targetUsage
        )
        {
            VariableNode.DataType vndt = targetDefinition
                .GetFromMembersByNameSafe(targetUsage.GetRecordMemberReferenceSafe().Name)
                .Type;
            if (vndt != VariableNode.DataType.Record)
                return vndt;
            else
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
                );
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
            VariableNode.DataType targetType,
            ASTNode anExpression,
            IReadOnlyDictionary<string, VariableNode> variables,
            ModuleNode parentModule,
            VariableNode target
        )
        {
            switch (anExpression)
            {
                case BooleanExpressionNode booleanExpressionNode:
                    if (targetType != VariableNode.DataType.Boolean)
                        throw new Exception(
                            "Boolean expressions have to be assigned to boolean variables"
                        );
                    break;
                case BoolNode boolNode:
                    if (targetType != VariableNode.DataType.Boolean)
                        throw new Exception(
                            "true and false must be assigned to boolean variables; found "
                                + boolNode.Value
                                + " assigned to "
                                + targetType
                        );
                    break;
                case CharNode charNode:
                    if (
                        targetType != VariableNode.DataType.Character
                        && targetType != VariableNode.DataType.String
                    )
                        throw new Exception(
                            "Characters have to be assigned to character variables"
                        );
                    break;
                case FloatNode floatNode:
                    if (targetType != VariableNode.DataType.Real)
                        throw new Exception("Real numbers have to be assigned to real variables");
                    break;
                case IntNode intNode:
                    if (targetType != VariableNode.DataType.Integer)
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
                        targetType != VariableNode.DataType.Character
                        && targetType != VariableNode.DataType.String
                    )
                        throw new Exception("strings have to be assigned to string variables");
                    break;
                case VariableReferenceNode vrn:
                    if (targetType == VariableNode.DataType.Enum)
                    {
                        EnumNode? enumDefinition = null;
                        foreach (var e in parentModule.getEnums())
                        {
                            if (e.Value.EnumElements.Contains(vrn.Name))
                            {
                                enumDefinition = e.Value;
                                break;
                            }
                        }
                        foreach (var e in parentModule.Imported)
                        {
                            if (e.Value is not EnumNode)
                                continue;
                            var Enum = (EnumNode)e.Value;
                            if (Enum.EnumElements.Contains(vrn.Name))
                            {
                                enumDefinition = Enum;
                                break;
                            }
                        }
                        if (enumDefinition == null)
                            throw new Exception(
                                $"Could not find the definition for an enum containing the element {vrn.Name}."
                            );
                        if (enumDefinition.Type != target.UnknownType)
                            throw new Exception(
                                $"Cannot assign an enum of type {enumDefinition.Type} to the enum element {target.UnknownType}."
                            );
                    }
                    else
                    {
                        var vn = variables[vrn.Name];
                        if (vn.GetSpecificType(parentModule, vrn) != targetType)
                            throw new Exception(
                                vrn.Name
                                    + " is a "
                                    + variables[vrn.Name].Type
                                    + " and can't be assigned to a "
                                    + targetType
                            );
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(anExpression));
            }
        }

        //public static VariableNode.DataType GetRecordMemberType(ASTNode anExpression)
        //{

        //}

        public static void checkModules()
        {
            Modules = Interpreter.getModules();
            setStartModule();
            handleExports();
            handleImports();
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
            if (startModule != null)
                return startModule;
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

        public static void handleImports()
        {
            foreach (string currentImport in startModule.getImportNames().Keys)
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

            foreach (var currentModule in startModule.getImportNames())
            {
                if (startModule.getImportNames()[currentModule.Key].Count == 0)
                {
                    var tempList = new LinkedList<string>();
                    foreach (string s in Modules[currentModule.Key].getExportNames())
                    {
                        tempList.AddLast(s);
                    }

                    startModule.getImportNames()[currentModule.Key] = tempList;
                }
            }
        }

        public static void recursiveImportCheck(ModuleNode m)
        {
            startModule.updateImports(
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

        public static void handleExports()
        {
            foreach (KeyValuePair<string, ModuleNode> currentModule in Modules)
            {
                currentModule.Value.updateExports();
            }
        }

        public static void handleTests()
        {
            foreach (KeyValuePair<string, ModuleNode> currentModule in Modules)
            {
                foreach (KeyValuePair<string, TestNode> test in currentModule.Value.getTests())
                {
                    if (
                        currentModule
                            .Value.getFunctions()
                            .ContainsKey(test.Value.targetFunctionName)
                    )
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
                    KeyValuePair<
                        string,
                        CallableNode
                    > function in currentModule.Value.getFunctions()
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
                        if (variable.Type == VariableNode.DataType.Unknown)
                        {
                            if (
                                variable.UnknownType != null
                                && enumsAndImports.ContainsKey(variable.UnknownType)
                                && enumsAndImports[variable.UnknownType] is EnumNode
                            )
                            {
                                variable.Type = VariableNode.DataType.Enum;
                            }
                            else if (
                                variable.UnknownType != null
                                && recordsAndImports.ContainsKey(variable.UnknownType)
                                && recordsAndImports[variable.UnknownType] is RecordNode
                            )
                            {
                                variable.Type = VariableNode.DataType.Record;
                                var allRecords = GetRecordsAndImports(
                                    currentModule.Value.Records,
                                    currentModule.Value.Imported
                                );
                                var targetRecord = (RecordNode)allRecords[variable.UnknownType];
                                AssignNestedRecordTypes(targetRecord.Members, currentModule.Value);
                            }
                            else
                                throw new Exception(
                                    "Could not find a definition for the unknown type "
                                        + variable.UnknownType
                                );
                        }
                    }
                    foreach (var statement in currentFunction.Statements)
                    {
                        if (statement is not AssignmentNode)
                        {
                            continue;
                        }
                        var assignment = (AssignmentNode)statement;
                        foreach (var variable in currentFunction.LocalVariables)
                        {
                            if (variable.Type == VariableNode.DataType.Enum)
                            {
                                if (assignment.Target.Name == variable.Name)
                                {
                                    assignment.Target.ExtensionType = ASTNode.VrnExtType.Enum;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void AssignNestedRecordTypes(
            List<StatementNode> statements,
            ModuleNode parentModule
        )
        {
            foreach (var statement in statements)
            {
                var rmn = (RecordMemberNode)statement;
                if (rmn.UnknownType == null)
                    continue;

                if (
                    parentModule.Records.ContainsKey(rmn.UnknownType)
                    || (
                        parentModule.Imported.ContainsKey(rmn.UnknownType)
                        && parentModule.Imported[rmn.UnknownType] is RecordNode
                    )
                )
                {
                    rmn.Type = VariableNode.DataType.Record;
                }
                else
                {
                    rmn.Type = VariableNode.DataType.Enum;
                }
            }
        }

        public static void reset()
        {
            Modules = new Dictionary<string, ModuleNode>();
            startModule = null;
            Interpreter.testOutput = new StringBuilder();
            Program.unitTestResults = new();
        }
    }
}
