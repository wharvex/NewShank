using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Shank;

public class IRGenerator
{
    public IRGenerator(string fnNamePrefix)
    {
        this.ModuleInit();
        this._fnNamePrefix = fnNamePrefix;
    }

    public void GenerateIR()
    {
        //// Create all the functions.
        //if (
        //    Interpreter.Functions.ContainsKey(this._fnNamePrefix + "start")
        //    && Interpreter.Functions.ContainsKey(this._fnNamePrefix + "write")
        //)
        //{
        //    var mainFunc = this.CreateFunc(Interpreter.Functions[this._fnNamePrefix + "start"]);
        //    var printfFunc = this.CreateFunc(Interpreter.Functions[this._fnNamePrefix + "write"]);

        //    // Add all the statements.
        //    LLVMBasicBlockRef entryBlock = mainFunc.AppendBasicBlock("entry");
        //    this._builder.PositionAtEnd(entryBlock);
        //    this.HelloWorld(printfFunc);
        //    this._builder.BuildRetVoid();

        //    // Verify all the functions.
        //    mainFunc.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        //}

        //// Output.
        //var outPath = Directory.CreateDirectory(
        //    Path.Combine(Directory.GetCurrentDirectory(), "IR")
        //);
        //this._module.PrintToFile(Path.Combine(outPath.FullName, "output4.ll"));
    }

    private void HelloWorld(LLVMValueRef printfFunc)
    {
        var myStr = this._builder.BuildGlobalStringPtr("Hello, World!\n");
        this._builder.BuildCall2(this._printfFuncType, printfFunc, new[] { myStr });
    }

    private LLVMValueRef CreateFunc(CallableNode callableNode)
    {
        LLVMValueRef func;

        // Builtin functions are only CallableNodes.
        // Non-Builtin (i.e. user-created) functions are CallableNodes and FunctionNodes.
        if (callableNode is FunctionNode)
        {
            var returnType = this.GetReturnType(callableNode.ParameterVariables);
            var funcType = LLVMTypeRef.CreateFunction(
                returnType,
                this.GetParamTypes(callableNode.ParameterVariables),
                false
            );

            var funcName = callableNode.OrigName.Equals("start") ? "main" : callableNode.Name;

            // What happens if you try to create a function that already exists?
            func = this._module.AddFunction(funcName, funcType);
        }
        else
        {
            (func, var _) = this.CreateBuiltin(callableNode);
        }

        // Make the function visible externally.
        func.Linkage = LLVMLinkage.LLVMExternalLinkage;

        return func;
    }

    /**
     * Creates the passed in Builtin function and adds it to the module.
     * Returns the function and a boolean indicating whether the function is "native"
     * to LLVM. For example, "printf" is native to LLVM because you can add it to the module
     * and it automatically has the ability to print to stdout. This means you don't need
     * to give it a basic block with statements.
     */
    private (LLVMValueRef, bool) CreateBuiltin(CallableNode builtin) =>
        builtin.Name switch
        {
            "write" => (this._module.AddFunction("printf", this._printfFuncType), true),
            _ => (this._module.AddFunction("printf", this._printfFuncType), true)
        };

    private LLVMTypeRef[] GetParamTypes(List<VariableNode> varNodes)
    {
        return varNodes.Select(vn => this.GetLLVMTypeFromShankType(vn.Type)).ToArray();
    }

    /**
     * GetReturnTypes
     *
     * Finds the LLVM types corresponding to the Shank types of all the
     * function's non-constant parameter variables.
     * These will be the ones marked with "var" that can be "returned".
     * TODO: Handle multiple returns (use a struct?).
     */
    private LLVMTypeRef GetReturnType(List<VariableNode> varNodes)
    {
        var returnTypes = varNodes
            .Where(vn => !vn.IsConstant)
            .Select(vn => this.GetLLVMTypeFromShankType(vn.Type))
            .ToList();
        if (returnTypes.Count < 1)
        {
            returnTypes.Add(this._context.VoidType);
        }
        return returnTypes[0];
    }

    private void ModuleInit()
    {
        this._context = LLVMContextRef.Create();
        this._module = _context.CreateModuleWithName("root");
        this._builder = _context.CreateBuilder();

        this._printfFuncType = LLVMTypeRef.CreateFunction(
            this._context.VoidType,
            new[] { LLVMTypeRef.CreatePointer(this._context.Int8Type, 0) },
            true
        );
    }

    private LLVMTypeRef GetLLVMTypeFromShankType(VariableNode.DataType dataType) =>
        dataType switch
        {
            VariableNode.DataType.Integer => this._context.Int64Type,
            VariableNode.DataType.Real => this._context.DoubleType,
            VariableNode.DataType.String => LLVMTypeRef.CreatePointer(this._context.Int8Type, 0),
            VariableNode.DataType.Boolean => this._context.Int1Type,
            VariableNode.DataType.Character => this._context.Int8Type,
            _ => this._context.Int32Type,
        };

    /**
     * Context
     *
     * Owns and manages the core "global" data of LLVM's core
     * infrastructure, including the type and constant uniquing tables.
     */
    private LLVMContextRef _context;

    /**
     * Module
     *
     * A Module instance is used to store all the information related to an
     * LLVM module. Modules are the top level container of all other LLVM
     * Intermediate Representation (IR) objects. Each module directly contains a
     * list of global variables, a list of functions, a list of libraries (or
     * other modules) it depends on, a symbol table, and various data
     * about the target's characteristics.
     *
     * A module maintains a GlobalList object that is used to hold all
     * constant references to global variables in the module. When a global
     * variable is destroyed, it should have no entries in the GlobalList.
     */
    private LLVMModuleRef _module;

    /**
     * Builder
     *
     * Provides uniform API for creating instructions and inserting them
     * into a basic block: either at the end of a BasicBlock, or at a
     * specific iterator location in a block.
     */
    private LLVMBuilderRef _builder;

    private LLVMTypeRef _printfFuncType;
    private LLVMTypeRef _printfRetType;
    private string _fnNamePrefix;
}
