using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

public class ModuleNode : ASTNode
{
    public Dictionary<string, EnumNode> Enums { get; init; }
    public string Name { get; set; }
    public Dictionary<string, CallableNode> Functions { get; init; }
    public Dictionary<string, List<CallableNode>> Functions2 { get; } = []; //not finished for overloaded functions
    public Dictionary<string, RecordNode> Records { get; init; }
    public Dictionary<string, VariableDeclarationNode> GlobalVariables { get; } = [];

    public Dictionary<string, ASTNode> Exported { get; set; }
    public Dictionary<string, ASTNode> Imported { get; set; }

    /// <summary>
    /// This dictionary is used to lookup by a module's name all the specific language construct
    /// names (e.g. function names, record names, etc.) that were "specifically" imported from that
    /// module using the square brackets import syntax.
    /// </summary>
    public Dictionary<string, LinkedList<string>> ImportTargetNames { get; set; }

    //the names of functions to be exported
    public LinkedList<string> ExportTargetNames { get; set; }

    public Dictionary<string, TestNode> Tests { get; set; }

    public ModuleNode(string name)
    {
        this.Name = name;
        Functions = new Dictionary<string, CallableNode>();
        Records = [];
        Exported = new Dictionary<string, ASTNode>();
        Imported = new Dictionary<string, ASTNode>();
        ImportTargetNames = new Dictionary<string, LinkedList<string>>();
        //ImportTargetNames = new LinkedList<string>();
        ExportTargetNames = new LinkedList<string>();
        Tests = new Dictionary<string, TestNode>();
        Enums = new Dictionary<string, EnumNode>();
    }

    public void AddToFunctions(CallableNode fn)
    {
        // If there is no name collision (i.e. the given function is not an overload),
        // then it can be added with no further action needed.
        if (Functions2.TryAdd(fn.Name, [fn]))
        {
            return;
        }

        // If there are any existing functions with the given function's name for which the
        // given function CANNOT be an overload (because their signatures are too similar),
        // then throw an exception.
        if (Functions2[fn.Name].Any(existingFn => !fn.IsValidOverloadOf(existingFn)))
        {
            throw new InvalidOperationException("Overload failed.");
        }

        // The given function has been confirmed as an overload and as valid to add.
        Functions2[fn.Name].Add(fn);
    }

    public CallableNode? GetFromFunctionsByCall(
        FunctionCallNode givenCall,
        Dictionary<string, VariableDeclarationNode> variablesInScope
    )
    {
        if (Functions2.TryGetValue(givenCall.Name, out var foundFns))
        {
            return foundFns.FirstOrDefault(
                fn => givenCall.EqualsWrtNameAndParams(fn, variablesInScope)
            );
        }

        return null;
    }

    public CallableNode GetFromFunctionsByNameSafe(string name)
    {
        if (Functions.TryGetValue(name, out var foundFn))
        {
            return foundFn;
        }

        throw new InvalidOperationException(
            "No function `" + name + "' found in module `" + Name + "'"
        );
    }

    public bool HasFuncWithName(string name)
    {
        return Functions.ContainsKey(name);
    }

    public void AddToGlobalVariables(List<VariableDeclarationNode> variables)
    {
        variables.ForEach(v =>
        {
            if (!GlobalVariables.TryAdd(v.GetNameSafe(), v))
            {
                throw new InvalidOperationException(
                    "Uncaught namespace conflict with global variable " + v.GetNameSafe()
                );
            }
        });
    }

    // TODO: This is no longer necessary.
    public Dictionary<string, ASTNode> GetImportedSafe()
    {
        var ret = new Dictionary<string, ASTNode>();
        Imported
            .ToList()
            .ForEach(
                i =>
                    ret.Add(
                        i.Key,
                        i.Value
                            ?? throw new InvalidOperationException(
                                "Expected the value associated with " + i.Key + " to not be null."
                            )
                    )
            );
        return ret;
    }

    public List<FunctionNode> GetFunctionsAsList() =>
        Functions
            .Where(cnKvp => cnKvp.Value is FunctionNode)
            .Select(funcKvp => (FunctionNode)funcKvp.Value)
            .ToList();

