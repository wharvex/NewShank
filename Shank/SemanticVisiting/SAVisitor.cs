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

public class AssignmentNodeTypeCheckerVisitor : SAVisitor
{
    public override ASTNode? Visit(AssignmentNode node)
    {
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
