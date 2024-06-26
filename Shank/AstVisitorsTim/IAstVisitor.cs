using System.Diagnostics;
using Shank.ASTNodes;

namespace Shank.AstVisitorsTim;

public interface IAstVisitor { }

public interface IVariableUsageVisitor : IAstVisitor
{
    void Visit(VariableUsageNodeTemp vun);
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
    private int Depth { get; } = depth;

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
