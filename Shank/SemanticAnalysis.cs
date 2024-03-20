using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Shank
{
    public class SemanticAnalysis
    {
        private static Dictionary<string, ModuleNode> Modules;

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

                    // If ArrayType is not null, then we should use it for this analysis because it
                    // means target is an array.
                    // If RecordType is not null, then we should use the type of the
                    // RecordMemberReference on the target.
                    VariableNode.DataType targetType = target.Type switch
                    {
                        VariableNode.DataType.Array
                            => target.ArrayType
                                ?? throw new InvalidOperationException(
                                    "Something went wrong internally. When the Type of a"
                                        + " VariableReferenceNode is Array, its ArrayType should"
                                        + " not be null."
                                ),
                        VariableNode.DataType.Record
                            => parentModule
                                .Records[
                                    target.RecordType
                                        ?? throw new InvalidOperationException(
                                            "Something went wrong internally. When the Type"
                                                + " of a VariableReferenceNode is Record, its"
                                                + " RecordType should not be null."
                                        )
                                ]
                                .GetFromMembersByName(
                                    an.target.RecordMemberReference?.Name
                                        ?? throw new Exception(
                                            "Cannot assign to a Record target without"
                                                + " specifying a Record member."
                                        )
                                )
                                ?.Type ?? throw new Exception("Member not found on Record"),
                        _ => target.Type
                    };
                    CheckNode(targetType, an.expression, dict);
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

        private static void CheckNode(
            VariableNode.DataType targetType,
            ASTNode anExpression,
            IReadOnlyDictionary<string, VariableNode> variables
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
                    CheckNode(targetType, mathOpNode.Left, variables);
                    CheckNode(targetType, mathOpNode.Right, variables);
                    break;
                case StringNode stringNode:
                    if (
                        targetType != VariableNode.DataType.Character
                        && targetType != VariableNode.DataType.String
                    )
                        throw new Exception("strings have to be assigned to string variables");
                    break;
                case VariableReferenceNode variableReferenceNode:
                    if (variables[variableReferenceNode.Name].Type != targetType)
                        throw new Exception(
                            $"{variableReferenceNode.Name} is a {variables[variableReferenceNode.Name].Type} and can't be assigned to a {targetType}"
                        );
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(anExpression));
            }
        }

        public static void checkModules(Dictionary<string, ModuleNode> modules)
        {
            Modules = modules;
            foreach (KeyValuePair<string, ModuleNode> module in modules)
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
                    if (!modules.ContainsKey(import.Key))
                        throw new Exception($"Module {import.Key} does not exist");
                    //if the i
                    if (import.Value != null || import.Value.Count > 0)
                    {
                        ModuleNode m = modules[import.Key];

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
    }
}
