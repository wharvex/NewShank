﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shank;
using Shank.ASTNodes;

namespace Tran
{
    public class TRANsformer
    {
        private static FunctionNode function;
        private static int index;

        public static Dictionary<string, ModuleNode> Walk(Dictionary<string, ModuleNode> modules)
        {
            VariableUsagePlainNode variableRef = new VariableUsagePlainNode("temp", "temp");
            foreach (var module in modules.Values)
            {
                foreach (FunctionNode functionNode in module.Functions.Values)
                {
                    function = functionNode;
                    if (function.Name[0] == '_')
                        continue;
                    for (index = 0; index < function.Statements.Count; index++)
                    {
                        var statement = function.Statements[index];

                        if (statement.GetType() == typeof(AssignmentNode))
                        {
                            var assignment = (AssignmentNode)statement;
                            if (VariableIsMember(module, assignment.Target) != null)
                            {
                                var call = new FunctionCallNode(
                                    "_" + assignment.Target.Name + "_mutator"
                                );
                                call.Arguments.Add(assignment.Expression);
                                function.Statements[index] = call;
                            }
                            assignment.Expression =
                                WalkExpression(assignment.Expression, module, ref variableRef)
                                ?? assignment.Expression;
                            AddAccessor(variableRef, module, assignment);
                        }
                        else if (statement.GetType() == typeof(FunctionCallNode))
                        {
                            var call = (FunctionCallNode)statement;
                            if (call.Arguments[0].Type is UnknownType)
                            {
                                var argument = (VariableUsagePlainNode)call.Arguments[0];
                                var function = module.Functions[call.Name];
                                if (function != null)
                                {
                                    Shank.Type type = function.ParameterVariables[0].Type;
                                    functionNode.VariablesInScope[argument.Name].Type = type;
                                    functionNode
                                        .LocalVariables.Find(
                                            functionNode => functionNode.Name == argument.Name
                                        )
                                        .Type = type;
                                    call.Arguments[0].Type = type;
                                }
                                else
                                    throw new Exception(
                                        "Call to unknown function found in function "
                                            + functionNode.Name
                                    );
                            }
                            for (int j = 0; j < call.Arguments.Count; j++)
                            {
                                call.Arguments[j] =
                                    WalkExpression(call.Arguments[j], module, ref variableRef)
                                    ?? call.Arguments[j];
                                AddAccessor(variableRef, module, call);
                            }
                        }
                        else if (statement.GetType() == typeof(WhileNode))
                        {
                            var loop = (WhileNode)statement;
                            ((WhileNode)function.Statements[index]).Expression =
                                (BooleanExpressionNode?)WalkExpression(
                                    loop.Expression,
                                    module,
                                    ref variableRef
                                ) ?? ((WhileNode)function.Statements[index]).Expression;
                            AddAccessor(variableRef, module, loop);
                        }
                        else if (statement.GetType() == typeof(IfNode))
                        {
                            var ifNode = (IfNode)statement;
                            WalkIf(ifNode, module, ref variableRef);
                        }
                    }
                }
            }
            return modules;
        }

        private static void AddAccessor(
            VariableUsagePlainNode variableRef,
            ModuleNode module,
            StatementNode statement
        )
        {
            if (
                variableRef.Name != "temp"
                && !function.VariablesInScope.ContainsKey("_temp_" + variableRef.Name)
            )
            {
                var tempVar = new VariableDeclarationNode(
                    false,
                    variableRef.Type,
                    "_temp_" + variableRef.Name,
                    module.Name,
                    false
                );
                function.VariablesInScope.Add(tempVar.Name, tempVar);
                function.LocalVariables.Add(tempVar);
                var accessor = new FunctionCallNode("_" + variableRef.Name + "_accessor");
                var tempArg = new VariableUsagePlainNode("_temp_" + variableRef.Name, module.Name);
                tempArg.IsInFuncCallWithVar = true;
                accessor.Arguments.Add(tempArg);
                function.Statements.Insert(index, accessor);
                index++;
            }
        }

        private static void WalkIf(
            IfNode ifNode,
            ModuleNode module,
            ref VariableUsagePlainNode variableRef
        )
        {
            if (ifNode.Expression != null)
            {
                ifNode.Expression =
                    (BooleanExpressionNode?)WalkExpression(
                        ifNode.Expression,
                        module,
                        ref variableRef
                    ) ?? ifNode.Expression;
                AddAccessor(variableRef, module, ifNode);
                if (ifNode.NextIfNode != null)
                {
                    WalkIf(ifNode.NextIfNode, module, ref variableRef);
                }
            }
        }

        //Returns a new expression if it finds a variable reference that is a member
        //Replaces the variable reference with a new variable called temp
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
                if (retVal != null)
                {
                    mathOp.Left = retVal;
                }
                else
                {
                    retVal = WalkExpression(mathOp.Right, module, ref variableRef);
                    if (retVal == null)
                    {
                        return null;
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
                    if (retVal == null)
                    {
                        return boolOp;
                    }
                    boolOp.Right = retVal;
                }
                return boolOp;
            }

            return null;
        }

        private static VariableDeclarationNode? VariableIsMember(
            ModuleNode module,
            VariableUsagePlainNode variable
        )
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
