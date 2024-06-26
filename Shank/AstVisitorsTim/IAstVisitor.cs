using System.Diagnostics;
using Shank.ASTNodes;

namespace Shank.AstVisitorsTim;

public interface IAstVisitor { }

public interface IVariableUsageVisitor : IAstVisitor
{
    void Visit(VariableUsageNodeTemp vun);
}

public interface IVariableUsageIndexVisitor : IAstVisitor
{
    void Visit(VariableUsageIndexNode i);
}

public interface IRecordTypeVisitor : IAstVisitor
{
    void Visit(RecordType rt);
}

public interface IInstantiatedTypeVisitor : IAstVisitor
{
    void Visit(InstantiatedType it);
}

public class MemberExpectingVisitor : IVariableUsageVisitor
{
    private MemberAccessNode? _contents;
    public MemberAccessNode Contents
    {
        get => _contents ?? throw new InvalidOperationException();
        set => _contents = value;
    }

    public void Visit(VariableUsageNodeTemp vun)
    {
        if (vun is VariableUsageMemberNode m)
        {
            Contents = m.Right;
        }
        else
        {
            throw new SemanticErrorException("Expected member; found " + vun);
        }
    }
}

public class MemberValidatingVisitor(MemberExpectingVisitor mev, Type exprType)
    : IInstantiatedTypeVisitor
{
    public MemberExpectingVisitor Mev { get; init; } = mev;
    public Type ExprType { get; init; } = exprType;

    public void Visit(InstantiatedType it)
    {
        var x = it.GetMember(Mev.Contents.Name);
        if (x is not null)
        {
            if (!x.Equals(ExprType))
            {
                throw new SemanticErrorException("Wrong member type");
            }
        }
        else
        {
            throw new SemanticErrorException(
                "Member `" + Mev.Contents.Name + "' not found on record `" + it.NewToString() + "'",
                Mev.Contents
            );
        }
    }
}

public class VuPlainAndDepthGettingVisitor : IVariableUsageVisitor
{
    private VariableUsageNodeTemp? _vuPlain;
    public VariableUsageNodeTemp VuPlain
    {
        get =>
            _vuPlain
            ?? throw new InvalidOperationException(
                "Don't try to access my VuPlain property before passing me to a VUN.Accept."
            );
        set => _vuPlain = value;
    }

    public int Depth { get; set; }

    public void Visit(VariableUsageNodeTemp vun)
    {
        while (vun is not VariableUsagePlainNode)
        {
            vun = vun switch
            {
                VariableUsageIndexNode i => i.Left,
                VariableUsageMemberNode m => m.Left,
                _
                    => throw new UnreachableException(
                        "VUN class hierarchy was altered; please update this switch accordingly."
                    )
            };
            Depth++;
        }

        VuPlain = vun;
    }
}

public class VuAtDepthGettingVisitor(int depth) : IVariableUsageVisitor
{
    public int Depth { get; } = depth;

    private VariableUsageNodeTemp? _vuAtDepth;
    public VariableUsageNodeTemp VuAtDepth
    {
        get =>
            _vuAtDepth
            ?? throw new InvalidOperationException(
                "Don't try to access my VuAtDepth property before passing me to a VUN.Accept."
            );
        set => _vuAtDepth = value;
    }

    public void Visit(VariableUsageNodeTemp vun)
    {
        var d = 0;
        while (d < Depth)
        {
            vun = vun switch
            {
                VariableUsageIndexNode i => i.Left,
                VariableUsageMemberNode m => m.Left,
                VariableUsagePlainNode p => p,
                _
                    => throw new UnreachableException(
                        "VUN class hierarchy was altered; please update this switch accordingly."
                    )
            };
            d++;
        }
        VuAtDepth = vun;
    }
}

public class VunVsTypeCheckingVisitor(
    Type t,
    Dictionary<string, VariableDeclarationNode> vdns,
    Func<ASTNode, Dictionary<string, VariableDeclarationNode>, float> afg,
    Func<ASTNode, Dictionary<string, VariableDeclarationNode>, float> atg
) : IVariableUsageVisitor
{
    public Type CheckingType { get; } = t;

    public Dictionary<string, VariableDeclarationNode> Vdns { get; } = vdns;

    public Func<
        ASTNode,
        Dictionary<string, VariableDeclarationNode>,
        float
    > ActualFromGetter { get; } = afg;

    public Func<
        ASTNode,
        Dictionary<string, VariableDeclarationNode>,
        float
    > ActualToGetter { get; } = atg;

    public string Comment { get; set; } = "";

    public void Visit(VariableUsageNodeTemp vun)
    {
        switch (vun)
        {
            case VariableUsageIndexNode i:
                if (CheckingType is not ArrayType at)
                {
                    throw new SemanticErrorException("Cannot index into ", i);
                }
                CheckRange(i, at.Range);

                break;
            case VariableUsageMemberNode m:
                if (CheckingType is not InstantiatedType it)
                {
                    throw new SemanticErrorException("Cannot dot into ", m);
                }

                if (it.GetMember(m.Right.Name) is null)
                {
                    throw new SemanticErrorException(
                        "Member `"
                            + m.Right.Name
                            + "' not found on record `"
                            + it.NewToString()
                            + "'"
                    );
                }

                break;
        }
    }

    private void CheckRange(VariableUsageIndexNode i, Range checkingRange)
    {
        var expectedFrom = checkingRange.From;
        var expectedTo = checkingRange.To;
        var actualFrom = ActualFromGetter(i.Right, Vdns);
        var actualTo = ActualToGetter(i.Right, Vdns);

        Comment =
            "expected index to be from "
            + expectedFrom
            + " to "
            + expectedTo
            + " and it was actually from "
            + actualFrom
            + " to "
            + actualTo;

        if (actualFrom < expectedFrom || actualTo > expectedTo)
        {
            throw new SemanticErrorException(
                i.Right
                    + " invalid as an index into "
                    + i
                    + "\n\nexpected index to be from "
                    + expectedFrom
                    + "\nto "
                    + expectedTo
                    + "\nbut it was actually from "
                    + actualFrom
                    + "\nto "
                    + actualTo
                    + "\n\n"
            );
        }
    }
}

public class InnerTypeGettingVisitor(VariableUsageNodeTemp vun) : IAstVisitor
{
    public VariableUsageNodeTemp Vun { get; } = vun;
    public Type? InnerType { get; set; }

    public void Visit(Type t)
    {
        switch (t)
        {
            case InstantiatedType it:
                if (vun is VariableUsageMemberNode m)
                {
                    InnerType =
                        it.GetMember(m.Right.Name)
                        ?? throw new UnreachableException(
                            "This should have been caught by VunVsTypeCheckingVisitor."
                        );
                }
                else
                {
                    throw new UnreachableException(
                        "This should have been caught by VunVsTypeCheckingVisitor."
                    );
                }

                break;
            case ArrayType at:
                InnerType = at.Inner;
                break;
            default:
                InnerType = null;
                break;
        }
    }
}
