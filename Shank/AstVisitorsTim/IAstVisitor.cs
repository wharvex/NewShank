﻿using Shank.ASTNodes;

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

public class MemberValidatingVisitor(MemberExpectingVisitor mev, Type expType)
    : IInstantiatedTypeVisitor
{
    public MemberExpectingVisitor Mev { get; init; } = mev;
    public Type ExpType { get; init; } = expType;

    public void Visit(InstantiatedType it)
    {
        var x = it.GetMember(Mev.Contents.Name);
        if (x is not null)
        {
            if (!x.Equals(ExpType))
            {
                throw new SemanticErrorException("Wrong member type");
            }
        }
        else
        {
            throw new SemanticErrorException("Member not found");
        }
    }
}
