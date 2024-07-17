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
            VariableUsagePlainNode variableRef = new VariableUsagePlainNode("temp", "temp");
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
                            if (VariableIsMember(module, assignment.Target) != null)
                            {
                                var call = new FunctionCallNode(
                                    "_" + assignment.Target.Name + "_mutator"
                                );
                                call.Arguments.Add(assignment.Expression);
                                function.Statements[i] = call;
                            }
                            assignment.Expression = WalkExpression(assignment.Expression, module, ref variableRef);
                            if(variableRef.Name != "temp")
                            {
                                function.Statements[i] = assignment;
                                var accessor = new FunctionCallNode("_" + variableRef.Name + "_accessor");
                                accessor.Arguments.Add(variableRef);
                                function.Statements.Insert(i, accessor);
                                var tempVar = new VariableDeclarationNode(false, variableRef.Type, "_temp_" + variableRef.Name, module.Name, false);
                                function.VariablesInScope.Add(tempVar.Name, tempVar);
                                function.LocalVariables.Add(tempVar);
                                i++;
                            }
                        }
                        else if (statement.GetType() == typeof(FunctionCallNode))
                        {
                            var call = (FunctionCallNode)statement;
                            for (int j = 0; j<call.Arguments.Count; j++)
                            {
                                var expression = WalkExpression(call.Arguments[j], module, ref variableRef);
                                if (expression != null)
                                {
                                    call.Arguments[j] = expression;
                                }
                            }
                            function.Statements[i] = call;
                        }
                        else if (statement.GetType() == typeof(WhileNode))
                        {
                            var loop = (WhileNode)statement;
                            ((WhileNode)function.Statements[i]).Expression = (BooleanExpressionNode)WalkExpression(loop.Expression, module, ref variableRef);
                        }
                        else if (statement.GetType() == typeof(IfNode))
                        {
                            var ifNode = (IfNode)statement;
                            function.Statements[i] = WalkIf(ifNode, module, ref variableRef);
                        }
                    }
                }
            }
            return modules;
        }

        private static IfNode? WalkIf(IfNode ifNode, ModuleNode module, ref VariableUsagePlainNode variableRef)
        {
            if (ifNode.Expression != null)
            {
                ifNode.Expression = (BooleanExpressionNode)WalkExpression(ifNode.Expression, module, ref variableRef);
                if (ifNode.NextIfNode != null)
                {
                    return WalkIf(ifNode.NextIfNode, module, ref variableRef);
                }
                return ifNode;
            }
            return null;
        }

        private static ExpressionNode? WalkExpression(
            ExpressionNode expression,
            ModuleNode module,
            ref VariableUsagePlainNode variableRef
        )
        {
            if (expression.GetType() == typeof(VariableUsagePlainNode))
            {
                var variable = (VariableUsagePlainNode)expression;
                VariableDeclarationNode member;
                if ((member = VariableIsMember(module, variable)) != null)
                {
                    variableRef = variable;
                    variableRef.Type = member.Type;
                    return new VariableUsagePlainNode("_temp_" + variable.Name, module.Name);
                }
            }
            else if (expression.GetType() == typeof(MathOpNode))
            {
                var mathOp = (MathOpNode)expression;
                var retVal = WalkExpression(mathOp.Left, module, ref variableRef);
                if(retVal != null)
                {
                    mathOp.Left = retVal;
                }
                else
                {
                    retVal = WalkExpression(mathOp.Right, module, ref variableRef);
                    if (retVal == null)
                    {
                        return mathOp;
                    }
                    mathOp.Right = retVal;
                }
                return mathOp;
            }
            else if (expression.GetType() == typeof(BooleanExpressionNode))
            {
                var boolOp = (BooleanExpressionNode)expression;
                var retVal = WalkExpression(boolOp.Left, module, ref variableRef);
                if (retVal != null)
                {
                    boolOp.Left = retVal;
                }
                else
                {
                    retVal = WalkExpression(boolOp.Right, module, ref variableRef);
                    if(retVal == null)
                    {
                        return boolOp;
                    }
                    boolOp.Right = retVal;
                }
                return boolOp;
            }

            return null;
        }

        private static VariableDeclarationNode? VariableIsMember(ModuleNode module, VariableUsagePlainNode variable)
        {
            foreach (var member in module.Records.First().Value.Members)
            {
                if (member.Name == variable.Name)
                {
                    return member;
                }
            }

            return null;
        }
    }
}
