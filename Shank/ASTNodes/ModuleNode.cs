using Shank.ExprVisitors;
using Shank.WalkCompliantVisitors;

namespace Shank.ASTNodes;

public class ModuleNode : ASTNode
{
    public Dictionary<string, EnumNode> Enums { get; set; }
    public string Name { get; set; }
    public Dictionary<string, CallableNode> Functions { get; set; }
    public Dictionary<string, List<CallableNode>> Functions2 { get; } = []; //not finished for overloaded functions
    public Dictionary<string, RecordNode> Records { get; set; }
    public Dictionary<string, VariableDeclarationNode> GlobalVariables { get; set; } = [];

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

    public Dictionary<string, CallableNode> OriginalFunctions { get; set; }

    public Dictionary<string, CallableNode> BuiltIns { get; set; }

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
        OriginalFunctions = Functions;
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

    public void UpdateImports(
        Dictionary<string, CallableNode> receivedFunctions,
        Dictionary<string, EnumNode> receivedEnums,
        Dictionary<string, RecordNode> receivedRecords,
        Dictionary<string, ASTNode?> receivedExports
    )
    {
        foreach (var functionKvp in receivedFunctions)
        {
            // Here we're adding to 'Imported' things that are not necessarily exports, which is a little confusing.
            if (!Imported.TryAdd(functionKvp.Key, functionKvp.Value))
                throw new SemanticErrorException("Name conflict: " + functionKvp.Key);
            if (receivedExports.ContainsKey(functionKvp.Key))
            {
                functionKvp.Value.IsPublic = true;
                continue;
            }

            if (ImportTargetNames.ContainsKey(functionKvp.Value.parentModuleName))
            {
                if (ImportTargetNames[functionKvp.Value.parentModuleName] != null)
                {
                    if (
                        !ImportTargetNames[functionKvp.Value.parentModuleName].Contains(
                            functionKvp.Key
                        )
                    )
                    {
                        ((CallableNode)Imported[functionKvp.Key]).IsPublic = false;
                    }
                }
            }
        }

        foreach (var Enum in receivedEnums)
        {
            if (!Imported.ContainsKey(Enum.Key))
                Imported.Add(Enum.Key, Enum.Value);
            if (receivedExports.ContainsKey(Enum.Key))
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

        foreach (var record in receivedRecords)
        {
            if (!Imported.ContainsKey(record.Key))
                Imported.Add(record.Key, record.Value);
            if (receivedExports.ContainsKey(record.Key))
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
    public void addFunction(FunctionNode function)
    {
        var originalFunction = Functions.GetValueOrDefault(function.Name);
        switch (originalFunction)
        {
            case FunctionNode f:
                f.SetOverload();
                function.SetOverload();
                var dictionary = new Dictionary<TypeIndex, CallableNode>
                {
                    [f.Overload] = f,
                    [function.Overload] = function,
                };
                Functions.Remove(f.Name);
                Functions.Add(
                    function.Name,
                    new OverloadedFunctionNode(function.Name, function.parentModuleName, dictionary)
                );
                break;
            case OverloadedFunctionNode f:
                function.SetOverload();
                f.Overloads[function.Overload] = function;
                break;
            case null:
                Functions.Add(function.Name, function);
                break;
        }
    }

    /// <summary>
    ///     <para>
    ///         Method <c>AddRecord</c> adds a record to our Dictionary of records in ModuleNode which includes the name and contents of the record
    ///     </para>
    /// </summary>
    /// <param name="record">The contents of the record</param>

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
    ///     <para>
    ///         Method <c>addImportName</c> adds an import to a dictionary in ModuleNode.
    ///         Since a list of functions is not present, it is left as an empty list of type "string"
    ///     </para>
    /// </summary>
    /// <param name="name">The import's name</param>
    public void addImportName(string? name)
    {
        ImportTargetNames.Add(name, new LinkedList<string>());
    }

    /// <summary>
    ///     <para>
    ///         Method <c>addImportNames</c> adds an import to a dictionary in ModuleNode along with its list of functions
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the import</param>
    /// <param name="functions">The list of corresponding functions</param>

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

    public override void Accept(Visitor v) => v.Visit(this);

    public override ASTNode Walk(WalkCompliantVisitor v)
    {
        var ret = v.Visit(this, out var shortCircuit);
        if (shortCircuit)
        {
            return ret;
        }

        Enums = Enums.WalkDictionary(v);
        GlobalVariables = GlobalVariables.WalkDictionary(v);
        Functions = Functions.WalkDictionary(v);
        Records = Records.WalkDictionary(v);
        Exported = Exported.WalkDictionary(v);
        Imported = Imported.WalkDictionary(v);
        Tests = Tests.WalkDictionary(v);

        return v.Final(this);
    }

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;

        // foreach (var function in Functions)
        // {
        //     Functions[function.Key] = (CallableNode)(
        //         Functions[function.Key].Walk(v) ?? Functions[function.Key]
        //     );
        // }

        foreach (var record in Records.Keys)
        {
            Records[record] = (RecordNode)(Records[record].Walk(v) ?? Records[record]);
        }

        foreach (var function in OriginalFunctions)
        {
            OriginalFunctions[function.Key] = (CallableNode)(
                OriginalFunctions[function.Key].Walk(v) ?? OriginalFunctions[function.Key]
            );
        }

        return v.PostWalk(this);
    }
}
