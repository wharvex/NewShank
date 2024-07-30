using System.Diagnostics;
using Shank.ExprVisitors;
using Shank.MathOppable;

namespace Shank.ASTNodes;

public class MathOpNode(ExpressionNode left, MathOpNode.MathOpType op, ExpressionNode right)
    : ExpressionNode
{
    public MathOpType Op { get; init; } = op;
    public ExpressionNode Left { get; set; } = left;
    public ExpressionNode Right { get; set; } = right;

    public override string ToString()
    {
        return $"{Left} {Op} {Right}";
    }

    public enum MathOpType
    {
        Plus,
        Minus,
        Times,
        Divide,
        Modulo
    }

    public override void Accept(Visitor v) => v.Visit(this);

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        Left = (ExpressionNode)(Left.Walk(v) ?? Left);

        Right = (ExpressionNode)(Right.Walk(v) ?? Right);

        return v.PostWalk(this);
    }

    public float GetResultOfOp(float l, float r)
    {
        return Op switch
        {
            MathOpType.Plus => l + r,
            MathOpType.Minus => l - r,
            MathOpType.Divide => l / r,
            MathOpType.Times => l * r,
            MathOpType.Modulo => l % r,
            _ => throw new NotImplementedException()
        };
    }

    public int GetResultOfOp(int l, int r)
    {
        return Op switch
        {
            MathOpType.Plus => l + r,
            MathOpType.Minus => l - r,
            MathOpType.Divide => l / r,
            MathOpType.Times => l * r,
            MathOpType.Modulo => l % r,
            _ => throw new NotImplementedException()
        };
    }

    public string GetResultOfOp(string l, string r)
    {
        return Op switch
        {
            MathOpType.Plus => l + r,
            _ => throw new NotImplementedException()
        };
    }

    public IMathOppable GetResultOfOp(InterpreterDataType l, InterpreterDataType r)
    {
        if (!l.TryGetMathOppable(out var lVal) || !r.TryGetMathOppable(out var rVal))
        {
            throw new InterpreterErrorException(
                "No math allowed on non-numeric, non-string types.",
                this
            );
        }

        if (lVal.GetType() != rVal.GetType())
        {
            throw new InterpreterErrorException("No math allowed on non-matching types.", this);
        }

        switch ((lVal, rVal))
        {
            case (MathOppableInt li, MathOppableInt ri):
                return new MathOppableInt(GetResultOfOp(li.Contents, ri.Contents));
            case (MathOppableFloat lf, MathOppableFloat rf):
                return new MathOppableFloat(GetResultOfOp(lf.Contents, rf.Contents));
            case (MathOppableString ls, MathOppableString rs):
                return new MathOppableString(GetResultOfOp(ls.Contents, rs.Contents));
            default:
                throw new UnreachableException();
        }
    }
}
