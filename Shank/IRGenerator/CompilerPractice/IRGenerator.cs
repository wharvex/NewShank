using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.IRGenerator.CompilerPractice;

public class IrGenerator
{
    private bool Arb { get; set; } = false;
    private List<string> ValidFuncs { get; } = ["validForLlvm"];
    private ProgramNode AstRoot { get; }

    /// <summary>
    /// "The Context is an opaque object that owns a lot of core LLVM data structures, such as the
    /// type and constant value tables."
    /// ~ https://llvm.org/docs/tutorial/MyFirstLanguageFrontend/LangImpl03.html
    /// </summary>
    public LLVMContextRef LlvmContext { get; }

    /// <summary>
    /// "The Module is an LLVM construct that contains functions and global variables. In many ways,
    /// it is the top-level structure that the LLVM IR uses to contain code. It will own the memory
    /// for all of the IR that we generate."
    /// ~ https://llvm.org/docs/tutorial/MyFirstLanguageFrontend/LangImpl03.html
    /// </summary>
    public LLVMModuleRef LlvmModule { get; }

    /// <summary>
    /// "The Builder object is a helper object that makes it easy to generate LLVM instructions.
    /// Instances of the LLVMBuilderRef class keep track of the current place to insert instructions
    /// and has methods to create new instructions."
    /// ~ https://llvm.org/docs/tutorial/MyFirstLanguageFrontend/LangImpl03.html
    /// </summary>
    public LLVMBuilderRef LlvmBuilder { get; }

    /// <summary>
    /// Needed to make Shank's `write' builtin work.
    /// </summary>
    public LLVMTypeRef PrintfFuncType { get; }

    public IrGenerator(ProgramNode astRoot)
    {
        AstRoot = astRoot;
        LlvmContext = LLVMContextRef.Create();
        LlvmModule = LlvmContext.CreateModuleWithName("root");
        LlvmBuilder = LlvmContext.CreateBuilder();

        PrintfFuncType = LLVMTypeRef.CreateFunction(
            LlvmContext.Int32Type,
            [LLVMTypeRef.CreatePointer(LlvmContext.Int8Type, 0)],
            true
        );
    }

    public IrGenerator() { }

    public void GenerateIr()
    {
        //var shankStartModule = AstRoot.GetStartModuleSafe();
        //var shankStartFunc = shankStartModule.GetStartFunctionSafe();

        //// Create `main' (start) and `printf' (write) functions.
        //var mainFunc = CreateFunc(shankStartFunc);
        //var printfFunc = CreateFunc(shankStartModule.GetFromFunctionsByNameSafe("write"));

        //// Experiment
        //LlvmModule.AddFunction("printf", PrintfFuncType);

        //// Create other functions only if their names appear in ValidFuncs.
        //var otherFuncs = AstRoot
        //    .GetStartModuleSafe()
        //    .Functions.Where(kvp => ValidFuncs.Contains(kvp.Key))
        //    .Select(kvp => CreateFunc(kvp.Value))
        //    .ToList();

        //// Add statements to `main'.
        //var entryBlock = mainFunc.AppendBasicBlock("entry");
        //LlvmBuilder.PositionAtEnd(entryBlock);
        //if (otherFuncs.Count > 0)
        //{
        //    var llvmStatementValueRefs = shankStartFunc
        //        .Statements.Select(sn => IrGeneratorByNode.CreateValueRef(this, sn))
        //        .ToList();
        //}
        //else
        //{
        //    HelloWorld(printfFunc, "hello invalid");
        //}

        //LlvmBuilder.BuildRetVoid();

        //OutputHelper.DebugPrintTxt(
        //    "aaa: " + LlvmContext.GetConstString("hi", true).Kind,
        //    "llvm_stuff"
        //);
        //OutputHelper.DebugPrintTxt(
        //    "bbb: " + LlvmBuilder.BuildGlobalStringPtr("hi2" + "\n").Kind,
        //    "llvm_stuff",
        //    true
        //);
        //var x = LlvmModule.GetNamedFunction("blah");
        //OutputHelper.DebugPrintTxt("blah name length: " + x.Name.Length, "llvm_stuff", true);
        //OutputHelper.DebugPrintTxt("blah is poison: " + x.IsPoison, "llvm_stuff", true);

        //// Verify all the functions.
        //mainFunc.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        //// Output.
        //LlvmModule.PrintToFile(Path.Combine(OutputHelper.DocPath, "IrOutput.ll"));
    }

