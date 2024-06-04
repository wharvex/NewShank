namespace Shank;

public interface IType; // our marker interface anything that implements is known to represent a shank type

public struct
    Range // the type that represents a type range in shank (from .. to ..), as ranges on types are part of the types
    (float from, float to)
{
    public float From { get; set; } = from;
    public float To { get; set; } = to;

    public static Range DefaultFloat()
    {
        return new Range(float.MinValue, float.MaxValue);
    }
}
public interface IRangeType : IType // this is a bit more specific than a plain IType in that besides for being a type it must also be able to use type limits (the range from before)
{
    public Range Range { get; set; }
}
public struct BooleanType : IType;

public struct StringType(Range range) : IRangeType
{
    public StringType() : this(Range.DefaultFloat()) {}
    

    public Range Range { get; set; } = range;
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

public record struct EnumType(string Name, List<String> Variants) : IType; // enums are just a list of variants

public record struct RecordType(string Name, Dictionary<String, IType> Fields, List<string> Generics) : IType; // records need to keep the types of their members along with any generics they declare

public record struct ArrayType(IType Inner, Range? Range = null) : IRangeType // arrays have only one inner type
{
    public Range? Range { get; set; } = Range;
}

public record struct UnknownType(String TypeName, List<IType> TypeParameters) : IType // unknown types are those types that we have not found their proper definition during semantic analysis yet
// they also need to keep and generics they instiate like Int, String in HashMap Int, String
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
