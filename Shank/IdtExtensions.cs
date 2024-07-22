using LLVMSharp;
using Newtonsoft.Json;
using Shank.ASTNodes;
using Shank.MathOppable;
using Shank.WalkCompliantVisitors;

namespace Shank;

public static class IdtExtensions
{
    public delegate int IntResolver(
        ASTNode node,
        Dictionary<string, InterpreterDataType> variables
    );

    public static InterpreterDataType? GetInnerIdt(
        this InterpreterDataType idt,
        VariableUsageNodeTemp vun,
        IntResolver? resolveInt,
        Dictionary<string, InterpreterDataType>? variables
    )
    {
        switch (idt)
        {
            case RecordDataType rdt:
                if (vun is VariableUsageMemberNode m)
                    return (InterpreterDataType)rdt.Value[m.Right.Name];

                throw new InvalidOperationException(
                    "Wrong lookup node provided for RecordDataType."
                );

            case ArrayDataType adt:
                if (vun is not VariableUsageIndexNode i)
                    throw new InvalidOperationException();

                if (resolveInt is null || variables is null)
                    throw new InvalidOperationException();

                return (InterpreterDataType)adt.Value[resolveInt(i.Right, variables)];
            case ReferenceDataType refDt:
                if (vun is VariableUsageMemberNode mm)
                {
                    if (mm.Line == 9)
                        OutputHelper.DebugPrintJson(refDt.Record, "blah");
                    return refDt.Record?.Value[mm.Right.Name] as InterpreterDataType
                        ?? throw new InvalidOperationException("Record has not been allocated.");
                }
                throw new InvalidOperationException();
            default:
                return null;
        }
    }

    public static T CopyAs<T>(this InterpreterDataType idt)
        where T : class
    {
        switch (idt)
        {
            case RecordDataType rdt:
                return new RecordDataType(
                        rdt.MemberTypes,
                        rdt.Value.Select(
                            kvp =>
                                new KeyValuePair<string, object>(
                                    kvp.Key,
                                    ((InterpreterDataType)kvp.Value).CopyAs<object>()
                                )
                        )
                            .ToDictionary()
                    ) as T
                    ?? throw new InvalidOperationException();

            case ArrayDataType adt:
                var retVal = new List<object>();
                foreach (var o in adt.Value)
                {
                    retVal.Add(((InterpreterDataType)o).CopyAs<object>());
                }
                return new ArrayDataType(retVal, adt.Type) as T
                    ?? throw new InvalidOperationException();

            case IntDataType intVal:
                return new IntDataType(intVal.Value) as T ?? throw new InvalidOperationException();
            case FloatDataType floatVal:
                return new FloatDataType(floatVal.Value) as T
                    ?? throw new InvalidOperationException();
            case StringDataType stringVal:
                return new StringDataType(stringVal.Value) as T
                    ?? throw new InvalidOperationException();
            case CharDataType charVal:
                return new CharDataType(charVal.Value) as T
                    ?? throw new InvalidOperationException();
            case BooleanDataType boolVal:
                return new BooleanDataType(boolVal.Value) as T
                    ?? throw new InvalidOperationException();
            case EnumDataType enumVal:
                return new EnumDataType(
                        enumVal.Type ?? throw new InvalidOperationException(),
                        enumVal.Value
                    ) as T
                    ?? throw new InvalidOperationException();
            case ReferenceDataType referenceVal:
                return new ReferenceDataType(
                        referenceVal.Record?.CopyAs<RecordDataType>(),
                        referenceVal.RecordType
                    ) as T
                    ?? throw new InvalidOperationException("T " + typeof(T) + " is not right.");
            default:
                throw new NotImplementedException("Cannot copy type " + idt);
        }
    }

    public static bool TryGetMathOppable(this InterpreterDataType idt, out IMathOppable val)
    {
        switch (idt)
        {
            case IntDataType i:
                val = new MathOppableInt(i.Value);
                break;
            case FloatDataType f:
                val = new MathOppableFloat(f.Value);
                break;
            default:
                val = IMathOppable.Default;
                return false;
        }

        return true;
    }

    public static T DeepCopyJsonDotNet<T>(this T input)
        where T : class
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(input))
            ?? throw new InvalidOperationException("???");
    }

    public static bool TryGetIdt(
        this List<InterpreterDataType> these,
        int idx,
        out InterpreterDataType idt
    )
    {
        if (idx < these.Count)
        {
            idt = these[idx];
            return true;
        }

        idt = InterpreterDataType.Default;
        return false;
    }
}

public class TypeConverter<T> : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? val, JsonSerializer serializer)
    {
        if (val is not null)
            val = (T)val;
        serializer.Serialize(writer, val);
    }

    public override object? ReadJson(
        JsonReader reader,
        System.Type objectType,
        object? existingValue,
        JsonSerializer serializer
    )
    {
        return serializer.Deserialize<T>(reader);
    }

    public override bool CanConvert(System.Type objectType)
    {
        return objectType == typeof(Type);
    }
}
