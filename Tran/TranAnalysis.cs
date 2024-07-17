using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shank.ASTNodes;

namespace Tran
{
    public class TranAnalysis
    {
        public static Dictionary<string, ModuleNode> Walk(Dictionary<string, ModuleNode> modules)
        {
            foreach (var module in modules.Values)
            {
                foreach (FunctionNode function in module.Functions.Values)
                {
                    if (function.Name[0] == '_')
                        continue;
                    for (int i = 0; i < function.Statements.Count; i++)
                    {
                        //TODO: replace "variable = ..." with actually replacing the variable reference with accessor call
                        //Unsure how to do that since function call is a statement not an expression
                        var statement = function.Statements[i];

                        if (statement.GetType() == typeof(AssignmentNode))
                        {
                            var assignment = (AssignmentNode)statement;
                            if (VariableIsMember(module, assignment.Target))
                            {
                                var call = new FunctionCallNode(
                                    "_" + assignment.Target.Name + "_mutator"
                                );
                                call.Arguments.Add(assignment.Expression);
                                function.Statements[i] = call;
                            }
                            var variable = WalkExpression(assignment.Expression, module);
                        }
                        else if (statement.GetType() == typeof(FunctionCallNode))
                        {
                            var call = (FunctionCallNode)statement;
                            foreach (var argument in call.Arguments)
                            {
                                var variable = WalkExpression(argument, module);
                                if (variable != null)
                                    break;
                            }
                        }
                        else if (statement.GetType() == typeof(WhileNode))
                        {
                            var loop = (WhileNode)statement;
                            var variable = WalkExpression(loop.Expression, module);
                        }
                        else if (statement.GetType() == typeof(IfNode))
                        {
                            var ifNode = (IfNode)statement;
                            WalkIf(ifNode, module);
                        }
                    }
                }
            }
            return modules;
        }

        private static void WalkIf(IfNode ifNode, ModuleNode module)
        {
            if (ifNode.Expression != null)
            {
                var variable = WalkExpression(ifNode.Expression, module);
                if (ifNode.NextIfNode != null)
                {
                    WalkIf(ifNode.NextIfNode, module);
                }
            }
        }

        private static VariableUsagePlainNode? WalkExpression(
            ExpressionNode expression,
            ModuleNode module
        )
        {
            if (expression.GetType() == typeof(VariableUsagePlainNode))
            {
                var variable = (VariableUsagePlainNode)expression;
                if (VariableIsMember(module, variable))
                {
                    return variable;
                }
            }
            else if (expression.GetType() == typeof(MathOpNode))
            {
                var mathOp = (MathOpNode)expression;
                var retVal =
                    WalkExpression(mathOp.Left, module) ?? WalkExpression(mathOp.Right, module);
                return retVal;
            }
            else if (expression.GetType() == typeof(BooleanExpressionNode))
            {
                var boolOp = (BooleanExpressionNode)expression;
                var retVal =
                    WalkExpression(boolOp.Left, module) ?? WalkExpression(boolOp.Right, module);
                return retVal;
            }

            return null;
        }

        private static bool VariableIsMember(ModuleNode module, VariableUsagePlainNode variable)
        {
            foreach (var member in module.Records.First().Value.Members)
            {
                if (member.Name == variable.Name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