    public void GenerateIrFlatPrintStr(string moduleName)
    {
        // What is encoded in the AST that each of these lines could be a response to?
        var llvmContext = LLVMContextRef.Create();
        var llvmModule = llvmContext.CreateModuleWithName(moduleName);
        var llvmBuilder = llvmContext.CreateBuilder();

        var writeFuncType = LLVMTypeRef.CreateFunction(
            llvmContext.Int32Type,
            [LLVMTypeRef.CreatePointer(llvmContext.Int8Type, 0)],
            true
        );
        var startFuncType = LLVMTypeRef.CreateFunction(llvmContext.Int32Type, []);

        var writeFunc = llvmModule.AddFunction("printf", writeFuncType);
        var startFunc = llvmModule.AddFunction("main", startFuncType);

        // External seems to be the default because nothing in the IR changes if these lines are
        // commented out.
        // Also see: https://stackoverflow.com/a/1358622/16458003
        writeFunc.Linkage = LLVMLinkage.LLVMExternalLinkage;
        startFunc.Linkage = LLVMLinkage.LLVMExternalLinkage;

        var entryBlock = startFunc.AppendBasicBlock("entry");
        llvmBuilder.PositionAtEnd(entryBlock);
        llvmBuilder.BuildCall2(
            writeFuncType,
            writeFunc,
            [llvmBuilder.BuildGlobalStringPtr("Hello, World!\n")]
        );
        llvmBuilder.BuildRet(LLVMValueRef.CreateConstInt(llvmContext.Int32Type, 0));

        startFunc.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        writeFunc.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        llvmModule.PrintToFile(Path.Combine(OutputHelper.DocPath, "IrOutput.ll"));
    }

    public void GenerateIrFlatPrintStr2(string moduleName)
    {
        // Initialize IR environment.
        var llvmContext = LLVMContextRef.Create();
        var llvmModule = llvmContext.CreateModuleWithName(moduleName);
        var llvmBuilder = llvmContext.CreateBuilder();

        // Create function types.
        var writeFuncType = LLVMTypeRef.CreateFunction(
            llvmContext.Int32Type,
            [LLVMTypeRef.CreatePointer(llvmContext.Int8Type, 0)],
            true
        );
        var startFuncType = LLVMTypeRef.CreateFunction(llvmContext.Int32Type, []);

        // Create functions.
        var writeFunc = llvmModule.AddFunction("printf", writeFuncType);
        var startFunc = llvmModule.AddFunction("main", startFuncType);

        // Set function linkages. See: https://stackoverflow.com/a/1358622/16458003
        // (External seems to be the default because nothing in the IR changes if these lines are
        // commented out, whereas setting them to internal does produce a change.)
        writeFunc.Linkage = LLVMLinkage.LLVMExternalLinkage;
        startFunc.Linkage = LLVMLinkage.LLVMExternalLinkage;

        // Create entry block for `start' and position the builder at its end.
        var entryBlock = startFunc.AppendBasicBlock("entry");
        llvmBuilder.PositionAtEnd(entryBlock);

        // Create format string(s) for `write' call in `start'.
        var intFormatString = llvmBuilder.BuildGlobalStringPtr("%s");
        var b = LLVMValueRef.CreateConstInt(llvmContext.Int32Type, 12345);

        // Build `write' call in `start'.
        llvmBuilder.BuildCall2(writeFuncType, writeFunc, [intFormatString, b]);

        // Build return statement to terminate `start'.
        llvmBuilder.BuildRet(LLVMValueRef.CreateConstInt(llvmContext.Int32Type, 0));

        // Verify functions.
        startFunc.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        writeFunc.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        llvmModule.PrintToFile(Path.Combine(OutputHelper.DocPath, "IrOutput.ll"));
    }

