using LLVMSharp.Interop;

namespace Shank.IRGenerator;

public class IrGenerator
{
    private ProgramNode AstRoot { get; init; }

    public IrGenerator(ProgramNode astRoot)
    {
        AstRoot = astRoot;
        ModuleInit();
    }

    public void GenerateIr()
    {
        // Create `main' and `write' functions.
        var mainFunc = CreateFunc(AstRoot.GetStartModuleSafe().GetStartFunctionSafe());
        var printfFunc = CreateFunc(
            AstRoot.GetStartModuleSafe().GetFromFunctionsByNameSafe("write")
        );

        // Add statements to `main'.
        var entryBlock = mainFunc.AppendBasicBlock("entry");
        LlvmBuilder.PositionAtEnd(entryBlock);
        HelloWorld(printfFunc);
        LlvmBuilder.BuildRetVoid();

        // Verify all the functions.
        mainFunc.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        // Output.
        var outPath = Directory.CreateDirectory(
            Path.Combine(Directory.GetCurrentDirectory(), "IR")
        );
        LlvmModule.PrintToFile(Path.Combine(outPath.FullName, "output4.ll"));
    }

    private void HelloWorld(LLVMValueRef printfFunc)
    {
        var myStr = LlvmBuilder.BuildGlobalStringPtr("Hello, World!\n");
        LlvmBuilder.BuildCall2(_printfFuncType, printfFunc, [myStr]);
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

            var funcName = callableNode.Name.Equals("start") ? "main" : callableNode.Name;

            // What happens if you try to create a function that already exists?
            func = LlvmModule.AddFunction(funcName, funcType);
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
            "write" => (LlvmModule.AddFunction("printf", _printfFuncType), true),
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

        _printfFuncType = LLVMTypeRef.CreateFunction(
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
    private LLVMModuleRef LlvmModule { get; set; }

    /// <summary>
    /// Provides uniform API for creating instructions and inserting them into a BasicBlock, either
    /// at the end of a BasicBlock, or at a specific iterator location in a block.
    /// </summary>
    private LLVMBuilderRef LlvmBuilder { get; set; }

    private LLVMTypeRef _printfFuncType;
    private LLVMTypeRef _printfRetType;
}
