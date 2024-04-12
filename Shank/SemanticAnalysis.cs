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

        public static void CheckAssignments(
            Dictionary<string, CallableNode> functions,
            ModuleNode parentModule
        )
        {
            foreach (var f in functions.Values.Where(f => f is FunctionNode).Cast<FunctionNode>())
            {
                CheckBlock(
                    f.Statements,
                    f.LocalVariables.Concat(f.ParameterVariables),
                    parentModule
                );
            }
        }

        private static void CheckBlock(
            List<StatementNode> statements,
            IEnumerable<VariableNode> variables,
            ModuleNode parentModule
        )
        {
            var dict = variables.ToDictionary(x => x.Name ?? "", x => x);
            foreach (var s in statements)
            {
                bool foundFunction = false;
                if (s is AssignmentNode an)
                {
                    var targetDefinition = dict[an.target.Name];

                    var targetType = GetTargetTypeForAssignmentCheck(
                        targetDefinition,
                        an.target,
                        parentModule
                    );

                    CheckNode(targetType, an.expression, dict, parentModule, targetDefinition);
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
                    CheckComparison(repNode.Expression, dict, parentModule);
                }
                else if (s is WhileNode whileNode)
                {
                    CheckComparison(whileNode.Expression, dict, parentModule);
                }
                else if (s is IfNode ifNode)
                {
                    CheckComparison(ifNode.Expression, dict, parentModule);
                }
            }
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
                    => GetSpecificRecordType(parentModule, targetDefinition, targetUsage),
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

        private static VariableNode.DataType GetTargetTypeForAssignmentCheck2(
            VariableNode vn,
            VariableReferenceNode anTarget,
            ModuleNode parentModule
        )
        {
            switch (vn.Type)
            {
                case VariableNode.DataType.Array:
                    if (anTarget.ExtensionType == ASTNode.VrnExtType.None)
                    {
                        throw new NotImplementedException(
                            "It is not implemented yet to assign to the base of an array variable."
                        );
                    }
                    else
                    {
                        return vn.GetArrayTypeSafe();
                    }
                case VariableNode.DataType.Record:
                    if (parentModule.Records.ContainsKey(vn.GetUnknownTypeSafe()))
                    {
                        return parentModule
                            .Records[vn.GetUnknownTypeSafe()]
                            .GetFromMembersByNameSafe(anTarget.GetRecordMemberReferenceSafe().Name)
                            .Type;
                    }
                    else
                    {
                        if (!parentModule.Imported.ContainsKey(vn.GetUnknownTypeSafe()))
                            throw new Exception(
                                $"Could not find definition for the record {vn.GetUnknownTypeSafe()}"
                            );
                        return ((RecordNode)parentModule.Imported[vn.GetUnknownTypeSafe()])
                            .GetFromMembersByNameSafe(anTarget.GetRecordMemberReferenceSafe().Name)
                            .Type;
                    }
                default:
                    return vn.Type;
            }
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

        public static Dictionary<string, ASTNode> GetNamespaceOfRecordsAndEnumsAndImports(
            ModuleNode module
        )
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
                CheckAssignments(module.Value.getFunctions(), module.Value);
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
                        if (variable.Type == VariableNode.DataType.Unknown)
                        {
                            if (
                                variable.UnknownType != null
                                && currentModule.Value.getEnums().ContainsKey(variable.UnknownType)
                            )
                            {
                                variable.Type = VariableNode.DataType.Enum;
                            }
                            else if (
                                variable.UnknownType != null
                                && currentModule.Value.Records.ContainsKey(variable.UnknownType)
                            )
                            {
                                variable.Type = VariableNode.DataType.Record;
                            }
                            else if (
                                variable.UnknownType != null
                                && currentModule.Value.Imported.ContainsKey(variable.UnknownType)
                            )
                            {
                                if (
                                    currentModule.Value.Imported[variable.UnknownType] is EnumNode
                                    && Modules[
                                        (
                                            (EnumNode)
                                                currentModule.Value.Imported[variable.UnknownType]
                                        ).ParentModuleName
                                    ]
                                        .getEnums()
                                        .ContainsKey(variable.UnknownType)
                                )
                                    variable.Type = VariableNode.DataType.Enum;
                                else
                                    variable.Type = VariableNode.DataType.Record;
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
                                if (assignment.target.Name == variable.Name)
                                {
                                    assignment.target.ExtensionType = ASTNode.VrnExtType.Enum;
                                }
                            }
                            //                            else if (variable.Type == VariableNode.DataType.Record)
                            //                            {
                            //                                if(assignment.target.Name == variable.Name)
                            //                                {
                            //                                    if (currentModule.Value.Records.ContainsKey(variable.Name)) {
                            //                                        assignment.target.ExtensionType = currentModule.Value.Records[variable.UnknownType]
                            //                                            .GetFromMembersByName(assignment.target.Name).Type;
                            //}
                            //                            }
                        }
                    }
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
