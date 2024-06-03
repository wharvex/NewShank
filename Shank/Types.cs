namespace Shank;

public interface IType;

public struct Range
{
    public Range(float from, float to)
    {
        From = from;
        To = to;
    }

    public float From { get; set; }
    public float To { get; set; }
}
public interface IRangeType : IType
{
    public Range? Range { get; set; }
}
public struct BooleanType : IType;

public struct StringType : IRangeType
{
    public StringType(Range? range = null)
    {
        Range = range;
    }

    public Range? Range { get; set; }
}



public struct RealType : IRangeType
{
    public RealType(Range? range = null)
    {
        Range = range;
    }

    public Range? Range { get; set; }
}

public struct IntegerType : IRangeType
{
    public IntegerType(Range? range = null)
    {
        Range = range;
    }

    public Range? Range { get; set; }
}

public struct CharacterType : IType;

public record struct EnumType(List<String> Variants) : IType;

public record struct RecordType(Dictionary<String, IType> Fields, List<string> Generics) : IType;

public record struct ArrayType(IType Inner, Range? Range = null) : IRangeType
{
    public Range? Range { get; set; } = Range;
}

public record struct UnknownType(String TypeName, List<IType> TypeParameters) : IType
{
    public UnknownType(String TypeName) : this(TypeName, new List<IType>())
    {
    }
    public VariableNode.UnknownTypeResolver ResolveUnknownType(ModuleNode parentModule)
    {
        if (
                parentModule.getEnums().ContainsKey(TypeName)
                && parentModule.Records.ContainsKey(TypeName)
            )
            {
                return VariableNode.UnknownTypeResolver.Multiple;
            }

            if (parentModule.getEnums().ContainsKey(TypeName))
            {
                return VariableNode.UnknownTypeResolver.Enum;
            }

            return parentModule.Records.ContainsKey(TypeName)
                ? VariableNode.UnknownTypeResolver.Record
                : VariableNode.UnknownTypeResolver.None;
    }
}
public record struct ReferenceType(IType inner) : IType;
