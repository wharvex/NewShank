using Shank.ASTNodes;

namespace Shank;

// ReSharper disable once InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
public interface Type // our marker interface anything that implements is known to represent a shank type
#pragma warning restore IDE1006 // Naming Styles
{
    // when have a list of generic types and what types replace them we need a function that does the replacement
    Type Instantiate(Dictionary<string, Type> instantiatedGenerics);
    public string ToString();
}

public readonly record struct Range // the type that represents a type range in shank (from … to …), as ranges on types are part of the types
(float From, float To)
{
    public static Range DefaultFloat => new(float.MinValue, float.MaxValue);

    public static Range DefaultInteger => new(long.MinValue, long.MaxValue);

    // since this is just for arrays and strings should it be unsigned
    public static Range DefaultSmallInteger => new(uint.MinValue, uint.MaxValue);

    // since this is just for characters should it be unsigned
    public static Range DefaultCharacter => new(byte.MinValue, byte.MaxValue);

    public override string ToString() => $"from {From} to {To}";
}

// ReSharper disable once InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
public interface RangeType : Type // this is a bit more specific than a plain IType in that besides for being a type it must also be able to use type limits (the range from before)
#pragma warning restore IDE1006 // Naming Styles
{
    // TODO: ranges should not be part of type equality as its not this range == the other range
    // its more this range in that range, so we do it separately
    public Range Range { get; init; }
    public static abstract Range DefaultRange { get; }
}

public static class RangeTypeExt
{
    public static Range DefaultRange<T>(this T _)
        where T : RangeType => T.DefaultRange;

    // get the string version of the range
    // if the range is the default one we don't do anything
    public static string RangeString<T>(this T range)
        where T : RangeType => T.DefaultRange == range.Range ? "" : $" {range.Range}";
}

public struct BooleanType : Type
{
    public readonly Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public override readonly string ToString() => "boolean";
}

public readonly record struct StringType(Range Range) : RangeType
{
    public static Range DefaultRange => Range.DefaultSmallInteger;

    public bool Equals(StringType other) => true; // we do range checking separately as we do not know which is the one with more important range

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public override int GetHashCode() => 0;

    public StringType()
        : this(DefaultRange) { }

    public override string ToString() => $"string{this.RangeString()}";
}

public readonly record struct RealType(Range Range) : RangeType
{
    public static Range DefaultRange => Range.DefaultFloat;

    public bool Equals(RealType other) => true; // we do range checking separately as we do not know which is the one with more important range

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public override int GetHashCode() => 0;

    public RealType()
        : this(Range.DefaultFloat) { }

    public override string ToString() => $"real{this.RangeString()}";
}

public readonly record struct IntegerType(Range Range) : RangeType
{
    public static Range DefaultRange => Range.DefaultInteger;

    public bool Equals(IntegerType other) => true; // we do range checking separately as we do not know which is the one with more important range

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public override int GetHashCode() => 0;

    public IntegerType()
        : this(DefaultRange) { }

    public override string ToString() => $"integer{this.RangeString()}";
}

public readonly record struct CharacterType(Range Range) : RangeType
{
    public bool Equals(CharacterType other) => true; // we do range checking separately as we do not know which is the one with more important range

    public override int GetHashCode() => 0;

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public CharacterType()
        : this(DefaultRange) { }

    public static Range DefaultRange => Range.DefaultCharacter;

    public override string ToString() => $"character{this.RangeString()}";
}

public class EnumType(string name, List<string> variants) : Type
{
    public string Name { get; } = name;
    public List<string> Variants { get; } = variants;

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public override string ToString() => $"{Name} [{string.Join(", ", Variants)}]";
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
        var member = Fields.GetValueOrDefault(name);
        return member?.Instantiate(instantiatedGenerics);
    }

    // TODO: should this print newlines for each member as it does not get used by any other Type.ToString
    public override string ToString() =>
        $"{Name} generic {string.Join(", ", Generics)} [{string.Join(", ", Fields.Select(typePair => $"{typePair.Key}: {typePair.Value}"))}]";
} // records need to keep the types of their members along with any generics they declare

