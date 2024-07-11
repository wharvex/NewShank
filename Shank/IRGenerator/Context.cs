using LLVMSharp;
using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.IRGenerator;

// a facade around a llvm value ref to make it more type safe and also to be able to get the actual type of the function as opposed to a function pointer type
// TODO: move all llvm facades to their own file
public class LLVMFunction
{
    public LLVMValueRef Function { get; private set; }
    public LLVMTypeRef ReturnType => TypeOf.ReturnType;
    public LLVMTypeRef TypeOf { get; }

    public LLVMFunction(LLVMModuleRef module, string name, LLVMTypeRef type)
    {
        Function = module.AddFunction(name, type);
        TypeOf = type;
    }

    protected LLVMFunction()
    {
    }

    public LLVMLinkage Linkage
    {
        get => Function.Linkage;
        set => Function = Function with { Linkage = value };
    }

    public LLVMValueRef GetParam(uint index) => Function.GetParam(index);

    public LLVMBasicBlockRef AppendBasicBlock(string entry) => Function.AppendBasicBlock(entry);
}

// represents a function accessible to a shank user
// we need to track each parameter mutability
public class LLVMShankFunction(
    LLVMModuleRef module,
    string name,
    LLVMTypeRef type,
    IEnumerable<bool> arguementMutability
) : LLVMFunction(module, name, type)
{
    public IEnumerable<bool> ArguementMutability { get; } = arguementMutability;
}

// this class adds extension methods to llvm modules to add functions, but return them to us as our llvm function facade
public static class LLVMAddFunctionExtension
{
    public static LLVMFunction addFunction(this LLVMModuleRef module, string name, LLVMTypeRef type)
    {
        return new LLVMFunction(module, name, type);
    }

    public static LLVMShankFunction addFunction(
        this LLVMModuleRef module,
        string name,
        LLVMTypeRef type,
        IEnumerable<bool> arguementMutability
    )
    {
        return new LLVMShankFunction(module, name, type, arguementMutability);
    }
}

/// <summary>
/// container for Varaibles (we need a type and refrence)
/// </summary>
public abstract class LLVMValue(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef typeRef)
{
    // public VariableDeclarationNode Node { get; set; } = node;
    public LLVMValueRef ValueRef { get; } = valueRef;

    // the type of the value, does not include any pointers, you can know if its needs a pointer by looking at the isMutable field
    public LLVMTypeRef TypeRef { get; } = typeRef;

    public bool IsMutable { get; } = isMutable;
}

public class LLVMInteger(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef typeRef)
    : LLVMValue(valueRef, isMutable, typeRef)
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef type) =>
        new LLVMInteger(valueRef, isMutable, type);
}

public class LLVMBoolean(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef typeRef)
    : LLVMValue(valueRef, isMutable, typeRef)
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef type) =>
        new LLVMBoolean(valueRef, isMutable, type);
}

public class LLVMReal(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef typeRef)
    : LLVMValue(valueRef, isMutable, typeRef)
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef type) =>
        new LLVMReal(valueRef, isMutable, type);
}

public class LLVMString(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef typeRef)
    : LLVMValue(valueRef, isMutable, typeRef)
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef type) =>
        new LLVMString(valueRef, isMutable, type);
}

public class LLVMArray(LLVMValueRef valueRef, bool isMutable, LLVMArrayType typeRef)
    : LLVMValue(valueRef, isMutable, typeRef.LlvmTypeRef)
{
    public Type Inner() => typeRef.Inner();

    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMArrayType type) =>
        new LLVMArray(valueRef, isMutable, type);
}

public class LLVMCharacter(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef typeRef)
    : LLVMValue(valueRef, isMutable, typeRef)
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef type) =>
        new LLVMCharacter(valueRef, isMutable, type);
}

