using Shank.ASTNodes;

namespace Shank;

public abstract class SAVisitor
{
    public virtual ASTNode? Visit(ProgramNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(ProgramNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(ModuleNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(ModuleNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(FunctionNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(FunctionNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(FunctionCallNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(FunctionCallNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(VariableDeclarationNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(VariableDeclarationNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(VariableUsagePlainNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(VariableUsagePlainNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(AssignmentNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(AssignmentNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(WhileNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(WhileNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(IfNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(IfNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(ForNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(ForNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(RepeatNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(RepeatNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(MathOpNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(MathOpNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(IntNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(IntNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(FloatNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(FloatNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(CharNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(CharNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(BoolNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(BoolNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(BooleanExpressionNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(BooleanExpressionNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(StringNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(StringNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(EnumNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(EnumNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(RecordNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(RecordNode node)
    {
        return null;
    }

    public virtual ASTNode? Visit(BuiltInFunctionNode node)
    {
        return null;
    }

    public virtual ASTNode? PostWalk(BuiltInFunctionNode node)
    {
        return null;
    }
}

public class ForNodeVisitor : SAVisitor
{
    private Dictionary<string, VariableDeclarationNode> Variables;

    public override ASTNode? Visit(FunctionNode node)
    {
        Variables = node.VariablesInScope;
        return null;
    }

    public override ASTNode? Visit(ForNode node)
    {
        if (SemanticAnalysis.GetTypeOfExpression(node.Variable, Variables) is not IntegerType)
        {
            throw new SemanticErrorException(
                $"For loop has a non-integer index called {node.Variable.Name}. This is not allowed.",
                node.Variable
            );
        }

        if (SemanticAnalysis.GetTypeOfExpression(node.From, Variables) is not IntegerType)
        {
            throw new SemanticErrorException(
                $"For loop must have its ranges be integers!",
                node.From
            );
        }

        if (SemanticAnalysis.GetTypeOfExpression(node.To, Variables) is not IntegerType)
        {
            throw new SemanticErrorException(
                $"For loop must have its ranges be integers!",
                node.To
            );
        }

        return null;
    }
}

public class BooleanExpressionNodeVisitor : SAVisitor
{
    protected Dictionary<string, VariableDeclarationNode> Variables;

    public override ASTNode? Visit(FunctionNode node)
    {
        Variables = node.VariablesInScope;
        return null;
    }

    public override ASTNode? Visit(BooleanExpressionNode node)
    {
        if (
            SemanticAnalysis.GetTypeOfExpression(node.Left, Variables).GetType()
            != SemanticAnalysis.GetTypeOfExpression(node.Right, Variables).GetType()
        )
        {
            throw new SemanticErrorException(
                "Cannot compare expressions of different types.",
                node
            );
        }
        return null;
    }
}

public class FunctionCallVisitor : SAVisitor
{
    protected Dictionary<string, CallableNode> Functions;

    protected Dictionary<string, VariableDeclarationNode> Variables;

    public override ASTNode? Visit(ModuleNode node)
    {
        Functions = node.Functions;
        return null;
    }

    public override ASTNode? Visit(FunctionNode node)
    {
        Variables = node.VariablesInScope;
        return null;
    }
}

public class FunctionCallTypeVisitor : FunctionCallVisitor
{
    public override ASTNode? Visit(FunctionCallNode node)
    {
        var function = Functions[node.Name];
        if (function is not BuiltInVariadicFunctionNode)
        {
            for (var index = 0; index < node.Arguments.Count; index++)
            {
                if (
                    SemanticAnalysis.GetTypeOfExpression(node.Arguments[index], Variables).GetType()
                    != function.ParameterVariables[index].Type.GetType()
                )
                {
                    throw new SemanticErrorException(
                        "Type of argument does not match what is required for the function",
                        node
                    );
                }
            }
        }

        return null;
    }
}

public class FunctionCallCountVisitor : FunctionCallVisitor
{
    public override ASTNode? Visit(FunctionCallNode node)
    {
        var function = Functions[node.Name];

        int defaultParameterCount = 0;
        foreach (var variableDeclarationNode in Variables.Values)
        {
            if (variableDeclarationNode.IsDefaultValue)
                defaultParameterCount++;
        }

        if (function is not BuiltInVariadicFunctionNode)
        {
            if (
                node.Arguments.Count < function.ParameterVariables.Count - defaultParameterCount
                || node.Arguments.Count > function.ParameterVariables.Count
            )
                throw new SemanticErrorException(
                    "Function call does not have a valid amount of arguments for the function",
                    node
                );
        }

        return null;
    }
}

public class FunctionCallDefaultVisitor : FunctionCallVisitor
{
    public override ASTNode? Visit(FunctionCallNode node)
    {
        // Adds the default values to the funciton call to help with the compiler
        var function = Functions[node.Name];
        if (function.ParameterVariables.Count - node.Arguments.Count > 0)
        {
            for (int i = node.Arguments.Count; i < function.ParameterVariables.Count; i++)
            {
                node.Arguments.Add((ExpressionNode)function.ParameterVariables[i].InitialValue);
            }
        }
        return null;
    }
}

public class MathOpNodeOptimizer : SAVisitor
{
    public override ASTNode? PostWalk(MathOpNode node)
    {
        if (node.Op != MathOpNode.MathOpType.Divide && node.Op != MathOpNode.MathOpType.Times)
        {
            if (node.Left is IntNode left)
                if (left.Value == 0)
                    return node.Right;
            if (node.Left is FloatNode left2)
                if (left2.Value == 0.0)
                    return node.Right;

            if (node.Right is IntNode right)
                if (right.Value == 0)
                    return node.Left;
            if (node.Right is FloatNode right2)
                if (right2.Value == 0.0)
                    return node.Left;
        }
        return null;
    }
}
