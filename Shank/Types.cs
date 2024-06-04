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
    public static Range DefaultInteger()
    {
        return new Range(long.MinValue, float.MaxValue);
    }
    public static Range DefaultSmallInteger()
    {
        // since this is just for arrays and strings should it be unsigned
        return new Range(int.MinValue, float.MaxValue);
    }
    public static Range DefaultCharacter()
    {
        // since this is just for characters should it be unsigned
        return new Range(byte.MinValue, float.MaxValue);
    }
}
public interface IRangeType : IType // this is a bit more specific than a plain IType in that besides for being a type it must also be able to use type limits (the range from before)
{
    public Range Range { get; set; }
}
public struct BooleanType : IType;

public struct StringType(Range range) : IRangeType
{
    public StringType() : this(Range.DefaultSmallInteger()) {}
    

    public Range Range { get; set; } = range;
}



public struct RealType(Range range) : IRangeType
{
    public RealType(): this(Range.DefaultFloat())
    {
        
    }
    public Range Range { get; set; } = range;
}

public struct IntegerType(Range range) : IRangeType
{
    public IntegerType() : this(Range.DefaultInteger())
    {
        
    }
    public Range Range { get; set; } = range;
}

public struct CharacterType(Range range) : IRangeType
{

    public CharacterType() : this(Range.DefaultCharacter())
    {
        
    }
    public Range Range { get; set; } = range;
}

public record struct EnumType(string Name, List<string> Variants) : IType; // enums are just a list of variants

public record struct RecordType(string Name, Dictionary<string, IType> Fields, List<string> Generics) : IType; // records need to keep the types of their members along with any generics they declare

public record struct ArrayType(IType Inner, Range Range ) : IRangeType // arrays have only one inner type
{
    public ArrayType(IType inner): this(inner, Range.DefaultSmallInteger())
    {
        
    }
    public Range Range { get; set; } = Range;
}

public record struct UnknownType(string TypeName, List<IType> TypeParameters) : IType // unknown types are those types that we have not found their proper definition during semantic analysis yet
// they also need to keep and generics they instiate like Int, String in HashMap Int, String
{
    public UnknownType(string TypeName) : this(TypeName, [])
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
public record struct ReferenceType(IType Inner) : IType;
