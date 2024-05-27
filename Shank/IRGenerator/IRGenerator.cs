using LLVMSharp.Interop;
using Shank.Interfaces;

namespace Shank.IRGenerator;

public class IrGenerator
{
    private List<string> ValidFuncs { get; } = ["validForLlvm"];
    private ProgramNode AstRoot { get; }

    public IrGenerator(ProgramNode astRoot)
    {
        AstRoot = astRoot;
        ModuleInit();
    }

    public void GenerateIr()
    {
        var shankStartModule = AstRoot.GetStartModuleSafe();
        var shankStartFunc = shankStartModule.GetStartFunctionSafe();

        // Create `main' (start) and `printf' (write) functions.
        var mainFunc = CreateFunc(shankStartFunc);
        var printfFunc = CreateFunc(shankStartModule.GetFromFunctionsByNameSafe("write"));

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
            llvmStatementValueRefs.ForEach(lsvr => OutputHelper.DebugPrintTxt(lsvr.Name, 7));
        }
        else
        {
            HelloWorld(printfFunc, "hello invalid");
        }
        LlvmBuilder.BuildRetVoid();

        // Verify all the functions.
        mainFunc.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        // Output.
        LlvmModule.PrintToFile(Path.Combine(OutputHelper.DocPath, "IrOutput.ll"));
    }

    private void HelloWorld(LLVMValueRef printfFunc, string msg)
    {
        var myStr = LlvmBuilder.BuildGlobalStringPtr(msg + "\n");
        LlvmBuilder.BuildCall2(PrintfFuncType, printfFunc, [myStr]);
    }

    private LLVMValueRef CreateFunc(CallableNode callableNode)
    {
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

            // What happens if you try to create a function that already exists?
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

    /// <summary>
    /// Creates the given Shank-Builtin function and adds it to the module. Returns the function and
    /// a boolean indicating whether the function is "native" to LLVM. For example, "printf" is
    /// native to LLVM because when added to the module, it automatically has the ability to print
    /// to stdout. This means you don't need to give it a basic block with statements.
    /// </summary>
    /// <param name="builtin"></param>
    /// <returns></returns>
    private (LLVMValueRef, bool) CreateBuiltin(CallableNode builtin) =>
        builtin.Name switch
        {
            "write" => (LlvmModule.AddFunction("printf", PrintfFuncType), true),
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
    /// <param name="varNodes"></param>
    /// <returns></returns>
    private LLVMTypeRef GetReturnType(List<VariableNode> varNodes) =>
        varNodes
            .Where(vn => !vn.IsConstant)
            .Select(vn => GetLlvmTypeFromShankType(vn.Type))
            .FirstOrDefault(LlvmContext.VoidType);

    private void ModuleInit()
    {
        LlvmContext = LLVMContextRef.Create();
        LlvmModule = LlvmContext.CreateModuleWithName("root");
        LlvmBuilder = LlvmContext.CreateBuilder();

        PrintfFuncType = LLVMTypeRef.CreateFunction(
            LlvmContext.VoidType,
            new[] { LLVMTypeRef.CreatePointer(LlvmContext.Int8Type, 0) },
            true
        );
    }

    private LLVMTypeRef GetLlvmTypeFromShankType(VariableNode.DataType dataType) =>
        dataType switch
        {
            VariableNode.DataType.Integer => LlvmContext.Int64Type,
            VariableNode.DataType.Real => LlvmContext.DoubleType,
            VariableNode.DataType.String => LLVMTypeRef.CreatePointer(LlvmContext.Int8Type, 0),
            VariableNode.DataType.Boolean => LlvmContext.Int1Type,
            VariableNode.DataType.Character => LlvmContext.Int8Type,
            _ => LlvmContext.Int32Type,
        };

    /// <summary>
    /// The LLVMContextRef owns and manages the core "global" data of LLVM's core infrastructure,
    /// including the type and constant "uniquing" tables.
    /// </summary>
    private LLVMContextRef LlvmContext { get; set; }

    /// <summary>
    /// A Module instance is used to store all the information related to an LLVM module.
    ///
    /// Modules are the top level container of all other LLVM Intermediate Representation (IR)
    /// objects.
    ///
    /// Each module directly contains a list of global variables, a list of functions, a list of
    /// libraries (or other modules) it depends on, a symbol table, and various data about the
    /// target's characteristics.
    ///
    /// A module maintains a GlobalList object that is used to hold all constant references to
    /// global variables in the module.
    ///
    /// When a global variable is destroyed, it should have no entries in the GlobalList.
    /// </summary>
    public LLVMModuleRef LlvmModule { get; set; }

    /// <summary>
    /// Provides uniform API for creating instructions and inserting them into a BasicBlock, either
    /// at the end of a BasicBlock, or at a specific iterator location in a block.
    /// </summary>
    public LLVMBuilderRef LlvmBuilder { get; set; }

    public LLVMTypeRef PrintfFuncType { get; set; }
    private LLVMTypeRef _printfRetType;
}
