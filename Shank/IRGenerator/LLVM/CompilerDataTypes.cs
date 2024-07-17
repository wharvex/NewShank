using LLVMSharp.Interop;

namespace Shank.IRGenerator;

/// <summary>
/// container for Variables (we need a type and reference)
/// </summary>
public abstract class LLVMValue(LLVMValueRef valueRef, bool isMutable, LLVMType typeRef)
{
    public LLVMValueRef ValueRef { get; set; } = valueRef;

    // the type of the value, does not include any pointers, you can know if its needs a pointer by looking at the isMutable field
    public LLVMType TypeRef { get; } = typeRef;

    public bool IsMutable { get; } = isMutable;
}

public class LLVMInteger(LLVMValueRef valueRef, bool isMutable)
    : LLVMValue(valueRef, isMutable, new LLVMIntegerType())
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable) =>
        new LLVMInteger(valueRef, isMutable);
}

public class LLVMBoolean(LLVMValueRef valueRef, bool isMutable)
    : LLVMValue(valueRef, isMutable, new LLVMBooleanType())
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable) =>
        new LLVMBoolean(valueRef, isMutable);
}

public class LLVMReal(LLVMValueRef valueRef, bool isMutable)
    : LLVMValue(valueRef, isMutable, new LLVMRealType())
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable) =>
        new LLVMReal(valueRef, isMutable);
}

public class LLVMString(LLVMValueRef valueRef, bool isMutable)
    : LLVMValue(valueRef, isMutable, new LLVMStringType())
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable) =>
        new LLVMString(valueRef, isMutable);
}

public class LLVMArray(LLVMValueRef valueRef, bool isMutable, LLVMArrayType typeRef)
    : LLVMValue(valueRef, isMutable, typeRef)
{
    public LLVMType Inner() => typeRef.Inner;

    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMArrayType type) =>
        new LLVMArray(valueRef, isMutable, type);
}

public class LLVMCharacter(LLVMValueRef valueRef, bool isMutable)
    : LLVMValue(valueRef, isMutable, new LLVMCharacterType())
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable) =>
        new LLVMCharacter(valueRef, isMutable);
}

public class LLVMStruct(LLVMValueRef valueRef, bool isMutable, LLVMStructType typeRef)
    : LLVMValue(valueRef, isMutable, typeRef)
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMStructType type) =>
        new LLVMStruct(valueRef, isMutable, type);

    public int Access(string name)
    {
        return typeRef.GetMemberIndex(name);
    }

    public LLVMType GetTypeOf(string name)
    {
        return typeRef.Members[name];
    }
}

public class LLVMReference(LLVMValueRef valueRef, bool isMutable, LLVMReferenceType typeRef)
    : LLVMValue(valueRef, isMutable, typeRef)
{
    public LLVMReferenceType TypeOf { get; } = typeRef;

    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMReferenceType type) =>
        new LLVMReference(valueRef, isMutable, type);
}

public class LLVMEnum(LLVMValueRef variant, bool isMutable, LLVMEnumType llvmType)
    : LLVMValue(variant, isMutable, llvmType)
{
    public LLVMEnum(string variant, bool isMutable, LLVMEnumType llvmType)
        : this(
            LLVMValueRef.CreateConstInt(
                LLVMTypeRef.Int32,
                (ulong)llvmType.GetVariantIndex(variant)
            ),
            isMutable,
            llvmType
        ) { }

    public List<string> Variants => llvmType.Variants;

    public static LLVMValue New(string variant, bool isMutable, LLVMEnumType llvmType) =>
        new LLVMEnum(variant, isMutable, llvmType);
}
