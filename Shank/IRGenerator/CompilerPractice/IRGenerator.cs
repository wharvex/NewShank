using LLVMSharp.Interop;
using Shank.Interfaces;

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
            LlvmContext.VoidType,
            [LLVMTypeRef.CreatePointer(LlvmContext.Int8Type, 0)],
            true
        );
    }

    public void GenerateIr()
    {
        var shankStartModule = AstRoot.GetStartModuleSafe();
        var shankStartFunc = shankStartModule.GetStartFunctionSafe();

        // Create `main' (start) and `printf' (write) functions.
        var mainFunc = CreateFunc(shankStartFunc);
        var printfFunc = CreateFunc(shankStartModule.GetFromFunctionsByNameSafe("write"));

        // Experiment
        LlvmModule.AddFunction("printf", PrintfFuncType);

        // Create other functions only if their names appear in ValidFuncs.
        var otherFuncs = AstRoot
            .GetStartModuleSafe()
            .Functions.Where(kvp => ValidFuncs.Contains(kvp.Key))
            .Select(kvp => CreateFunc(kvp.Value))
            .ToList();

        // Add statements to `main'.
        var entryBlock = mainFunc.AppendBasicBlock("entry");
        LlvmBuilder.PositionAtEnd(entryBlock);
        if (otherFuncs.Count > 0)
        {
            var llvmStatementValueRefs = shankStartFunc
                .Statements.Select(sn => IrGeneratorByNode.CreateValueRef(this, sn))
                .ToList();
        }
        else
        {
            HelloWorld(printfFunc, "hello invalid");
        }
        LlvmBuilder.BuildRetVoid();

        OutputHelper.DebugPrintTxt(
            "aaa: " + LlvmContext.GetConstString("hi", true).Kind,
            "llvm_stuff"
        );
        OutputHelper.DebugPrintTxt(
            "bbb: " + LlvmBuilder.BuildGlobalStringPtr("hi2" + "\n").Kind,
            "llvm_stuff",
            true
        );
        var x = LlvmModule.GetNamedFunction("blah");
        OutputHelper.DebugPrintTxt("blah name length: " + x.Name.Length, "llvm_stuff", true);
        OutputHelper.DebugPrintTxt("blah is poison: " + x.IsPoison, "llvm_stuff", true);

        // Verify all the functions.
        mainFunc.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        // Output.
        LlvmModule.PrintToFile(Path.Combine(OutputHelper.DocPath, "IrOutput.ll"));
    }

    public void GenerateIrFlat() { }

    private void HelloWorld(LLVMValueRef printfFunc, string msg)
    {
        var myStr = LlvmBuilder.BuildGlobalStringPtr(msg + "\n");
        LlvmBuilder.BuildCall2(PrintfFuncType, printfFunc, [myStr]);
    }

    private LLVMValueRef CreateFunc(CallableNode callableNode)
    {
        //LlvmFuncWrapper = callableNode is FunctionNode ? new LlvmFuncWrapper(LLVMTypeRef.CreateFunction( LlvmContext.Int1Type, GetParamTypes(callableNode.ParameterVariables) ), LlvmModule.AddFunction( ((ILlvmTranslatable)callableNode).GetNameForLlvm(), funcType ); )

        LLVMValueRef func;

        // Builtin functions are only CallableNodes.
        // Non-Builtin (i.e. user-created) functions are CallableNodes and FunctionNodes.
        if (callableNode is FunctionNode)
        {
            var returnType = GetReturnType(callableNode.ParameterVariables);
            var funcType = LLVMTypeRef.CreateFunction(
                returnType,
                GetParamTypes(callableNode.ParameterVariables)
            );

            func = LlvmModule.AddFunction(
                ((ILlvmTranslatable)callableNode).GetNameForLlvm(),
                funcType
            );
        }
        else
        {
            (func, _) = CreateBuiltin(callableNode);
        }

        // Make the function visible externally.
        func.Linkage = LLVMLinkage.LLVMExternalLinkage;

        return func;
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

    private LLVMTypeRef[] GetParamTypes(IEnumerable<VariableNode> varNodes) =>
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
    private LLVMTypeRef GetReturnType(IEnumerable<VariableNode> vns) =>
        vns.Where(vn => !vn.IsConstant)
            .Select(vn => GetLlvmTypeFromShankType(vn.Type))
            .FirstOrDefault(LlvmContext.VoidType);

    private LLVMTypeRef GetLlvmTypeFromShankType(VariableNode.DataType dataType) =>
        dataType switch
        {
            VariableNode.DataType.Integer => LlvmContext.Int64Type,
            VariableNode.DataType.Real => LlvmContext.DoubleType,
            VariableNode.DataType.String => LLVMTypeRef.CreatePointer(LlvmContext.Int8Type, 0),
            VariableNode.DataType.Boolean => LlvmContext.Int1Type,
            VariableNode.DataType.Character => LlvmContext.Int8Type,
            _ => throw new NotImplementedException()
        };
}
