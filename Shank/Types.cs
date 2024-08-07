using Shank.ASTNodes;
using Shank.AstVisitorsTim;

namespace Shank;

// ReSharper disable once InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
public interface Type // our marker interface anything that implements is known to represent a shank type
#pragma warning restore IDE1006 // Naming Styles
{
    // when have a list of generic types and what types replace them we need a function that does the replacement
    Type Instantiate(Dictionary<string, Type> instantiatedGenerics);
    public T Accept<T>(ITypeVisitor<T> v);
    public string ToString();
    public void Accept(InnerTypeGettingVisitor visitor) => visitor.Visit(this);
    public void Accept(IAstTypeVisitor visitor) => visitor.Visit(this);
    public static Type Default => new DefaultType();
}

// I (Tim) created this so I wouldn't need to make `Expression.Type` nullable.
// It causes issues with Json.NET (i.e. serializing the AST) when `Expression.Type` is nullable
// (more specifically when it actually is null sometimes).
public readonly struct DefaultType : Type
{
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

    public override string ToString() => "default";
}

/// <summary>
///     <para>
///         Record <c>Range</c> is the type that represents a type range in shank (from … to …), as ranges on types are part of the types
///     </para>
/// </summary>
/// <param name="From">Lower bound of the range</param>
/// <param name="To">Upper bound of the range</param>
public readonly record struct Range // the type that represents a type range in shank (from … to …), as ranges on types are part of the types
(float From, float To)
{
    public static Range DefaultFloat => new(float.MinValue, float.MaxValue);

    public static Range DefaultInteger => new(long.MinValue, long.MaxValue);

    // since this is just for arrays and strings should it be unsigned
    public static Range DefaultSmallInteger => new(uint.MinValue, uint.MaxValue);

    // public static Range DefaultStringRange => new(1, uint.MaxValue);

    // since this is just for characters should it be unsigned
    public static Range DefaultCharacter => new(byte.MinValue, byte.MaxValue);

    public float Length { get; } = To - From;

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
    // each method here is overloaded with taking RangeType, and T where T is RangeType
    // we need both because in the second case it can be done at compile time
    // but in the first case since we do not know the specific range type we have to switch on it to the default range which is only available in on the type
    public static Range DefaultRange(this RangeType range) =>
        range switch
        {
            ArrayType => ArrayType.DefaultRange,
            CharacterType => CharacterType.DefaultRange,
            IntegerType => IntegerType.DefaultRange,
            RealType => RealType.DefaultRange,
            StringType => StringType.DefaultRange,
            _ => throw new ArgumentOutOfRangeException(nameof(range))
        };

    public static Range DefaultRange<T>(this T _)
        where T : RangeType => T.DefaultRange;

    // get the string version of the range
    // if the range is the default one we don't do anything
    public static string RangeString(this RangeType range) =>
        IsDefaultRange(range) ? "" : $" {range.Range}";

    public static string RangeString<T>(this T range)
        where T : RangeType => IsDefaultRange(range) ? "" : $" {range.Range}";

    public static bool IsDefaultRange<T>(this T range)
        where T : RangeType => T.DefaultRange == range.Range;

    public static bool IsDefaultRange(this RangeType range) => range.DefaultRange() == range.Range;
}

public readonly struct BooleanType : Type
{
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

    public override string ToString() => "boolean";
}

public readonly record struct StringType(Range Range) : RangeType
{
    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

    public static Range DefaultRange => Range.DefaultSmallInteger;

    public bool Equals(StringType other) => true; // we do range checking separately as we do not know which is the one with more important range

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public override int GetHashCode() => 0;

    public StringType()
        : this(DefaultRange) { }

    public override string ToString() => $"string{this.RangeString()}";
}

/// <summary>
///      The stucture <c>RealType</c> creates an object holding the range of a real number
/// </summary>
/// <param name="Range">The expected range of the data type</param>
public readonly record struct RealType(Range Range) : RangeType
{
    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

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
    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

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
    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

    public bool Equals(CharacterType other) => true; // we do range checking separately as we do not know which is the one with more important range

    public override int GetHashCode() => 0;

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public CharacterType()
        : this(DefaultRange) { }

    public static Range DefaultRange => Range.DefaultCharacter;

    public override string ToString() => $"character{this.RangeString()}";
}

public class EnumType(string name, string moduleName, List<string> variants) : Type
{
    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

    public ModuleIndex MonomorphizedIndex => new(new NamedIndex(Name), ModuleName);

    public string Name { get; } = name;
    public string ModuleName { get; } = moduleName;
    public List<string> Variants { get; } = variants;

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    public int GetElementNum(string name)
    {
        foreach (var (param, index) in Variants.Select((param, index) => (param, index)))
        {
            if (param == name)
                return index;
        }

        return -1;
    }

    public string GetElemenTvalue(int index)
    {
        if (index < Variants.Count)
            return Variants[index];
        return "error";
    }

    public override string ToString() => $"{Name} [{string.Join(", ", Variants)}]";
} // enums are just a list of variants

