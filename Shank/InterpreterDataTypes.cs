using System.Text;
using Shank.ASTNodes;

namespace Shank;

public abstract class InterpreterDataType
{
    public abstract override string ToString();
    public abstract void FromString(string input);
    public static InterpreterDataType Default => new DefaultDataType();
}

public class IntDataType : InterpreterDataType
{
    public IntDataType(int value)
    {
        Value = value;
    }

    public int Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }

    public override void FromString(string input)
    {
        Value = int.Parse(input);
    }
}

public class IteratorDataType : InterpreterDataType
{
    public IEnumerator<int> Value { get; set; }

    public IteratorDataType(int count)
    {
        Value = Enumerable.Range(0, count).GetEnumerator();
    }

    public bool MoveNext()
    {
        return Value.MoveNext();
    }

    public int Current
    {
        get { return Value.Current; }
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public override void FromString(string input) { }
}

public class FloatDataType : InterpreterDataType
{
    public FloatDataType(float value)
    {
        Value = value;
    }

    public float Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }

    public override void FromString(string input)
    {
        Value = float.Parse(input);
    }
}

public class BooleanDataType : InterpreterDataType
{
    public BooleanDataType(bool value)
    {
        Value = value;
    }

    public bool Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }

    public override void FromString(string input)
    {
        Value = bool.Parse(input);
    }
}

public class StringDataType : InterpreterDataType
{
    public StringDataType(string value)
    {
        Value = value;
    }

    public string Value { get; set; }

    public override string ToString()
    {
        return Value;
    }

    public override void FromString(string input)
    {
        Value = input;
    }
}

public class CharDataType : InterpreterDataType
{
    public CharDataType(char value)
    {
        Value = value;
    }

    public char Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }

    public override void FromString(string input)
    {
        Value = input[0];
    }
}

public class EnumDataType : InterpreterDataType
{
    public EnumDataType(EnumNode type)
    {
        Type = type;
        Value = "";
    }

    public EnumDataType(EnumNode type, string value)
    {
        Type = type;
        Value = value;
    }

    public EnumDataType(EnumDataType edt)
    {
        Type = edt.Type;
        Value = edt.Value;
    }

    public EnumDataType(string value)
    {
        Value = value;
    }

    public EnumDataType(EnumType t)
    {
        Type = new EnumNode(t.Name, t.ModuleName, t.Variants);
        Value = "";
    }

    public string Value { set; get; }
    public EnumNode? Type { get; set; }

    public override void FromString(string input)
    {
        Value = input;
    }

    public override string ToString()
    {
        return Value;
    }
}

public class ReferenceDataType : InterpreterDataType
{
    public ReferenceDataType(InstantiatedType rn)
    {
        Record = null;
        RecordType = rn;
    }

    public ReferenceDataType(RecordDataType? rdt, InstantiatedType it)
    {
        Record = rdt;
        RecordType = it;
    }

    public ReferenceDataType(ReferenceDataType rdt)
    {
        RecordType = rdt.RecordType;
        Record = rdt.Record;
    }

    public InstantiatedType RecordType { get; init; }
    public RecordDataType? Record { get; set; }

    public override void FromString(string input) { }

    public override string ToString()
    {
        return Record == null ? "Reference: unallocated" : "Reference: " + Record.ToString();
    }
}

public class ArrayDataType : InterpreterDataType
{
    public List<object> Value { get; set; }
    private Range range { get; }
    public ArrayType Type => new(ArrayContentsType, range);
    public Type ArrayContentsType { get; init; }

    public ArrayDataType(List<object> val, ArrayType arrayType)
    {
        Value = val;
        range = arrayType.Range;
        ArrayContentsType = arrayType.Inner;
    }

    public void AddElement(object element, int idx)
    {
        Value.Insert(idx, element);
    }

    public object GetElement(int idx)
    {
        if (idx < Value.Count && idx >= 0)
        {
            return Value[idx];
        }

        throw new InvalidOperationException(
            "\nIndex out of range.\nidx: " + idx + "; count: " + Value.Count
        );
    }

    public float GetElementReal(int idx)
    {
        return (float)GetElement(idx);
    }

    public int GetElementInteger(int idx)
    {
        return (int)GetElement(idx);
    }

    public int NewGetElementInteger(int idx)
    {
        return ((IntDataType)GetElement(idx)).Value;
    }

    public string GetElementString(int idx)
    {
        return (string)GetElement(idx);
    }

    public char GetElementCharacter(int idx)
    {
        if (GetElement(idx) is char c)
        {
            return c;
        }

        throw new InvalidOperationException("Expected array element to be of type: character");
    }

    public bool GetElementBoolean(int idx)
    {
        return (bool)GetElement(idx);
    }

    public override string ToString()
    {
        return "";
    }

    public override void FromString(string input) { }
}

public class RecordDataType : InterpreterDataType
{
    public Dictionary<string, object> Value { get; set; } = [];

    // public Dictionary<string, Type> MemberTypes { get; init; } = [];
    public InstantiatedType MemberTypes;

    public Dictionary<string, Type> getMemberTypes() =>
        MemberTypes
            .Inner.Fields.Select(
                field => (field.Key, field.Value.Instantiate(MemberTypes.InstantiatedGenerics))
            )
            .ToDictionary();

    public RecordDataType(InstantiatedType members)
    {
        MemberTypes = members;
    }

    public RecordDataType(InstantiatedType ty, Dictionary<string, object> val)
    {
        MemberTypes = ty;
        Value = val;
    }

    // public RecordDataType(List<StatementNode> members)
    // {
    //     members.ForEach(s =>
    //     {
    //         if (s is RecordMemberNode rmn)
    //         {
    //             MemberTypes[rmn.Name] = rmn.NewType;
    //         }
    //         else
    //         {
    //             throw new InvalidOperationException(
    //                 "A RecordDataType must be initialized with a List of RecordMemberNode."
    //             );
    //         }
    //     });
    // }

    // public RecordDataType(List<VariableNode> members)
    // {
    //     members.ForEach(vn => MemberTypes.Inner.GetMember(vn.GetNameSafe()) = vn.Type);
    // }

    public RecordDataType(RecordDataType rdt)
    {
        Value = rdt.Value;
        MemberTypes = rdt.MemberTypes;
    }

    public string GetValueString(string key)
    {
        return (string)Value[key];
    }

    public char GetValueCharacter(string key)
    {
        return (char)Value[key];
    }

    public int GetValueInteger(string key)
    {
        return (int)Value[key];
    }

    public float GetValueReal(string key)
    {
        return (float)Value[key];
    }

    public bool GetValueBoolean(string key)
    {
        return (bool)Value[key];
    }

    public ReferenceDataType GetValueReference(string key)
    {
        return (ReferenceDataType)Value[key];
    }

    public RecordDataType GetValueRecord(string key)
    {
        return (RecordDataType)Value[key];
    }

    public EnumDataType GetValueEnum(string key)
    {
        return (EnumDataType)Value[key];
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var member in Value)
        {
            sb.Append(member.Key).Append(": ").Append(member.Value).Append(" ");
        }
        return sb.ToString();
    }

    public override void FromString(string input) { }

    public Type GetMemberType(string rmVrnName)
    {
        return MemberTypes.Inner.GetMember(rmVrnName, MemberTypes.InstantiatedGenerics);
    }
}

public class DefaultDataType : InterpreterDataType
{
    public override string ToString()
    {
        return "default";
    }

    public override void FromString(string input)
    {
        throw new NotImplementedException();
    }
}
