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

public interface IRecordDataTypeVisitor : IAstVisitor
{
    void Visit(RecordDataType rt);
}

public interface IInstantiatedTypeVisitor : IAstVisitor
{
    void Visit(InstantiatedType it);
}

public interface IArrayTypeVisitor : IAstVisitor
{
    void Visit(ArrayType at);
}

public interface IAstTypeVisitor : IAstVisitor
{
    void Visit(Type t);
}

public interface IAstExpressionVisitor : IAstVisitor
{
    void Visit(ExpressionNode e);
}

public interface IInterpreterDataTypeVisitor
{
    void Visit(InterpreterDataType idt);
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

public class VunVsTypeCheckingVisitor(Type t, Dictionary<string, VariableDeclarationNode> vDecs)
    : IVariableUsageVisitor
{
    public Type CheckingType { get; } = t;

    public Dictionary<string, VariableDeclarationNode> VDecs { get; } = vDecs;

    public string Comment { get; set; } = "";

    public void Visit(VariableUsageNodeTemp vun)
    {
        switch (vun)
        {
            case VariableUsageIndexNode i:
                var at = CheckingType switch
                {
                    ArrayType arrRet => arrRet,
                    ReferenceType(Inner: ArrayType arrRet) => arrRet,
                    _
                        => throw new SemanticErrorException(
                            "Only arrays can be indexed into. Found: " + CheckingType.GetType(),
                            i.Left
                        )
                };
                // CheckRange(i, at.Range);

                break;
            case VariableUsageMemberNode m:
                switch (CheckingType)
                {
                    case InstantiatedType it:
                        if (it.GetMember(m.Right.Name) is null)
                            throw new SemanticErrorException(
                                "Member `" + m.Right.Name + "' not found on record `" + it + "'"
                            );
                        break;
                    case ReferenceType rt:
                        if (rt.Inner is not InstantiatedType)
                            throw new SemanticErrorException(
                                "Cannot dot into reference to " + rt.Inner.GetType(),
                                m
                            );
                        break;
                    default:
                        throw new SemanticErrorException("Cannot dot into " + CheckingType, m);
                }
                break;
        }
    }

    private void CheckRange(VariableUsageIndexNode i, Range checkingRange)
    {
        var expectedFrom = checkingRange.From;
        var expectedTo = checkingRange.To;

        var arVis = new ActualRangeGettingVisitor(VDecs);
        i.Right.Accept(arVis);

        var actualFrom = arVis.ActualFrom;
        var actualTo = arVis.ActualTo;

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
            case ReferenceType rt:
                InnerType = rt.Inner;
                break;
            default:
                throw new UnreachableException("Bad type for getting inner: " + t.GetType());
        }
    }
}

public class VunTypeGettingVisitor(
    Type originalType,
    Dictionary<string, VariableDeclarationNode> vDecs
) : IVariableUsageVisitor
{
    public Dictionary<string, VariableDeclarationNode> VDecs { get; } = vDecs;

    public Type VunType { get; set; } = originalType;

    public void Visit(VariableUsageNodeTemp vun)
    {
        // Get the depth d of the innermost vun in the target's vun structure.
        var padVis = new VuPlainAndDepthGettingVisitor();
        vun.Accept(padVis);
        var d = padVis.Depth;

        // Set t to the outermost type in the target's type structure.
        var t = VunType;

        // Ensure vun and type agree internally, and end up with t set to the type which the
        // assignment's "expression" (it's RHS) should be.
        while (true)
        {
            // Recurse through the type structure but "reverse-recurse" through the vun structure.
            d--;
            if (d < 0)
            {
                break;
            }

            // Get variable usage at depth d of target and store it in vadVis.
            var vadVis = new VuAtDepthGettingVisitor(d);
            vun.Accept(vadVis);

            // Ensure the variable usage in vadVis and the type in t "agree internally".
            vadVis.VuAtDepth.Accept(new VunVsTypeCheckingVisitor(t, vDecs));

            // Set t to its own inner type (forward recursion).
            var itVis = new InnerTypeGettingVisitor(vadVis.VuAtDepth);
            t.Accept(itVis);
            t = itVis.InnerType;

            // itVis.InnerType is null if there are no more inner types (this shouldn't happen).
            if (t is null)
            {
                break;
            }
        }

        VunType = t ?? throw new InvalidOperationException();
    }
}

