using System;
using System.Collections.Generic;
using System.Linq;

namespace Shank
{
    public class SemanticAnalysis
    {
        public static void CheckAssignments(Dictionary<string, CallableNode> functions)
        {
            foreach (var f in functions.Values.Where(f => f is FunctionNode).Cast<FunctionNode>())
            {
                CheckBlock(f.Statements, f.LocalVariables.Concat(f.ParameterVariables));
            }
        }

        private static void CheckBlock(
            List<StatementNode> statements,
            IEnumerable<VariableNode> variables
        )
        {
            var dict = variables.ToDictionary(x => x.Name ?? "", x => x);
            foreach (var s in statements)
            {
                if (s is AssignmentNode an)
                {
                    var target = dict[an.target.Name];
                    CheckNode(target.Type, an.expression, dict);
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
                            "Integer numbers have to be assigned to integer variables"
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
    }
}