    public FunctionNode GetStartFunctionSafe() =>
        GetStartFunction()
        ?? throw new InvalidOperationException("Expected GetStartFunction to not return null.");

    public FunctionNode? GetStartFunction() =>
        Functions
            .Where(kvp => kvp.Key.Equals("start") && kvp.Value is FunctionNode)
            .Select(startKvp => (FunctionNode)startKvp.Value)
            .FirstOrDefault();

    // TODO: This method needs some work.
    public void updateImports(
        Dictionary<string, CallableNode> recievedFunctions,
        Dictionary<string, EnumNode> recievedEnums,
        Dictionary<string, RecordNode> recievedRecords,
        Dictionary<string, ASTNode?> recievedExports
    )
    {
        foreach (var function in recievedFunctions)
        {
            if (!Imported.ContainsKey(function.Key))
                Imported.Add(function.Key, function.Value);
            if (recievedExports.ContainsKey(function.Key))
            {
                ((CallableNode)Imported[function.Key]).IsPublic = true;
                continue;
            }

            string pmn =
                function.Value.parentModuleName
                ?? throw new Exception("Could not get parent module name while updating imports.");
            if (ImportTargetNames.ContainsKey(function.Value.parentModuleName))
            {
                if (ImportTargetNames[function.Value.parentModuleName] != null)
                {
                    if (!ImportTargetNames[function.Value.parentModuleName].Contains(function.Key))
                    {
                        ((CallableNode)Imported[function.Key]).IsPublic = false;
                    }
                }
            }
        }

        foreach (var Enum in recievedEnums)
        {
            if (!Imported.ContainsKey(Enum.Key))
                Imported.Add(Enum.Key, Enum.Value);
            if (recievedExports.ContainsKey(Enum.Key))
            {
                ((EnumNode)Imported[Enum.Key]).IsPublic = true;
                continue;
            }

            if (ImportTargetNames.ContainsKey(Enum.Value.ParentModuleName))
            {
                if (ImportTargetNames[Enum.Value.ParentModuleName] != null)
                {
                    if (!ImportTargetNames[Enum.Value.ParentModuleName].Contains(Enum.Key))
                    {
                        ((EnumNode)Imported[Enum.Key]).IsPublic = false;
                    }
                }
            }
        }

        foreach (var record in recievedRecords)
        {
            if (!Imported.ContainsKey(record.Key))
                Imported.Add(record.Key, record.Value);
            if (recievedExports.ContainsKey(record.Key))
            {
                ((RecordNode)Imported[record.Key]).IsPublic = true;
                continue;
            }

            if (ImportTargetNames.ContainsKey(record.Value.ParentModuleName))
            {
                if (ImportTargetNames[record.Value.ParentModuleName] != null)
                {
                    if (!ImportTargetNames[record.Value.ParentModuleName].Contains(record.Key))
                    {
                        ((RecordNode)Imported[record.Key]).IsPublic = false;
                    }
                }
            }
        }
    }

    public void UpdateExports()
    {
        foreach (var exportName in ExportTargetNames)
        {
            if (Functions.ContainsKey(exportName))
            {
                Exported.Add(exportName, Functions[exportName]);
            }
            else if (Enums.ContainsKey(exportName))
            {
                Exported.Add(exportName, Enums[exportName]);
            }
            else if (Records.ContainsKey(exportName))
            {
                Exported.Add(exportName, Records[exportName]);
            }
            else
            {
                throw new Exception(
                    "Could not find '"
                        + exportName
                        + "' in the current list of functions, enums or records in module "
                        + Name
                );
            }
        }
    }

    //merges two unnamed modules into one
    public void mergeModule(ModuleNode moduleIn)
    {
        foreach (var function in moduleIn.getFunctions())
        {
            Functions.Add(function.Key, function.Value);
        }

        if (moduleIn.getImportNames().Any())
            throw new Exception("An unnamed module cannot import.");
        if (moduleIn.getExportNames().Any())
            throw new Exception("An unnamed module cannot export.");
    }