public readonly record struct ArrayType(Type Inner, Range Range) : RangeType // arrays have only one inner type
{
    public bool Equals(ArrayType other)
    {
        return other.Inner.Equals(Inner);
    }

    public override int GetHashCode() => Inner.GetHashCode();

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) =>
        new ArrayType(Inner.Instantiate(instantiatedGenerics), Range);

    public ArrayType(Type inner, Range? range)
        : this(inner, range ?? DefaultRange) { }

    public static Range DefaultRange => Range.DefaultSmallInteger;

    public override string ToString() => $"array {this.RangeString()} of {Inner}";
}

public readonly record struct UnknownType(string TypeName, List<Type> TypeParameters) : Type // unknown types are those types that we have not found their proper definition during semantic analysis, yet
// they also need to keep and generics they instantiate like Int, String in HashMap Int, String
{
    public UnknownType(string TypeName)
        : this(TypeName, []) { }

    // even though record struct implement equal they do not do the right thing for collections see https://github.com/dotnet/csharplang/discussions/5767
    // what does equality even mean here?
    public bool Equals(UnknownType other) =>
        TypeName == other.TypeName && TypeParameters.SequenceEqual(other.TypeParameters);

    public override int GetHashCode() => HashCode.Combine(TypeName, TypeParameters);

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public VariableDeclarationNode.UnknownTypeResolver ResolveUnknownType(ModuleNode parentModule)
    {
        if (
            parentModule.getEnums().ContainsKey(TypeName)
            && parentModule.Records.ContainsKey(TypeName)
        )
        {
            return VariableDeclarationNode.UnknownTypeResolver.Multiple;
        }

        if (parentModule.getEnums().ContainsKey(TypeName))
        {
            return VariableDeclarationNode.UnknownTypeResolver.Enum;
        }

        return parentModule.Records.ContainsKey(TypeName)
            ? VariableDeclarationNode.UnknownTypeResolver.Record
            : VariableDeclarationNode.UnknownTypeResolver.None;
    }

    public override string ToString() => $"{TypeName}({string.Join(", ", TypeParameters)})";
}

// Only used in semantic analysis and later
// represents a generic type, since we don't use UnknownType is semantic analysis
// also generics cannot have type parameters
public readonly record struct GenericType(string Name) : Type
{
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) =>
        instantiatedGenerics.GetValueOrDefault(Name, this);

    public override string ToString() => Name;
}

// Only used in semantic analysis and later
// what is this and why do we need it?
// record types define the structure of the record, but each time you use the record in your code (in a variable declaration, parameter, or even another record)
// you give and generics that may be defined new types (these could another generic, a record, float, ...)
public readonly record struct InstantiatedType(
    RecordType Inner,
    Dictionary<string, Type> InstantiatedGenerics
) : Type
{
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) =>
        this with
        {
            InstantiatedGenerics = InstantiatedGenerics
                .Select(
                    typePair => (typePair.Key, typePair.Value.Instantiate(instantiatedGenerics))
                )
                .ToDictionary()
        };

    public bool Equals(InstantiatedType other) =>
        Inner.Equals(other.Inner) && InstantiatedGenerics.SequenceEqual(other.InstantiatedGenerics);

    public override int GetHashCode() => HashCode.Combine(Inner, InstantiatedGenerics);

    public Type? GetMember(string name) => Inner.GetMember(name, InstantiatedGenerics);

    public override string ToString() =>
        $"{Inner}({string.Join(", ", InstantiatedGenerics.Select(typePair => $"{typePair.Value}"))})";
}

public readonly record struct ReferenceType(Type Inner) : Type
{
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics)
    {
        var instantiate = Inner.Instantiate(instantiatedGenerics);
        return new ReferenceType(
            instantiate is InstantiatedType or GenericType
                ? instantiate
                : throw new SemanticErrorException(
                    $"tried to use refersTo (dynamic memory management) on a non record type ",
                    null
                )
        );
    }

    public override string ToString() => $"refersTo {Inner}";
}
