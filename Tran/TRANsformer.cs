using System;
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
                    {
                        for (index = 0; index < function.Statements.Count; index++)
                        {
                            var statement = function.Statements[index];
                            if (statement is AssignmentNode assignment)
                            {
                                //TODO: this is buggy
                                if (function.ParameterVariables.Last().Type is UnknownType record)
                                {
                                    var member = module.Records[record.TypeName].Members[0];
                                    if (member != null)
                                    {
                                        if (function.Name.Contains("mutator"))
                                        {
                                            assignment.Target = new VariableUsagePlainNode(
                                            record.TypeName,
                                            assignment.Target,
                                            VariableUsagePlainNode.VrnExtType.RecordMember,
                                            module.Name
                                            );
                                            assignment.Target.Type = module.Records["this"].Type;
                                        }
                                        else if (function.Name.Contains("accessor"))
                                        {
                                            //This assumes the expression is setting the member - needs to be specified
                                            if(assignment.Expression is VariableUsagePlainNode variable && variable.Name.Equals(member.Name))
                                            {
                                                assignment.Expression = new VariableUsagePlainNode(
                                                record.TypeName,
                                                new VariableUsagePlainNode(member.Name, module.Name),
                                                VariableUsagePlainNode.VrnExtType.RecordMember,
                                                module.Name
                                                );
                                                assignment.Target.Type = module.Records["this"].Type;
                                            }
                                        }
                                        
                                    }
                                }
                            }
                        }
                        continue;
                    }
                    else if (function.Name.Equals("start"))
                    {
                        var thisVar = new VariableDeclarationNode(false, new UnknownType("this"), "this", module.Name, false);
                        function.LocalVariables.Add(thisVar);
                        function.VariablesInScope.Add(thisVar.Name, thisVar);
                    }

                    else if (function.Name[0] == '_')
                    {
                        continue;
                    }

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
                                var thisRef = new VariableUsagePlainNode("this", module.Name);
                                thisRef.NewIsInFuncCallWithVar = true;
                                thisRef.IsInFuncCallWithVar = true;
                                call.Arguments.Add(thisRef);
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

                            for (int i = 0; i < call.Arguments.Count; i++)
                            {
                                if (call.Arguments[i].Type is UnknownType)
                                {
                                    var argument = (VariableUsagePlainNode)call.Arguments[i];
                                    var function = module.Functions[call.Name];
                                    if (function != null)
                                    {
                                        Shank.Type type = function.ParameterVariables[i].Type;
                                        functionNode.VariablesInScope[argument.Name].Type = type;
                                        functionNode
                                            .LocalVariables.Find(
                                                functionNode => functionNode.Name == argument.Name
                                            )
                                            .Type = type;
                                        call.Arguments[i].Type = type;
                                    }
                                    else
                                        throw new Exception(
                                            "Call to unknown function found in function "
                                                + functionNode.Name
                                        );
                                }
                            }
                            for (int j = 0; j < call.Arguments.Count; j++)
                            {
                                call.Arguments[j] =
                                    WalkExpression(call.Arguments[j], module, ref variableRef)
                                    ?? call.Arguments[j];
                                AddAccessor(variableRef, module, call);
                            }

                            //TODO: Fix this later, assumes the function call is within the same class
                            if (module.Functions.ContainsKey(call.Name))
                            {
                                var thisRef = new VariableUsagePlainNode("this", module.Name);
                                thisRef.NewIsInFuncCallWithVar = true;
                                thisRef.IsInFuncCallWithVar = true;
                                call.Arguments.Add(thisRef);
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
                tempArg.NewIsInFuncCallWithVar = true;
                tempArg.IsInFuncCallWithVar = true;
                accessor.Arguments.Add(tempArg);
                var thisRef = new VariableUsagePlainNode("this", module.Name);
                thisRef.NewIsInFuncCallWithVar = true;
                thisRef.IsInFuncCallWithVar = true;
                accessor.Arguments.Add(thisRef);
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
                retVal = WalkExpression(mathOp.Right, module, ref variableRef);
                if (retVal == null)
                {
                    return null;
                }
                mathOp.Right = retVal;
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
                retVal = WalkExpression(boolOp.Right, module, ref variableRef);
                if (retVal == null)
                {
                    return boolOp;
                }
                boolOp.Right = retVal;
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

        //Interfaces should use an enum inside the interface to determine which subtype to use, each implemented subclass should have enum
        //Interfaces: contain an enum inside the interface for subtype of class, each class has a type - do later
        public static List<EnumNode> InterfaceWalk(ModuleNode module)
        {
            List<EnumNode> enums = new List<EnumNode>();

            foreach (var member in module.Records)
            {
                if (member.Value.ParentModuleName.Contains("interface_"))
                {
                    List<String> emptyEnumElements = new List<String>();
                    EnumNode newEnumNode = new EnumNode(
                        "interface",
                        member.Value.Name,
                        emptyEnumElements
                    );
                    enums.Add(newEnumNode);
                }
            }

            return enums;
        }
    }
}
