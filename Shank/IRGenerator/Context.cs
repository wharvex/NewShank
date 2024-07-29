using LLVMSharp.Interop;

namespace Shank.IRGenerator;

// Functions "linked" from c that are used in code generated by the compiler
public struct CFuntions
{
    // adds the c functions to the module
    public CFuntions(LLVMModuleRef llvmModule)
    {
        var sizeT = LLVMTypeRef.Int32;
        var charStar = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
        var timeT = LLVMTypeRef.Int64;
        printf = llvmModule.addFunction(
            "printf",
            LLVMTypeRef.CreateFunction(sizeT, [charStar], true)
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
        malloc.Linkage = LLVMLinkage.LLVMExternalLinkage;
        exit = llvmModule.addFunction(
            "exit",
            LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [LLVMTypeRef.Int32,])
        );
        exit.Linkage = LLVMLinkage.LLVMExternalLinkage;
        getchar = llvmModule.addFunction(
            "getchar",
            LLVMTypeRef.CreateFunction(LLVMTypeRef.Int8, [])
        );
        getchar.Linkage = LLVMLinkage.LLVMExternalLinkage;
        realloc = llvmModule.addFunction(
            "realloc",
            LLVMTypeRef.CreateFunction(voidStar, [voidStar, sizeT])
        );
        realloc.Linkage = LLVMLinkage.LLVMExternalLinkage;
        memcmp = llvmModule.addFunction(
            "memcmp",
            LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, [charStar, charStar, sizeT])
        );
        memcmp.Linkage = LLVMLinkage.LLVMExternalLinkage;
        memset = llvmModule.addFunction(
            "memset",
            LLVMTypeRef.CreateFunction(voidStar, [voidStar, LLVMTypeRef.Int32, sizeT])
        );
        memset.Linkage = LLVMLinkage.LLVMExternalLinkage;
        time = llvmModule.addFunction(
            "time",
            LLVMTypeRef.CreateFunction(timeT, [LLVMTypeRef.CreatePointer(timeT, 0)])
        );
        time.Linkage = LLVMLinkage.LLVMExternalLinkage;
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

    // int getchar(void)
    public LLVMFunction getchar { get; }

    // void *realloc(void *ptr, size_t size);
    public LLVMFunction realloc { get; }

    // we use memcmp as opposed to str*cmp to not have to deal with null termination
    // int memcmp(const char* lhs, const char* rhs, size_t count );
    public LLVMFunction memcmp { get; }

    // void *memset( void *dest, int ch, size_t count );
    public LLVMFunction memset { get; }

    // time_t time( time_t *arg );
    public LLVMFunction time { get; }
}

public class Context(MonomorphizedProgramNode moduleNode, CFuntions cFuntions, RandomInformation randomInformation)
{
    private readonly MonomorphizedProgramNode moduleNode = moduleNode;

    public RandomInformation RandomVariables = randomInformation;
    public CFuntions CFuntions { get; } = cFuntions;
    public LLVMFunction CurrentFunction { get; set; }

    private Dictionary<string, LLVMValue> Variables { get; set; } = [];
    private Dictionary<ModuleIndex, LLVMValue> GlobalVariables { get; } = [];
    public Dictionary<TypedBuiltinIndex, LLVMShankFunction> BuiltinFunctions { get; } = [];
    private Dictionary<TypedModuleIndex, LLVMShankFunction> Functions { get; } = [];
    public Dictionary<TypedModuleIndex, LLVMStructType> Records { get; } = [];
    public Dictionary<ModuleIndex, LLVMEnumType> Enums { get; } = [];