    public void GenerateIrFlatPrintInt(string moduleName)
    {
        // Initialize IR environment.
        var llvmContext = LLVMContextRef.Create();
        var llvmModule = llvmContext.CreateModuleWithName(moduleName);
        var llvmBuilder = llvmContext.CreateBuilder();

        // Create this by passing in a func delegate to AstNode.GetLlvmType called something like
        // GetFuncRetType.
        var func1RetType = llvmContext.Int32Type;

        // Create this by passing in a func delegate to AstNode.GetLlvmTypes called something like
        // GetFuncParamsTypes
        LLVMTypeRef[] func1ParamsTypes = [LLVMTypeRef.CreatePointer(llvmContext.Int8Type, 0)];

        // Create this by passing in a func delegate to AstNode.GetLlvmBool called something like
        // GetFuncIsVarArg
        var func1IsVarArg = true;

        // Create this by passing in a func delegate to AstNode.GetLlvmString called something like
        // GetFuncName
        var func1Name = "printf";

        // Create function types.
        var writeFuncType = LLVMTypeRef.CreateFunction(
            func1RetType,
            func1ParamsTypes,
            func1IsVarArg
        );
        var startFuncType = LLVMTypeRef.CreateFunction(llvmContext.Int32Type, []);

        // Create functions.
        var writeFunc = llvmModule.AddFunction(func1Name, writeFuncType);
        var startFunc = llvmModule.AddFunction("main", startFuncType);

        // Set function linkages. See: https://stackoverflow.com/a/1358622/16458003
        // (External seems to be the default because nothing in the IR changes if these lines are
        // commented out, whereas setting them to internal does produce a change.)
        writeFunc.Linkage = LLVMLinkage.LLVMExternalLinkage;
        startFunc.Linkage = LLVMLinkage.LLVMExternalLinkage;

        // Create entry block for `start' and position the builder at its end.
        var entryBlock = startFunc.AppendBasicBlock("entry");
        llvmBuilder.PositionAtEnd(entryBlock);

        // Create arguments for `write' call in `start'.
        var writeCallArg1 = llvmBuilder.BuildGlobalStringPtr("%d");
        var writeCallArg2 = LLVMValueRef.CreateConstInt(llvmContext.Int32Type, 123);

        // Build `write' call in `start'.
        llvmBuilder.BuildCall2(writeFuncType, writeFunc, [writeCallArg1, writeCallArg2]);

        // Create return value for `start'.
        var startRet = LLVMValueRef.CreateConstInt(llvmContext.Int32Type, 0);

        // Build return statement to terminate `start'.
        llvmBuilder.BuildRet(startRet);

        // Verify functions.
        startFunc.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        writeFunc.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        llvmModule.PrintToFile(Path.Combine(OutputHelper.DocPath, "IrOutput.ll"));
    }

    private void HelloWorld(LLVMValueRef printfFunc, string msg)
    {
        var myStr = LlvmBuilder.BuildGlobalStringPtr(msg + "\n");
        LlvmBuilder.BuildCall2(PrintfFuncType, printfFunc, [myStr]);
    }

    private (LLVMValueRef, bool) CreateBuiltin(CallableNode builtin) =>
        builtin.Name switch
        {
            "write"
                //=> new LlvmFuncWrapper(
                //    PrintfFuncType,
                //    LlvmModule.AddFunction("printf", PrintfFuncType),
                //    true
                //),
                => (LlvmModule.AddFunction("printf", PrintfFuncType), true),
            _
                => throw new NotImplementedException(
                    "Creating an LLVM function for Shank-builtin `"
                        + builtin.Name
                        + "' not supported yet."
                )
        };

    private LLVMTypeRef[] GetParamTypes(IEnumerable<VariableDeclarationNode> varNodes) =>
        varNodes.Select(vn => GetLlvmTypeFromShankType(vn.Type)).ToArray();

    /// <summary>
    /// Find the first non-constant VariableNode in the given list and return its corresponding LLVM
    /// type. The given list is assumed to be parameter variables, so a non-constant list element
    /// would be a parameter marked with var that can be "returned" in the Shank way.
    ///
    /// TODO: This method should eventually be able to handle multiple return types.
    /// </summary>
    /// <param name="vns"></param>
    /// <returns></returns>
    private LLVMTypeRef GetReturnType(IEnumerable<VariableDeclarationNode> vns) =>
        vns.Where(vn => !vn.IsConstant)
            .Select(vn => GetLlvmTypeFromShankType(vn.Type))
            .FirstOrDefault(LlvmContext.VoidType);

    private LLVMTypeRef GetLlvmTypeFromShankType(Type type) =>
        type switch
        {
            IntegerType => LlvmContext.Int64Type,
            RealType => LlvmContext.DoubleType,
            StringType => LLVMTypeRef.CreatePointer(LlvmContext.Int8Type, 0),
            BooleanType => LlvmContext.Int1Type,
            CharacterType => LlvmContext.Int8Type,
            _ => throw new NotImplementedException()
        };
}