    public void MergeModule(ModuleNode m)
    {
        m.Functions.ToList()
            .ForEach(kvp =>
            {
                if (!Functions.TryAdd(kvp.Key, kvp.Value))
                {
                    throw new InvalidOperationException(
                        "Default module already contains a function named " + kvp.Key
                    );
                }
            });
    }

    /// <summary>
    ///     <para> 
    ///         Method <c>addFunction</c> adds a function to a ModuleNode if it is present
    ///     </para>
    /// </summary>
    /// <param name="function">The function passed in</param>
    public void addFunction(CallableNode? function)
    {
        if (function != null)
        {
            Functions.Add(function.Name, function);
        }
    }

    public void AddRecord(RecordNode? record)
    {
        if (record is not null)
        {
            Records.Add(record.Name, record);
        }
    }

    public void addEnum(EnumNode? enumNode)
    {
        if (enumNode is not null)
        {
            Enums.Add(enumNode.TypeName, enumNode);
        }
    }

    public void addExportName(string? name)
    {
        ExportTargetNames.AddLast(name);
    }

    /// <summary>
    ///     Method <c>addExportNames</c> takes a list of exports and adds it to our ModuleNode
    /// </summary>
    /// <param name="names">List of export names</param>

    public void addExportNames(LinkedList<string> names)
    {
        foreach (var name in names)
        {
            ExportTargetNames.AddLast(name);
        }
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="name"></param>
    public void addImportName(string? name)
    {
        ImportTargetNames.Add(name, new LinkedList<string>());
    }

    public void addImportNames(string moduleName, LinkedList<string> functions)
    {
        ImportTargetNames.Add(moduleName, functions);
    }

    public LinkedList<string> getExportNames()
    {
        return ExportTargetNames;
    }

    public Dictionary<string, LinkedList<string>> getImportNames()
    {
        return ImportTargetNames;
    }

    public Dictionary<string, ASTNode?> getExportedFunctions()
    {
        return Exported;
    }

    public Dictionary<string, ASTNode?> getImportedFunctions()
    {
        return Imported;
    }

    public CallableNode? getFunction(string name)
    {
        if (Functions.ContainsKey(name))
            return Functions[name];
        else
            return null;
    }

    public Dictionary<string, CallableNode> getFunctions()
    {
        return Functions;
    }

    public Dictionary<string, EnumNode> getEnums()
    {
        return Enums;
    }

    public string getName()
    {
        return Name;
    }

    public void setName(string nameIn)
    {
        Name = nameIn;
    }

    public void addTest(TestNode t)
    {
        Tests.Add(t.Name, t);
    }

    public Dictionary<string, TestNode> getTests()
    {
        return Tests;
    }

    // public void VisitStatement(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     // this happens after visiting the prototypes
    //     // TODO: compile types
    //
    //
    //     // first we tell the context that were the current module
    //     context.SetCurrentModule(Name);
    //     // then we add to our scope all our imports
    //     foreach (var (moduleName, imports) in ImportTargetNames)
    //     {
    //         var shankModule = context.Modules[moduleName];
    //         foreach (var import in imports)
    //         {
    //             // TODO: type imports
    //             if (shankModule.Functions.TryGetValue(import, out var function))
    //             {
    //                 context.AddFunction(import, function);
    //             }
    //         }
    //     }
    //     GetFunctionsAsList().ForEach(f => f.Visit(visitor, context, builder, module));
    // }

    public void VisitPrototype(Context context, LLVMModuleRef module)
    {
        // TODO: compile types
        // generate function prototypes
        context.SetCurrentModule(Name);
        GetFunctionsAsList().ForEach(f => f.VisitPrototype(context, module));
    }

    public void VisitProto(VisitPrototype visitPrototype)
    {
        visitPrototype.Accept(this);
    }

    public void Visit(StatementVisitor visit)
    {
        visit.Accept(this);
    }

    public override void Accept<T>(StatementVisitor v)
    {
        throw new NotImplementedException();
    }

    public override void Accept(Visitor v) => v.Visit(this);
}
