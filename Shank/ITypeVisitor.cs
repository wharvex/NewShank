namespace Shank;

public interface ITypeVisitor<out T>
{
    public T Visit(RealType type);
    public T Visit(RecordType type);
    public T Visit(InstantiatedType type);
    public T Visit(ArrayType type);
    public T Visit(EnumType type);
    public T Visit(BooleanType type);
    public T Visit(CharacterType type);
    public T Visit(StringType type);
    public T Visit(IntegerType type);
    public T Visit(ReferenceType type);
    public T Visit(UnknownType type);
    public T Visit(GenericType type);
}