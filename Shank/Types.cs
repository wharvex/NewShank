using Shank.ASTNodes;

namespace Shank;

public interface IType; // our marker interface anything that implements is known to represent a shank type

public record struct 
    Range // the type that represents a type range in shank (from .. to ..), as ranges on types are part of the types
    (float From, float To)
{
    public static Range DefaultFloat()
    {
        return new Range(float.MinValue, float.MaxValue);
    }
    public static Range DefaultInteger()
    {
        return new Range(long.MinValue, long.MaxValue);
    }
    public static Range DefaultSmallInteger()
    {
        // since this is just for arrays and strings should it be unsigned
        return new Range(uint.MinValue, uint.MaxValue);
    }
    public static Range DefaultCharacter()
    {
        // since this is just for characters should it be unsigned
        return new Range(byte.MinValue, byte.MaxValue);
    }
}
public interface IRangeType : IType // this is a bit more specific than a plain IType in that besides for being a type it must also be able to use type limits (the range from before)
{
    // TODO: ranges should not be part of type equality as its not this range == the other range
    // its more this range in that range, so we do it seperatly
    public Range Range { get; set; }
}
public struct BooleanType : IType;

public  record struct  StringType(Range Range) : IRangeType
{
    public StringType() : this( Range.DefaultSmallInteger()) {}
}

public record struct RealType(Range Range) : IRangeType
{
    public RealType() : this(Range.DefaultFloat()) {}
}

public record struct IntegerType(Range Range) : IRangeType
{
    public IntegerType() : this(Range.DefaultInteger()) {}
}

public record struct CharacterType(Range Range) : IRangeType
{
    public CharacterType() : this(Range.DefaultCharacter()) {}
}

public class EnumType(string name, List<string> variants) : IType
{
    public string Name { get; } = name;
    public List<string> Variants { get; } = variants;
} // enums are just a list of variants

public  class RecordType(string name, Dictionary<string, IType> fields, List<string> generics) : IType
{
    public List<string> Generics
    {
        get;
        set;
    } = generics;

    public Dictionary<string, IType> Fields
    {
        get;
        set ;
    } = fields;

    public string Name
    {
        get;
    } = name;

} // records need to keep the types of their members along with any generics they declare

public record struct ArrayType(IType Inner, Range Range ) : IRangeType // arrays have only one inner type
{
    public ArrayType(IType inner): this(inner, Range.DefaultSmallInteger()) {}
}

public readonly record struct UnknownType(string TypeName, List<IType> TypeParameters) : IType // unknown types are those types that we have not found their proper definition during semantic analysis yet
// they also need to keep and generics they instiate like Int, String in HashMap Int, String
{
    public UnknownType(string TypeName) : this(TypeName, []) {}

    // even though record struct implement equal they do not do the right thing for collections see https://github.com/dotnet/csharplang/discussions/5767
    // what does equality even mean here?
    public bool Equals(UnknownType other) => TypeName == other.TypeName && TypeParameters.SequenceEqual(other.TypeParameters);

    public override int GetHashCode() => HashCode.Combine(TypeName, TypeParameters);

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