public class LLVMEnum(EnumType enumType)
    : LLVMValue(LLVMValueRef.CreateConstNull(LLVMTypeRef.Int64), true, LLVMTypeRef.Int64)
{
    public EnumType EnumType { get; set; } = enumType;

    public static LLVMValue New(EnumType enumType) => new LLVMEnum(enumType);
}

public class LLVMStruct(LLVMValueRef valueRef, bool isMutable, LLVMStructType typeRef)
    : LLVMValue(valueRef, isMutable, typeRef.LlvmTypeRef)
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMStructType type) =>
        new LLVMStruct(valueRef, isMutable, type);

    public int Access(string name)
    {
        return typeRef.GetMemberIndex(name);
    }

    public Type GetTypeOf(string name)
    {
        return ((RecordType)typeRef.Type).Fields[name];
    }
}

public class LLVMReference(LLVMValueRef valueRef, bool isMutable, LLVMReferenceType typeRef)
    : LLVMValue(valueRef, isMutable, typeRef.LlvmTypeRef)
{
    public LLVMReferenceType TypeOf { get; } = typeRef;

    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMReferenceType type) =>
        new LLVMReference(valueRef, isMutable, type);
}

public abstract class LLVMType(Type type, LLVMTypeRef llvmtype)
{
    public Type Type { get; set; } = type;
    public LLVMTypeRef LlvmTypeRef { get; set; } = llvmtype;
}

public class LLVMStructType(RecordType type, LLVMTypeRef llvmtype, List<string> members)
    : LLVMType(type, llvmtype)
{
    public int GetMemberIndex(string member) => members.IndexOf(member);
}

public class LLVMReferenceType(LLVMStructType inner, LLVMTypeRef typeRef)
    : LLVMType(inner.Type, typeRef)
{
    public LLVMStructType Inner { get; } = inner;
}

public class LLVMArrayType(ArrayType type, LLVMTypeRef llvmtype) : LLVMType(type, llvmtype)
{
    public Type Inner() => type.Inner;
}

public class LLVMEnumType(EnumType type, LLVMTypeRef llvmtype) : LLVMType(type, llvmtype)
{
}

// Functions "linked" from c that are used in code generated by the compiler
public struct CFuntions
{
    // adds the c functions to the module
    public CFuntions(LLVMModuleRef llvmModule)
    {
        var sizeT = LLVMTypeRef.Int32;
        printf = llvmModule.addFunction(
            "printf",
            LLVMTypeRef.CreateFunction(
                sizeT,
                [LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)],
                true
            )
        );
        printf.Linkage = LLVMLinkage.LLVMExternalLinkage;
        // llvm does not like void pointers, so we most places I've seen use i8* instead
        var voidStar = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);

        memcpy = llvmModule.addFunction(
            "memcpy",
            LLVMTypeRef.CreateFunction(voidStar, [voidStar, voidStar, sizeT])
        );
        memcpy.Linkage = LLVMLinkage.LLVMExternalLinkage;
        malloc = llvmModule.addFunction("malloc", LLVMTypeRef.CreateFunction(voidStar, [sizeT]));
        memcpy.Linkage = LLVMLinkage.LLVMExternalLinkage;
        exit = llvmModule.addFunction(
            "exit",
            LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [LLVMTypeRef.Int32,])
        );
        exit.Linkage = LLVMLinkage.LLVMExternalLinkage;
    }

    //  int printf(const char *restrict format, ...);
    // sometimes llvm can optimize this to a puts
    public LLVMFunction printf { get; }

    // void *memcpy(void dest[restrict .n], const void src[restrict .n], size_t n);
    public LLVMFunction memcpy { get; }

    // void *malloc(size_t n);
    public LLVMFunction malloc { get; }

    // void exit(int exit_code);
    public LLVMFunction exit { get; }
}

public class Context(MonomorphizedProgramNode moduleNode, CFuntions cFuntions)
{
    private readonly MonomorphizedProgramNode moduleNode = moduleNode;

