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

public class SemanticAnalysisMemberAccessGettingVisitor : IVariableUsageVisitor
{
    private MemberAccessNode? _memberAccess;
    public MemberAccessNode MemberAccess
    {
        get => _memberAccess ?? throw new InvalidOperationException();
        set => _memberAccess = value;
    }

    public void Visit(VariableUsageNodeTemp vun)
    {
        if (vun is VariableUsageMemberNode vumn)
        {
            MemberAccess = vumn.Right;
        }

        throw new SemanticErrorException("Variable usage looked up as record but is dotless.");
    }
}

public class SemanticAnalysisMemberAccessTypeCheckingVisitor(
    SemanticAnalysisMemberAccessGettingVisitor samagv,
    Type rhsType
) : IRecordTypeVisitor
{
    public SemanticAnalysisMemberAccessGettingVisitor Samagv { get; init; } = samagv;
    public Type RhsType { get; init; } = rhsType;

    public void Visit(RecordType rt)
    {
        var x = rt.GetMember(Samagv.MemberAccess.Name, []);
        if (x is not null)
        {
            if (!x.Equals(RhsType))
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