public class ActualRangeGettingVisitor(Dictionary<string, VariableDeclarationNode> vDecs)
    : IAstExpressionVisitor
{
    public Dictionary<string, VariableDeclarationNode> VDecs { get; } = vDecs;
    public float ActualFrom { get; set; }
    public float ActualTo { get; set; }

    public float GetActualLowerOrUpper(
        ExpressionNode node,
        Dictionary<string, VariableDeclarationNode> variables,
        bool getUpper
    )
    {
        switch (node)
        {
            case MathOpNode mon:
                return mon.Op switch
                {
                    MathOpNode.MathOpType.Plus
                        => GetActualLowerOrUpper(mon.Left, variables, getUpper)
                            + GetActualLowerOrUpper(mon.Right, variables, getUpper),
                    MathOpNode.MathOpType.Minus
                        => GetActualLowerOrUpper(mon.Left, variables, getUpper)
                            - GetActualLowerOrUpper(mon.Right, variables, !getUpper),
                    MathOpNode.MathOpType.Times
                        => GetActualLowerOrUpper(mon.Left, variables, getUpper)
                            * GetActualLowerOrUpper(mon.Right, variables, getUpper),
                    MathOpNode.MathOpType.Divide
                        => GetActualLowerOrUpper(mon.Left, variables, !getUpper)
                            / GetActualLowerOrUpper(mon.Right, variables, getUpper),
                    MathOpNode.MathOpType.Modulo => 0,
                    _ => throw new InvalidOperationException()
                };
            case IntNode i:
                return i.Value;
            case FloatNode f:
                return f.Value;
            case StringNode s:
                return s.Value.Length;
            case VariableUsageNodeTemp vun:
            {
                var vtVis = new VunTypeGettingVisitor(
                    variables[vun.GetPlain().Name].Type,
                    variables
                );
                if (vtVis.VunType is RangeType t)
                {
                    return getUpper ? t.Range.To : t.Range.From;
                }
                throw new Exception(
                    "Ranged variables can only be assigned variables with a range."
                );
            }
            default:
                throw new InvalidOperationException(
                    "Unrecognized node type in math expression while checking range"
                );
        }
    }

    public void Visit(ExpressionNode e)
    {
        ActualFrom = GetActualLowerOrUpper(e, VDecs, false);
        ActualTo = GetActualLowerOrUpper(e, VDecs, true);
    }
}

public class ExpressionTypeGettingVisitor : IAstExpressionVisitor
{
    private Type? _exprType;
    public Type ExprType
    {
        get => _exprType ?? throw new InvalidOperationException();
        set => _exprType = value;
    }

    public void Visit(ExpressionNode e)
    {
        ExprType = e switch
        {
            IntNode => new IntegerType(),
            FloatNode => new RealType(),
            BoolNode => new BooleanType(),
            StringNode => new StringType(),
        };
    }
}

public class TypeToInterpreterDataTypeConvertingVisitor(ExpressionNode? initVal) : IAstTypeVisitor
{
    private ExpressionNode? InitVal { get; } = initVal;
    private InterpreterDataType? _idt;
    public InterpreterDataType Idt
    {
        get => _idt ?? throw new InvalidOperationException();
        set => _idt = value;
    }

    public void Visit(Type t)
    {
        switch (t)
        {
            case IntegerType:
                Idt = new IntDataType(((IntNode)(InitVal ?? new IntNode(default))).Value);
                break;
            case RealType:
                Idt = new FloatDataType(((FloatNode)(InitVal ?? new FloatNode(default))).Value);
                break;
            case CharacterType:
                Idt = new CharDataType(((CharNode)(InitVal ?? new CharNode(default))).Value);
                break;
            case BooleanType:
                Idt = new BooleanDataType(((BoolNode)(InitVal ?? new BoolNode(default))).Value);
                break;
            case StringType:
                Idt = new StringDataType(((StringNode)(InitVal ?? new StringNode(""))).Value);
                break;
            case InstantiatedType it:
                var itrVis = new InstantiatedTypeToRecordDataTypeConvertingVisitor();
                it.Accept(itrVis);
                Idt = itrVis.Rdt;
                break;
            case ArrayType at:
                var ataVis = new ArrayTypeToArrayDataTypeConvertingVisitor();
                at.Accept(ataVis);
                Idt = ataVis.Adt;
                break;
            // TODO: Cases for Enums and References.
            default:
                throw new UnreachableException("Type not recognized");
        }
    }
}

public class InstantiatedTypeToRecordDataTypeConvertingVisitor : IInstantiatedTypeVisitor
{
    private RecordDataType? _rdt;
    public RecordDataType Rdt
    {
        get => _rdt ?? throw new InvalidOperationException();
        set => _rdt = value;
    }

    public void Visit(InstantiatedType it)
    {
        Rdt = new RecordDataType(
            it,
            it.Inner.Fields.Select(kvp =>
            {
                var vtiVis = new TypeToInterpreterDataTypeConvertingVisitor(null);
                kvp.Value.Accept(vtiVis);
                return new KeyValuePair<string, object>(kvp.Key, vtiVis.Idt);
            })
                .ToDictionary()
        );
    }
}

public class ArrayTypeToArrayDataTypeConvertingVisitor : IArrayTypeVisitor
{
    private ArrayDataType? _adt;
    public ArrayDataType Adt
    {
        get => _adt ?? throw new InvalidOperationException();
        set => _adt = value;
    }

    public void Visit(ArrayType at)
    {
        List<object> adtList = [];
        Enumerable
            .Range((int)at.Range.From, (int)at.Range.To)
            .ToList()
            .ForEach(i =>
            {
                var ttiVis = new TypeToInterpreterDataTypeConvertingVisitor(null);
                at.Inner.Accept(ttiVis);
                adtList.Insert(i, ttiVis.Idt);
            });
        Adt = new ArrayDataType(adtList, at);
    }
}

public class VunToInterpreterDataTypeConvertingVisitor : IVariableUsageVisitor
{
    public void Visit(VariableUsageNodeTemp vun)
    {
        throw new NotImplementedException();
    }
}

public class InnerIdtGettingVisitor(VariableUsageNodeTemp vun) : IInterpreterDataTypeVisitor
{
    public VariableUsageNodeTemp Vun { get; } = vun;
    public InterpreterDataType? InnerIdt { get; set; }

    public void Visit(InterpreterDataType idt)
    {
        throw new NotImplementedException();
    }
}
