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

    protected LLVMFunction() { }

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

public class LLVMArray(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef typeRef)
    : LLVMValue(valueRef, isMutable, typeRef)
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef type) =>
        new LLVMString(valueRef, isMutable, type);
}

public class LLVMCharacter(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef typeRef)
    : LLVMValue(valueRef, isMutable, typeRef)
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMTypeRef type) =>
        new LLVMCharacter(valueRef, isMutable, type);
}

public class LLVMStruct(LLVMValueRef valueRef, bool isMutable, LLVMStructType typeRef) : LLVMValue(valueRef, isMutable, typeRef.LlvmTypeRef)
{
    public static LLVMValue New(LLVMValueRef valueRef, bool isMutable, LLVMStructType type) =>
        new LLVMStruct(valueRef, isMutable, type);

    public int Access(string name)
    {
        return  typeRef.GetMemberIndex(name);
    }

    public Type GetTypeOf(string name)
    {
        return ((RecordType)typeRef.Type).Fields[name];
    }
}
public abstract class LLVMType(Type type, LLVMTypeRef llvmtype)
{
    public Type Type { get; set; } = type;
    public LLVMTypeRef LlvmTypeRef { get; set; } = llvmtype;
}
public class LLVMStructType(RecordType type, LLVMTypeRef llvmtype, List<string> members) : LLVMType(type, llvmtype)
{
    public int GetMemberIndex(string member) => members.IndexOf(member);
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
                new LLVMTypeRef[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) },
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
        malloc.Linkage = LLVMLinkage.LLVMExternalLinkage;
    }

    //  int printf(const char *restrict format, ...);
    // sometimes llvm can optimize this to a puts
    public LLVMFunction printf { get; }

    // void *memcpy(void dest[restrict .n], const void src[restrict .n], size_t n);
    public LLVMFunction memcpy { get; }

    // void *malloc(size_t n);
    public LLVMFunction malloc { get; }
}

public class Module
{
    public Dictionary<string, LLVMShankFunction> Functions { get; } = new();
    public Dictionary<string, LLVMType> CustomTypes { get; } = new();

    // global variables, constants or variables defined at the top level
    public Dictionary<string, LLVMValue> GloabalVariables { get; } = new();
}

public class Context
{
    public LLVMTypeRef StringType = LLVMTypeRef.CreateStruct(
        [LLVMTypeRef.Int32, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)],
        false
    );

    public Context(ModuleNode moduleNode, CFuntions cFuntions)
    {
        this.moduleNode = moduleNode;
        CFuntions = cFuntions;
    }

    public ModuleNode moduleNode { get; set; }
    public CFuntions CFuntions { get; }
    public LLVMFunction CurrentFunction { get; set; }
    public Dictionary<string, LLVMShankFunction> BuiltinFunctions { get; } = new();
    public Dictionary<string, LLVMValue> Variables { get; set; } = new();

    public void SetCurrentModule(string module) => CurrentModule = Modules[module];

    public Module CurrentModule { get; private set; }

    public Dictionary<string, Module> Modules = new();

    /// <summary>
    /// converts shank type to LLVM type
    /// </summary>
    /// <param name="dataType"></param>
    /// <param name="unknownType"></param>
    /// <returns></returns>
    public LLVMTypeRef? GetLLVMTypeFromShankType(Type dataType)
    {
        return dataType switch
        {
            IntegerType => LLVMTypeRef.Int64,
            RealType => LLVMTypeRef.Double,
            RecordType recordType => CurrentModule.CustomTypes[recordType.Name].LlvmTypeRef,
            Shank.StringType => StringType,
            BooleanType => LLVMTypeRef.Int1,
            CharacterType => LLVMTypeRef.Int8,
            EnumType e => LLVMTypeRef.Int32,
            // if it's a custom type we look it up in the context
            ReferenceType r
                => LLVMTypeRef.CreatePointer((LLVMTypeRef)GetLLVMTypeFromShankType(r.Inner), 0),
            ArrayType a
                => LLVMTypeRef.CreatePointer(
                    LLVMTypeRef.CreateStruct(
                        [(LLVMTypeRef)GetLLVMTypeFromShankType(a.Inner), LLVMTypeRef.Int32],
                        false
                    ),
                    0
                ),
            UnknownType t => CurrentModule.CustomTypes[t.TypeName], // TODO: instiate type parameters if needed
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
                Shank.StringType => (LLVMString.New, LLVMStringType: StringType),
                UnknownType unknownType => throw new NotImplementedException(),
                BooleanType => (LLVMBoolean.New, LLVMTypeRef.Int1),
                CharacterType => (LLVMCharacter.New, LLVMTypeRef.Int8),
                EnumType enumType => throw new NotImplementedException(),
                // if it's a custom type we look it up in the context
                ReferenceType => null,
                ArrayType
                    => (
                        LLVMArray.New,
                        LLVMTypeRef.CreatePointer(
                            LLVMTypeRef.CreateStruct(
                                [
                                    GetLLVMTypeFromShankType(((ArrayType)type).Inner).Value,
                                    LLVMTypeRef.Int32
                                ],
                                false
                            ),
                            0
                        )
                    )
            };

        (Func<LLVMValueRef, bool, LLVMTypeRef, LLVMValue>, LLVMTypeRef)? NewRecordValue(RecordType recordType)
        {
            var type = CurrentModule.CustomTypes[recordType.Name];
            return ((Value, mutable, _type) => LLVMStruct.New(Value, mutable, (LLVMStructType)type), type.LlvmTypeRef);
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
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public LLVMValue GetVariable(string name)
    {
        if (CurrentModule.GloabalVariables.TryGetValue(name, out var globalVar))
        {
            return globalVar;
        }
        else if (Variables.TryGetValue(name, out var varaible))
        {
            return varaible;
        }
        else
        {
            throw new Exception("undefined varname");
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
    public void AddVariable(string name, LLVMValue value, bool isGlobal)
    {
        if (CurrentModule.GloabalVariables.ContainsKey(name) || Variables.ContainsKey(name))
            throw new Exception("error");
        else
        {
            if (isGlobal)
                CurrentModule.GloabalVariables.Add(name, value);
            else
                Variables.Add(name, value);
        }
    }

    /// <summary>
    /// resets the Varaible dictionary run at the end of a function
    /// </summary>
    public void ResetLocal()
    {
        Variables = new();
    }
    public void ResetLocal() => Variables = new();

    public void AddFunction(string name, LLVMShankFunction function) => CurrentModule.Functions[name] = function;

    public void AddBuiltinFunction(string name, LLVMShankFunction function) => BuiltinFunctions[name] = function;

    public LLVMShankFunction? GetFunction(string name) =>
        CurrentModule.Functions.ContainsKey(name)
            ? CurrentModule.Functions[name]
            : BuiltinFunctions.TryGetValue(name, out var function)
                ? function
                : null;

    public void SetModules(IEnumerable<String> modulesKeys) => Modules = modulesKeys.Select(moduleName => (moduleName, new Module())).ToDictionary();

    public LLVMType GetCustomType(string nodeName) => CurrentModule.CustomTypes[nodeName];

    public void AddCustomType(string import, LLVMType value) => CurrentModule.CustomTypes[import] = value;
}