    /// <summary>
    /// converts shank type to LLVM type
    /// </summary>
    /// <param name="dataType"></param>
    /// <param name="unknownType"></param>
    /// <returns></returns>
    public LLVMType GetLLVMTypeFromShankType(Type dataType)
    {
        return dataType switch
        {
            IntegerType => new LLVMIntegerType(),
            RecordType t => Records[t.MonomorphizedIndex],
            RealType => new LLVMRealType(),
            StringType => new LLVMStringType(),
            BooleanType => new LLVMBooleanType(),
            CharacterType => new LLVMCharacterType(),
            EnumType e => Enums[e.MonomorphizedIndex],
            // if it's a custom type we look it up in the context
            ReferenceType r
                => new LLVMReferenceType((LLVMInnerReferenceType)GetLLVMTypeFromShankType(r.Inner)),
            ArrayType a => new LLVMArrayType(GetLLVMTypeFromShankType(a.Inner), a.Range)
        };
    }

    // given a shank type this gives back the correct constructor for the CDT (compiler date type)
    public Func<LLVMValueRef, bool, LLVMValue> NewVariable(Type type)
    {
        Func<LLVMValueRef, bool, LLVMValue> typeConstructor = type switch
        {
            IntegerType => LLVMInteger.New,
            RealType => LLVMReal.New,
            RecordType recordType
                => (value, mutable) =>
                    LLVMStruct.New(value, mutable, Records[recordType.MonomorphizedIndex]),

            StringType => LLVMString.New,
            BooleanType => LLVMBoolean.New,
            CharacterType => LLVMCharacter.New,
            EnumType => LLVMInteger.New,
            ReferenceType r
                => (value, mutable) =>
                    new LLVMReference(
                        value,
                        mutable,
                        new LLVMReferenceType(
                            (LLVMInnerReferenceType)GetLLVMTypeFromShankType(r.Inner)
                        )
                    ),
            ArrayType arrayType
                => (value, mutable) =>
                    new LLVMArray(
                        value,
                        mutable,
                        new LLVMArrayType(
                            GetLLVMTypeFromShankType(arrayType.Inner),
                            arrayType.Range
                        )
                    ),
        };

        return (value, mutable) => typeConstructor(value, mutable);
    }

    /// <summary>
    /// converts shank type to LLVM type
    /// </summary>
    /// <param name="dataType"></param>
    /// <param name="unknownType"></param>
    /// <returns></returns>
    public LLVMTypeRef GetLLVMTypeFromShankType(Type type, bool isVar)
    {
        var llvmTypeFromShankType = GetLLVMTypeFromShankType(type);
        return isVar
            ? LLVMTypeRef.CreatePointer(llvmTypeFromShankType.TypeRef, 0)
            : llvmTypeFromShankType.TypeRef;
    }

    /// <summary>
    /// gets the variable
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
            throw new Exception($"undefined variable name {index}");
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
    /// resets the Variable dictionary run at the end of a function
    /// </summary>
    public void ResetLocal() => Variables = new();

    public void AddFunction(TypedModuleIndex name, LLVMShankFunction function) =>
        Functions[name] = function;

    public void AddBuiltinFunction(TypedBuiltinIndex name, LLVMShankFunction function) =>
        BuiltinFunctions[name] = function;

    public LLVMShankFunction GetFunction(Index name) =>
        name is TypedModuleIndex m ? Functions[m] : BuiltinFunctions[(TypedBuiltinIndex)name];
}

public record struct RandomInformation(
    LLVMValueRef S0,
    LLVMValueRef S1,
    LLVMValueRef S2,
    LLVMValueRef S3
)
{
    public RandomInformation(LLVMModuleRef module)
        : this(
            module.AddGlobal(LLVMTypeRef.Int64, "s0"),
            module.AddGlobal(LLVMTypeRef.Int64, "s1"),
            module.AddGlobal(LLVMTypeRef.Int64, "s2"),
            module.AddGlobal(LLVMTypeRef.Int64, "s3")
        )
    {
        S0 = S0 with { Initializer = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0) };
        S1 = S1 with { Initializer = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0) };
        S2 = S2 with { Initializer = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0) };
        S3 = S3 with { Initializer = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0) };
    }
}
