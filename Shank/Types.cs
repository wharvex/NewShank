using LLVMSharp;
using Shank.ASTNodes;

namespace Shank;

// ReSharper disable once InconsistentNaming
public interface Type // our marker interface anything that implements is known to represent a shank type
{
    Type Instantiate(Dictionary<string, Type> instantiatedGenerics);
}


public record struct Range // the type that represents a type range in shank (from .. to ..), as ranges on types are part of the types
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

// ReSharper disable once InconsistentNaming
public interface RangeType : Type // this is a bit more specific than a plain IType in that besides for being a type it must also be able to use type limits (the range from before)
{
    // TODO: ranges should not be part of type equality as its not this range == the other range
    // its more this range in that range, so we do it seperatly
    public Range Range { get; set; }
}

public struct BooleanType : Type
{
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;
}

public record struct StringType(Range Range) : RangeType
{
    public bool Equals(StringType other)
    {
        return true; // we do range checking seperatly as we do not know which is the one with more important range
    }
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;
    public override readonly int GetHashCode()
    {
        return 0;
    }

    public StringType()
        : this(Range.DefaultSmallInteger()) { }
}

public record struct RealType(Range Range) : RangeType
{
    public bool Equals(RealType other)
    {
        return true; // we do range checking seperatly as we do not know which is the one with more important range
    }
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;
    public override readonly int GetHashCode()
    {
        return 0;
    }

    public RealType()
        : this(Range.DefaultFloat()) { }
}

public record struct IntegerType(Range Range) : RangeType
{
    public bool Equals(IntegerType other)
    {
        return true; // we do range checking seperatly as we do not know which is the one with more important range
    }
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;
    public override readonly int GetHashCode()
    {
        return 0;
    }

    public IntegerType()
        : this(Range.DefaultInteger()) { }
}

public record struct CharacterType(Range Range) : RangeType
{
    public bool Equals(CharacterType other)
    {
        return true; // we do range checking seperatly as we do not know which is the one with more important range
    }

    public override readonly int GetHashCode()
    {
        return 0;
    }

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public CharacterType()
        : this(Range.DefaultCharacter()) { }
}

public class EnumType(string name, List<string> variants) : Type
{
    public string Name { get; } = name;
    public List<string> Variants { get; } = variants;

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;
} // enums are just a list of variants

public class RecordType(string name, Dictionary<string, Type> fields, List<string> generics) : Type
{
    public List<string> Generics { get; set; } = generics;

    public Dictionary<string, Type> Fields { get; set; } = fields;

    public string Name { get; } = name;
    // we don't instantiate records directly, rather we do that indirectly from InstantiatedType
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;
    internal Type? GetMember(string name, Dictionary<string, Type> instantiatedGenerics)
    {
        var member = fields.GetValueOrDefault(Name);
        return member.Instantiate(instantiatedGenerics);

    }
} // records need to keep the types of their members along with any generics they declare

public record struct ArrayType(Type Inner, Range Range) : RangeType // arrays have only one inner type
{
    public bool Equals(ArrayType other)
    {
        return other.Inner.Equals(Inner);
    }

    public override readonly int GetHashCode()
    {
        return Inner.GetHashCode();
    }
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => new ArrayType(Inner.Instantiate(instantiatedGenerics));
    public ArrayType(Type inner)
        : this(inner, Range.DefaultSmallInteger()) { }
}

public readonly record struct UnknownType(string TypeName, List<Type> TypeParameters) : Type // unknown types are those types that we have not found their proper definition during semantic analysis yet
// they also need to keep and generics they instiate like Int, String in HashMap Int, String
{
    public UnknownType(string TypeName)
        : this(TypeName, []) { }

    // even though record struct implement equal they do not do the right thing for collections see https://github.com/dotnet/csharplang/discussions/5767
    // what does equality even mean here?
    public bool Equals(UnknownType other) =>
        TypeName == other.TypeName && TypeParameters.SequenceEqual(other.TypeParameters);

    public override int GetHashCode() => HashCode.Combine(TypeName, TypeParameters);

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

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

public record struct GenericType(string Name) : Type
{
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => instantiatedGenerics.GetValueOrDefault(Name, this);
}

// constraints: inner can only be record type or generic type
public record struct InstantiatedType(RecordType Inner, Dictionary<string, Type> InstantiatedGenerics) : Type
{
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => new InstantiatedType(Inner, InstantiatedGenerics.Select(tpair => (tpair.Key, tpair.Value.Instantiate(instantiatedGenerics))).ToDictionary());

    Type? GetMember(string name) => Inner.GetMember(name, InstantiatedGenerics);

    public override string ToString()
    {
        return $"{Inner}<{String.Join(",", InstantiatedGenerics.Select(tpair => $"{tpair.Key}: {tpair.Value}"))}>";
    }
}
public record struct ReferenceType(Type Inner) : Type
{
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => new ReferenceType(Inner.Instantiate(instantiatedGenerics));
}

