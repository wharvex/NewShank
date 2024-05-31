namespace Shank;

public interface IType;

public struct BooleanType : IType;

public struct StringType : IType;

public struct RealType : IType;

public struct IntegerType : IType;

public struct CharacterType : IType;

public record struct EnumType(List<String> Variants) : IType;

public record struct RecordType(Dictionary<String, IType> Fields, List<string> Generics) : IType;

public record struct ArrayType(IType Inner) : IType;

public record struct UnknownType(String TypeName, List<IType> TypeParameters) : IType
{
    public UnknownType(String TypeName) : this(TypeName, new List<IType>())
    {
    }
}
public record struct ReferenceType(IType inner) : IType;
