using System.Diagnostics;
using Shank.ASTNodes;

namespace Shank;

public static class TypesHelper
{
    public static Type? GetInnerType(Type t, VariableUsageNodeTemp vun)
    {
        switch (t)
        {
            case InstantiatedType it:
                if (vun is VariableUsageMemberNode m)
                {
                    return it.GetMemberSafe(m.Right.Name, m);
                }
                throw new SemanticErrorException("Cannot dot into a " + vun.GetType(), vun);

            case ArrayType at:
                return at.Inner;
            default:
                return null;
        }
    }

    public static InterpreterDataType ToIdt(this Type t, ExpressionNode? e)
    {
        switch (t)
        {
            case IntegerType:
                return new IntDataType(((IntNode)(e ?? new IntNode(default))).Value);
            case RealType:
                return new FloatDataType(((FloatNode)(e ?? new FloatNode(default))).Value);
            case CharacterType:
                return new CharDataType(((CharNode)(e ?? new CharNode(default))).Value);
            case BooleanType:
                return new BooleanDataType(((BoolNode)(e ?? new BoolNode(default))).Value);
            case StringType:
                return new StringDataType(((StringNode)(e ?? new StringNode(""))).Value);
            case InstantiatedType it:
                return it.ToIdt();
            case ArrayType at:
                return at.ToIdt();
            case EnumType et:
                return new EnumDataType(et);
            case ReferenceType rt:
                var innerIt = (InstantiatedType)rt.Inner;
                return new ReferenceDataType(innerIt);
            // TODO: GenericType case
            default:
                throw new UnreachableException("Type not recognized");
        }
    }

    public static InterpreterDataType ToIdt(this InstantiatedType t)
    {
        return new RecordDataType(
            t,
            t.Inner.Fields.Select(
                kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value.ToIdt(null))
            )
                .ToDictionary()
        );
    }

    public static ArrayDataType ToIdt(this ArrayType t)
    {
        var adtArr = new object[(int)t.Range.From + (int)t.Range.To + 1];
        Enumerable
            .Range((int)t.Range.From, (int)t.Range.To + 1)
            .ToList()
            .ForEach(i =>
            {
                adtArr[i] = t.Inner.ToIdt(null);
            });
        return new ArrayDataType([..adtArr], t);
    }

    // ToIdtDangerous is dangerous because if you call it in the wrong place when pre-rendering IDTs
    // for self-referential structures in the AST (e.g. linked lists), it stack-overflows.
    public static InterpreterDataType ToIdtDangerous(this Type t, ExpressionNode? e)
    {
        switch (t)
        {
            case ReferenceType refDt:
                var innerIt = (InstantiatedType)refDt.Inner;
                var recDt = (RecordDataType)innerIt.ToIdt();
                return new ReferenceDataType(recDt, innerIt);
            default:
                return t.ToIdt(e);
        }
    }
}