public class RecordType(
    string name,
    string moduleName,
    Dictionary<string, Type> fields,
    List<string> generics
) : Type
{
    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

    public List<string> Generics { get; set; } = generics;
    public TypedModuleIndex MonomorphizedIndex { get; set; }
    public Dictionary<string, Type> Fields { get; set; } = fields;

    public string Name { get; } = name;
    public string ModuleName { get; } = moduleName;

    // we don't instantiate records directly, rather we do that indirectly from InstantiatedType
    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) => this;

    internal Type? GetMember(string name, Dictionary<string, Type> instantiatedGenerics)
    {
        var member = Fields.GetValueOrDefault(name);
        return member?.Instantiate(instantiatedGenerics);
    }

    public Type GetMemberSafe(string memberName, Dictionary<string, Type> instGens, ASTNode cause)
    {
        return GetMember(memberName, instGens)
            ?? throw new SemanticErrorException(
                "Only existent members can be accessed. Found: " + memberName,
                cause
            );
    }

    // TODO: should this print newlines for each member as it does not get used by any other Type.ToString
    public override string ToString() =>
        $"{Name}{(Generics.Count == 0 ? "" : $"generic {string.Join(", ", Generics)}")}";
} // records need to keep the types of their members along with any generics they declare

public readonly record struct ArrayType(Type Inner, Range Range) : RangeType // arrays have only one inner type
{
    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

    public void Accept(IAstTypeVisitor visitor) => visitor.Visit(this);

    public void Accept(IArrayTypeVisitor visitor) => visitor.Visit(this);

    // public bool Equals(ArrayType other)
    // {
    //     return other.Inner.Equals(Inner) && other.Range == Range;
    // }

    // public override int GetHashCode() => HashCode.Combine(Inner.GetHashCode(), Range.GetHashCode);

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics) =>
        this with
        {
            Inner = Inner.Instantiate(instantiatedGenerics)
        };

    // public ArrayType(Type inner, Range? range)
    //     : this(inner, range ?? DefaultRange) { }

    // We want to change this back to DefaultSmallInteger once we have better infrastructure in
    // place for verifying ranges with if-statements.
    public static Range DefaultRange => Range.DefaultInteger;

    public override string ToString() => $"array {this.RangeString()} of {Inner}";
}

/// <summary>
///     <para>
///     Structure <c>UnknownType</c>
///     </para>
/// </summary>
/// <param name="TypeName"></param>
/// <param name="TypeParameters"></param>
public readonly record struct UnknownType(string TypeName, List<Type> TypeParameters) : Type // unknown types are those types that we have not found their proper definition during semantic analysis, yet
// they also need to keep and generics they instantiate like Int, String in HashMap Int, String
{
    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

    public UnknownType(string TypeName)
        : this(TypeName, []) { }

    // even though record struct implement equal they do not do the right thing for collections see https://github.com/dotnet/csharplang/discussions/5767
    // what does equality even mean here?
    public bool Equals(UnknownType other) =>
        TypeName == other.TypeName && TypeParameters.SequenceEqual(other.TypeParameters);

    //a hashcode generated based off the name a parameter values of our UnknownType
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

// just because a generic has the same as another generic it does not mean they are the same generic
// one could be from a record and on from a function ...
// to fix this we add information about where the generic came from (function/record, function/record name, module name, and function overload)
#pragma warning disable IDE1006 // Naming Styles
public interface GenericContext;
#pragma warning restore IDE1006 // Naming Styles
public record FunctionGenericContext(string Function, string Module, TypeIndex Overload)
    : GenericContext;

public record RecordGenericContext(string Record, string Module) : GenericContext;

// dummy context is needed for having context for global variables, which do not have generics so this just needed as placeholder and never actually gets used, but some functions require a generic context
public record DummyGenericContext : GenericContext;

// Only used in semantic analysis and later
// represents a generic type, since we don't use UnknownType is semantic analysis
// also generics cannot have type parameters (no HKTs)
public readonly record struct GenericType(string Name, GenericContext Context) : Type
{
    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

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
    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

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

    public Type GetMemberSafe(string name, ASTNode node) =>
        Inner.GetMember(name, InstantiatedGenerics)
        ?? throw new SemanticErrorException(
            "No member `" + name + "' on `" + Inner.Name + "'",
            node
        );

    public void Accept(IInstantiatedTypeVisitor visitor) => visitor.Visit(this);

    public override string ToString() =>
        $"{Inner}{(InstantiatedGenerics.Count == 0 ? "" : $"({string.Join(", ", InstantiatedGenerics.Select(typePair => $"{typePair.Value}"))})")}";
}

public readonly record struct ReferenceType(Type Inner) : Type
{
    public T Accept<T>(ITypeVisitor<T> v) => v.Visit(this);

    public Type Instantiate(Dictionary<string, Type> instantiatedGenerics)
    {
        var instantiate = Inner.Instantiate(instantiatedGenerics);
        return new ReferenceType(
            instantiate is InstantiatedType or ArrayType or GenericType
                ? instantiate
                : throw new SemanticErrorException(
                    $"tried to use refersTo (dynamic memory management) on a non record type "
                        + instantiate.GetType()
                )
        );
    }

    public override string ToString() => $"refersTo {Inner}";
}
