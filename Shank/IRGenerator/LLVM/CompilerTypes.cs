using LLVMSharp.Interop;

namespace Shank.IRGenerator;

// ReSharper disable once InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
public interface LLVMType
#pragma warning restore IDE1006 // Naming Styles
{
    public LLVMTypeRef TypeRef { get; }
    public LLVMValue IntoValue(LLVMValueRef llvmValue, bool mutable);
    public static LLVMTypeRef BooleanType { get; } = LLVMTypeRef.Int1;
    public static LLVMTypeRef CharacterType { get; } = LLVMTypeRef.Int8;
    public static LLVMTypeRef IntegerType { get; } = LLVMTypeRef.Int64;
    public static LLVMTypeRef RealType { get; } = LLVMTypeRef.Double;

    // public int line_number = 0;

    public static LLVMTypeRef StringType { get; } =
        LLVMTypeRef.CreateStruct(
            [LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), LLVMTypeRef.Int32,],
            false
        );
}

public readonly record struct LLVMBooleanType : LLVMType
{
    public LLVMTypeRef TypeRef => LLVMType.BooleanType;

    public LLVMValue IntoValue(LLVMValueRef llvmValue, bool mutable) =>
        new LLVMBoolean(llvmValue, mutable);
}

public readonly record struct LLVMCharacterType : LLVMType
{
    public LLVMTypeRef TypeRef => LLVMType.CharacterType;

    public LLVMValue IntoValue(LLVMValueRef llvmValue, bool mutable) =>
        new LLVMCharacter(llvmValue, mutable);
}

public readonly record struct LLVMIntegerType : LLVMType
{
    public LLVMTypeRef TypeRef => LLVMType.IntegerType;

    public LLVMValue IntoValue(LLVMValueRef llvmValue, bool mutable) =>
        new LLVMInteger(llvmValue, mutable);
}

public readonly record struct LLVMRealType : LLVMType
{
    public LLVMTypeRef TypeRef => LLVMType.RealType;

    public LLVMValue IntoValue(LLVMValueRef llvmValue, bool mutable) =>
        new LLVMReal(llvmValue, mutable);
}

public readonly record struct LLVMStringType : LLVMType
{
    public LLVMTypeRef TypeRef => LLVMType.StringType;

    public LLVMValue IntoValue(LLVMValueRef llvmValue, bool mutable) =>
        new LLVMString(llvmValue, mutable);
}

public readonly record struct LLVMArrayType(LLVMType Inner, Range Range) : LLVMInnerReferenceType
{
    public LLVMTypeRef TypeRef =>
        LLVMTypeRef.CreateArray(Inner.TypeRef, (uint)(Range.To - Range.From + 1));

    public LLVMValue IntoValue(LLVMValueRef llvmValue, bool mutable) =>
        new LLVMArray(llvmValue, mutable, this);
}

public readonly record struct LLVMEnumType(string Name, List<string> Variants) : LLVMType
{
    public LLVMTypeRef TypeRef { get; } = LLVMTypeRef.Int32;

    public int GetVariantIndex(string variant) => Variants.IndexOf(variant);

    public LLVMValue IntoValue(LLVMValueRef llvmValue, bool mutable) =>
        new LLVMEnum(llvmValue, mutable, this);
}

public class LLVMStructType(string name, LLVMTypeRef llvmTypeRef) : LLVMInnerReferenceType
{
    public int GetMemberIndex(string member) => Members.Keys.ToList().IndexOf(member);

    public string Name { get; } = name;
    public Dictionary<string, LLVMType> Members { get; set; }
    public LLVMTypeRef TypeRef { get; } = llvmTypeRef;

    public LLVMValue IntoValue(LLVMValueRef llvmValue, bool mutable) =>
        new LLVMStruct(llvmValue, mutable, this);
}

#pragma warning disable IDE1006 // Naming Styles
public interface LLVMInnerReferenceType : LLVMType { }
#pragma warning restore IDE1006 // Naming Styles
public readonly record struct LLVMReferenceType(LLVMInnerReferenceType Inner) : LLVMType
{
    public LLVMTypeRef TypeRef { get; } =
        LLVMTypeRef.CreateStruct(
            [LLVMTypeRef.CreatePointer(Inner.TypeRef, 0), LLVMTypeRef.Int32, LLVMTypeRef.Int1,],
            false
        );

    public LLVMValue IntoValue(LLVMValueRef llvmValue, bool mutable) =>
        new LLVMReference(llvmValue, mutable, this);
}
