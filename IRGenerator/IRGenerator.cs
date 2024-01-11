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
    public IRGenerator()
    {
        this.ModuleInit();
    }

    public void GenerateIR(string program)
    {
        Console.WriteLine(this._module.PrintToString());

        var outPath = Directory.CreateDirectory(
            Path.Combine(Directory.GetCurrentDirectory(), "IR")
        );

        this._module.PrintToFile(Path.Combine(outPath.FullName, "output4.ll"));
    }

    private void SetupAndCallPrint(LLVMValueRef[] printArgs)
    {
        var bytePtrTy = LLVMTypeRef.CreatePointer(this._context.Int8Type, 0);
        var printFType = LLVMTypeRef.CreateFunction(
            this._context.VoidType,
            new[] { bytePtrTy },
            true
        );
        var printF = this._module.AddFunction("printf", printFType);
        printF.Linkage = LLVMLinkage.LLVMExternalLinkage;
        printF.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        this._builder.BuildCall2(printFType, printF, printArgs);
    }

    private void CreateMainFunc()
    {
        var func = this.GetFunc(Interpreter.Functions["start"]);
        Gen(); // TODO: Pass in AST
        this._builder.BuildRet(LLVMValueRef.CreateConstInt(this._context.Int32Type, 0));
    }

    private void Gen()
    {
        var myStr = this._builder.BuildGlobalStringPtr("Hello, World!\n");
        this.SetupAndCallPrint(new[] { myStr });
    }

    private LLVMValueRef GetFunc(CallableNode callNode)
    {
        if (callNode is FunctionNode funcNode)
        {
            return this.CreateFunc(funcNode);
        }

        // TODO: Handle builtin functions.
        return this.CreateFunc(new FunctionNode("blah"));
    }

    private LLVMValueRef CreateFunc(FunctionNode funcNode)
    {
        var returnType = this.GetReturnType(funcNode.ParameterVariables);

        // TODO: How to tell if a FunctionNode accepts variadic arguments?
        var funcType = LLVMTypeRef.CreateFunction(
            returnType,
            this.GetParamTypes(funcNode.ParameterVariables),
            false
        );

        var funcName = string.Equals(funcNode.Name, "start") ? "main" : funcNode.Name;

        // TODO: Keep track of functions that have already been created.
        // What happens if you try to create a function that already exists?
        LLVMValueRef func = this._module.AddFunction(funcName, funcType);

        // Make the function visible externally.
        func.Linkage = LLVMLinkage.LLVMExternalLinkage;

        LLVMBasicBlockRef entryBlock = func.AppendBasicBlock("entry");

        // Specifies that created instructions should be appended to the end of the specified block.
        this._builder.PositionAtEnd(entryBlock);

        // TODO: Keep track of where the builder is.
        // Associate LLVM func name and block name with some kind of identifier of AST node groupings?
        // Multiple AST nodes will go into one LLVM block.
        // What should determine where to begin a new LLVM block?

        // TODO: Create a switch method to build a return value based on returnType.

        // Check the function for errors; print a message if one is found.
        func.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        return func;
    }

    private LLVMValueRef CreateFunc(CallableNode callNode)
    {
        string funcName;
        LLVMTypeRef[] paramTypes;
        LLVMTypeRef returnType,
            funcType;
        if (callNode is FunctionNode funcNode)
        {
            returnType = this.GetReturnType(funcNode.ParameterVariables);
            paramTypes = this.GetParamTypes(funcNode.ParameterVariables);
            funcType = LLVMTypeRef.CreateFunction(returnType, paramTypes, false);
        }
        else
        {
            funcType = this._printfFnType;
        }

        // TODO: Keep track of functions that have already been created.
        // What happens if you try to create a function that already exists?
        LLVMValueRef func = this._module.AddFunction(callNode.Name, funcType);

        // Make the function visible externally.
        func.Linkage = LLVMLinkage.LLVMExternalLinkage;

        if (true)
        {
            LLVMBasicBlockRef entryBlock = func.AppendBasicBlock("entry");

            // Specifies that created instructions should be appended to the end of the specified block.
            this._builder.PositionAtEnd(entryBlock);

            // TODO: Keep track of where the builder is.
            // Associate LLVM func name and block name with some kind of identifier of AST node groupings?
            // Multiple AST nodes will go into one LLVM block.
            // What should determine where to begin a new LLVM block?
        }

        return func;
    }

    private (LLVMTypeRef, bool) GetBuiltinTypeAndNativity(CallableNode builtin) =>
        builtin.Name switch
        {
            "write" => (this._printfFnType, true),
            _ => (this._printfFnType, true),
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

        this._printfFnType = LLVMTypeRef.CreateFunction(
            _context.VoidType,
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

    private LLVMTypeRef _printfFnType;
}
