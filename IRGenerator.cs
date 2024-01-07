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

    public void Exec(string program)
    {
        // 1. Parse the program.
        // ???

        // 2. Compile to LLVM IR.
        this.Compile();

        // 3a. Print generated IR code to console.
        Console.WriteLine(this._module.PrintToString());

        // 3b. Save generated IR code to file.
        this._module.PrintToFile(Path.Combine(Directory.GetCurrentDirectory(), "output4.ll"));
    }

    private void Compile()
    {
        var func = this.GetOrCreateFunc(new FunctionNode("main"));
        var result = Gen(func);
        this._builder.BuildRet(result);
    }

    private LLVMValueRef Gen(LLVMValueRef func)
    {
        return LLVMValueRef.CreateConstInt(this._context.Int32Type, 47);
    }

    private LLVMValueRef GetOrCreateFunc(CallableNode callNode)
    {
        if (callNode is FunctionNode funcNode)
        {
            return this.CreateFunc(funcNode);
        }
        // TODO: Handle builtin functions.
        return this.CreateFunc(new FunctionNode("blah"));
    }

    private LLVMValueRef? GetFunc(string funcName)
    {
        try
        {
            return this._module.GetNamedFunction(funcName);
        }
        catch (Exception e)
        {
            return null;
        }
    }

    private LLVMValueRef CreateFunc(FunctionNode funcNode)
    {
        LLVMTypeRef[] paramTypes = this.GetParamTypes(funcNode.ParameterVariables);

        // Find all of the function's non-constant parameter variables.
        // These will be the ones marked with var that can be "returned".
        List<LLVMTypeRef> returnTypes = funcNode
            .ParameterVariables.Where(paramVar => !paramVar.IsConstant)
            .Select(varVar => this.GetLLVMTypeFromShankType(varVar.Type))
            .ToList();

        // TODO: Handle multiple "return" types with var.
        // Maybe create multiple functions, one for each "return"?
        LLVMTypeRef returnType = returnTypes.Count > 0 ? returnTypes[0] : this._context.Int32Type;

        LLVMTypeRef funcType = LLVMTypeRef.CreateFunction(returnType, paramTypes);
        LLVMValueRef func = this._module.AddFunction(funcNode.Name, funcType);

        // Check the function for errors, print a message if one is found.
        func.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        LLVMBasicBlockRef entryBlock = func.AppendBasicBlock("entry");

        // Specifies that created instructions should be appended to the end of the specified block.
        this._builder.PositionAtEnd(entryBlock);

        return func;
    }

    private LLVMTypeRef[] GetParamTypes(List<VariableNode> varNodes)
    {
        return varNodes.Select(vn => this.GetLLVMTypeFromShankType(vn.Type)).ToArray();
    }

    private void ModuleInit()
    {
        this._context = LLVMContextRef.Create();
        this._module = _context.CreateModuleWithName("root");
        this._builder = _context.CreateBuilder();
    }

    private LLVMTypeRef GetLLVMTypeFromShankType(VariableNode.DataType dataType) =>
        dataType switch
        {
            VariableNode.DataType.Integer => this._context.Int32Type,
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
     * other modules) this module depends on, a symbol table, and various data
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
}