    public LLVMTypeRef StringType = LLVMTypeRef.CreateStruct(
        [
            LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), // content
            // LLVMTypeRef.Int32, // start
            LLVMTypeRef.Int32 // size
        ],
        false
    );

    // public ModuleNode moduleNode { get; set; }
    public CFuntions CFuntions { get; } = cFuntions;
    public LLVMFunction CurrentFunction { get; set; }

    // public Dictionary<string, LLVMShankFunction> BuiltinFunctions { get; } = new();
    public Dictionary<string, LLVMValue> Variables { get; set; } = new();
    public Dictionary<ModuleIndex, LLVMValue> GlobalVariables { get; set; } = new();
    public Dictionary<TypedBuiltinIndex, LLVMShankFunction> BuiltinFunctions { get; set; } = new();
    public Dictionary<TypedModuleIndex, LLVMShankFunction> Functions { get; set; } = new();
    public Dictionary<TypedModuleIndex, LLVMStructType> Records { get; } = [];
    public Dictionary<ModuleIndex, LLVMEnumType> Enums { get; } = [];

    // public void SetCurrentModule(string module) => CurrentModule = Modules[module];

    // public Module CurrentModule { get; private set; }

    // public Dictionary<string, Module> Modules = new();

    /// <summary>
    /// converts shank type to LLVM type
    /// </summary>
    /// <param name="dataType"></param>
    /// <param name="unknownType"></param>
    /// <returns></returns>
    public LLVMTypeRef? GetLLVMTypeFromShankType(Type dataType)
    {
        //broken with structs
        return dataType switch
        {
            IntegerType => LLVMTypeRef.Int64,
            RecordType t => Records[t.MonomorphizedIndex].LlvmTypeRef,
            RealType => LLVMTypeRef.Double,
            Shank.StringType => StringType,
            BooleanType => LLVMTypeRef.Int1,
            CharacterType => LLVMTypeRef.Int8,
            EnumType e => LLVMTypeRef.Int32,
            // if it's a custom type we look it up in the context
            ReferenceType r
                => LLVMTypeRef.CreateStruct(
                    [
                        LLVMTypeRef.CreatePointer(
                            (LLVMTypeRef)GetLLVMTypeFromShankType(r.Inner),
                            0
                        ), // record
                        LLVMTypeRef.Int32, // size (we probably don't need this as we know size at compile time)
                        LLVMTypeRef.Int1 // isSet (isSet is probably not needed, just check record for null)
                    ],
                    false
                ),
            ArrayType a
                => LLVMTypeRef.CreateStruct(
                    [
                        LLVMTypeRef.CreatePointer(
                            (LLVMTypeRef)GetLLVMTypeFromShankType(a.Inner),
                            0
                        ),
                        LLVMTypeRef.Int32, // start
                        LLVMTypeRef.Int32 // size
                    ],
                    false
                ),
            // UnknownType t => CurrentModule.CustomTypes[t.TypeName], // TODO: instiate type parameters if needed
        };
    }

    // given a shank type this gives back the correct constructor for the CDT (compiler date type)
    public Func<LLVMValueRef, bool, LLVMValue>? NewVariable(Type type)
    {
        (Func<LLVMValueRef, bool, LLVMTypeRef, LLVMValue>, LLVMTypeRef)? typeConstructor =
            type switch
            {
                IntegerType => (LLVMInteger.New, LLVMTypeRef.Int64),
                RealType => (LLVMReal.New, LLVMTypeRef.Double),
                RecordType recordType => NewRecordValue(recordType),
                Shank.StringType => (LLVMString.New, StringType),
                UnknownType unknownType => throw new TypeAccessException($"{type} doesnt exist"),
                BooleanType => (LLVMBoolean.New, LLVMTypeRef.Int1),
                CharacterType => (LLVMCharacter.New, LLVMTypeRef.Int8),
                EnumType => (LLVMInteger.New, LLVMTypeRef.Int64),
                // if it's a custom type we look it up in the context
                ReferenceType r
                    => (
                        (value, mutable, type) =>
                            LLVMReference.New(
                                value,
                                mutable,
                                new LLVMReferenceType(
                                    Records[((RecordType)r.Inner).MonomorphizedIndex],
                                    type
                                )
                            ),
                        (LLVMTypeRef)GetLLVMTypeFromShankType(type)
                    ),
                ArrayType arrayType
                    => (
                        (value, mutable, type) =>
                            LLVMArray.New(value, mutable, new LLVMArrayType(arrayType, type)),
                        LLVMTypeRef.CreateStruct(
                            [
                                LLVMTypeRef.CreatePointer(
                                    GetLLVMTypeFromShankType(arrayType.Inner).Value,
                                    0
                                ),
                                LLVMTypeRef.Int32,
                                LLVMTypeRef.Int32
                            ],
                            false
                        )
                    )
            };

        (Func<LLVMValueRef, bool, LLVMTypeRef, LLVMValue>, LLVMTypeRef)? NewRecordValue(
            RecordType recordType
        )
        {
            var type = Records[recordType.MonomorphizedIndex];
            return (
                (Value, mutable, _type) => LLVMStruct.New(Value, mutable, type),
                type.LlvmTypeRef
            );
        }

        return typeConstructor == null
            ? null
            : (value, mutable) =>
                typeConstructor.Value.Item1(value, mutable, typeConstructor.Value.Item2);
    }

    /// <summary>
    /// converts shank type to LLVM type
    /// </summary>
    /// <param name="dataType"></param>
    /// <param name="unknownType"></param>
    /// <returns></returns>
    public LLVMTypeRef? GetLLVMTypeFromShankType(Type type, bool isVar)
    {
        var llvmTypeFromShankType = GetLLVMTypeFromShankType(type);
        return isVar
            ? LLVMTypeRef.CreatePointer((LLVMTypeRef)llvmTypeFromShankType, 0)
            : llvmTypeFromShankType;
    }

    /// <summary>
    /// gets the varaible
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public LLVMValue GetVariable(Index index)
    {
        if (index is ModuleIndex m)
        {
            return GlobalVariables[m];
        }
        else if (index is NamedIndex n)
        {
            return Variables[n.Name];
        }
        else
        {
            throw new Exception($"undefined varname {index}");
        }
    }

    /// <summary>
    /// adds a varaivle to the dictionary
    /// </summary>
    /// <param name="name">var name (the key)</param>
    ///
    /// <param name="valueRef">the LLVMref (needed for assignments)</param>
    /// <param name="type">Data type the varaible is</param>
    /// <param name="isGlobal">IsGlobal</param>
    /// <exception cref="Exception">if it already exists</exception>
    public void AddVariable(Index name, LLVMValue value)
    {
        // if (GlobalVariables.ContainsKey(name) || Variables.ContainsKey(name))
        //     throw new Exception("error");
        // else
        {
            if (name is ModuleIndex m)
            {
                GlobalVariables.Add(m, value);
            }
            else if (name is NamedIndex n)
            {
                Variables.Add(n.Name, value);
            }
        }
    }

    /// <summary>
    /// resets the Varaible dictionary run at the end of a function
    /// </summary>
    public void ResetLocal() => Variables = new();

    public void AddFunction(TypedModuleIndex name, LLVMShankFunction function) =>
        Functions[name] = function;

    public void AddBuiltinFunction(TypedBuiltinIndex name, LLVMShankFunction function) =>
        BuiltinFunctions[name] = function;

    public LLVMShankFunction GetFunction(Index name) =>
        name is TypedModuleIndex m ? Functions[m] : BuiltinFunctions[(TypedBuiltinIndex)name];

    public LLVMType GetCustomType(Index nodeName) =>
        nodeName is TypedModuleIndex m ? Records[m] : Enums[(ModuleIndex)nodeName];

    // public void AddCustomType(string import, LLVMType value) =>
    //     CurrentModule.CustomTypes[import] = value;
}
