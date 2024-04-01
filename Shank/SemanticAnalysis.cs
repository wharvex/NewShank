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
                    var target = dict[an.target.Name];

                    var targetType = GetTargetTypeForAssignmentCheck(target, an, parentModule);

                    CheckNode(targetType, an.expression, dict, parentModule);
                }
                else if (s is FunctionCallNode fn)
                {
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
            }
        }

        //private static VariableNode.DataType GetTargetTypeForAssignmentCheck(
        //    VariableNode vn,
        //    AssignmentNode an,
        //    ModuleNode parentModule
        //) =>
        //    vn.Type switch
        //    {
        //        VariableNode.DataType.Array
        //            => an.target.ExtensionType == ASTNode.VrnExtType.None
        //                ? throw new NotImplementedException(
        //                    "It is not implemented yet to assign to the base of an array variable."
        //                )
        //                : vn.GetArrayTypeSafe(),
        //        VariableNode.DataType.Record
        //            => parentModule
        //                .Records[vn.GetUnknownTypeSafe()]
        //                .GetFromMembersByNameSafe(an.target.GetRecordMemberReferenceSafe().Name)
        //                .Type,
        //        _ => vn.Type
        //    };
        private static VariableNode.DataType GetTargetTypeForAssignmentCheck(VariableNode vn, AssignmentNode an, ModuleNode parentModule)
        {
            switch (vn.Type)
            {
                case VariableNode.DataType.Array:
                    if (an.target.ExtensionType == ASTNode.VrnExtType.None)
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
                    return parentModule.Records[vn.GetUnknownTypeSafe()].GetFromMembersByNameSafe(
                        an.target.GetRecordMemberReferenceSafe().Name).Type;
                default:
                    return vn.Type;
            }
        }

        private static void CheckNode(
            VariableNode.DataType targetType,
            ASTNode anExpression,
            IReadOnlyDictionary<string, VariableNode> variables,
            ModuleNode parentModule
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
                            "True and False have to be assigned to boolean variables"
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
                    CheckNode(targetType, mathOpNode.Left, variables, parentModule);
                    CheckNode(targetType, mathOpNode.Right, variables, parentModule);
                    break;
                case StringNode stringNode:
                    if (
                        targetType != VariableNode.DataType.Character
                        && targetType != VariableNode.DataType.String
                    )
                        throw new Exception("strings have to be assigned to string variables");
                    break;
                case VariableReferenceNode vrn:
                    var vn = variables[vrn.Name];
                    if (vn.GetSpecificType(parentModule, vrn) != targetType)
                        throw new Exception(
                            vrn.Name
                                + " is a "
                                + variables[vrn.Name].Type
                                + " and can't be assigned to a "
                                + targetType
                        );
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(anExpression));
            }
        }

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
                    if (!module.Value.getFunctions().ContainsKey(exportName))
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
                            if (!m.getFunctions().ContainsKey(s))
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
                    throw new Exception("Could not find " + currentImport + " in the list of modules.");
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
                foreach (KeyValuePair<string, CallableNode> function in currentModule.Value.getFunctions())
                {
                    if(function.Value is BuiltInFunctionNode) { continue; }
                    FunctionNode currentFunction = (FunctionNode)function.Value;
                    foreach (VariableNode variable in currentFunction.LocalVariables)
                    {
                        if (variable.Type == VariableNode.DataType.Unknown)
                        {
                            if (variable.UnknownType != null && currentModule.Value.getEnums().ContainsKey(variable.UnknownType))
                            {
                                variable.Type = VariableNode.DataType.Enum;
                            }
                            else if (variable.UnknownType != null && currentModule.Value.Records.ContainsKey(variable.UnknownType))
                            {
                                variable.Type = VariableNode.DataType.Record;
                            }
                            else throw new Exception(
                                "Could not find a definition for the unknown type " + variable.UnknownType
                                );
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
