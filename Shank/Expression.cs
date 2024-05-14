using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using LLVMSharp.Interop;

//To compile to RISC-V: llc -march=riscv64 output_ir_3.ll -o out3.s

namespace Shank;

[JsonDerivedType(typeof(StringNode))]
[JsonDerivedType(typeof(IntNode))]
[JsonDerivedType(typeof(FloatNode))]
[JsonDerivedType(typeof(BoolNode))]
[JsonDerivedType(typeof(CharNode))]
[JsonDerivedType(typeof(VariableReferenceNode))]
[JsonDerivedType(typeof(MathOpNode))]
[JsonDerivedType(typeof(BooleanExpressionNode))]
[JsonDerivedType(typeof(StatementNode))]
[JsonDerivedType(typeof(FunctionNode))]
public abstract class ASTNode
{
    public string NodeName { get; init; }
    public string InheritsDirectlyFrom { get; init; }
    public int Line { get; init; }

    public enum BooleanExpressionOpType
    {
        lt,
        le,
        gt,
        ge,
        eq,
        ne
    }

    public enum MathOpType
    {
        plus,
        minus,
        times,
        divide,
        modulo
    }

    public enum VrnExtType
    {
        RecordMember,
        ArrayIndex,
        Enum,
        None
    }

    protected ASTNode()
    {
        NodeName = GetType().Name;
        InheritsDirectlyFrom = GetType().BaseType?.Name ?? "None";
        Line = Parser.Line;
    }
}

public class FunctionCallNode : StatementNode
{
    public string Name { get; set; }
    public int LineNum { get; set; }
    public List<ParameterNode> Parameters { get; } = [];
    public string OverloadNameExt { get; set; } = "";

    public FunctionCallNode(string name)
    {
        Name = name;
    }

    public bool EqualsWrtNameAndParams(
        CallableNode givenFunction,
        Dictionary<string, VariableNode> variablesInScope
    )
    {
        // If the names don't match, it's not a match.
        if (!givenFunction.Name.Equals(Name))
        {
            return false;
        }

        // If the param counts don't match, it's not a match.
        if (givenFunction.ParameterVariables.Count != Parameters.Count)
        {
            return false;
        }

        // If there's any parameter whose type and 'var' status would disqualify the given
        // function from matching this call, return false, otherwise true.
        return !Parameters
            .Where(
                (p, i) =>
                    !p.EqualsWrtTypeAndVar(givenFunction.ParameterVariables[i], variablesInScope)
            )
            .Any();
    }

    public override object[] returnStatementTokens()
    {
        var b = new StringBuilder();
        if (Parameters.Any())
        {
            Parameters.ForEach(p => b.AppendLine($"   {p}"));
        }
        object[] arr = { "FUNCTION", Name, b.ToString() };
        return arr;
    }

    public override string ToString()
    {
        var b = new StringBuilder();
        b.AppendLine($"Function {Name}:");
        if (Parameters.Any())
        {
            b.AppendLine("Parameters:");
            Parameters.ForEach(p => b.AppendLine($"   {p}"));
        }

        return b.ToString();
    }
}

/**
 * ParameterNodes are the arguments passed to a FunctionCallNode.
 * For the parameters of FunctionNodes, see VariableNode.
 */
public class ParameterNode : ASTNode
{
    public ParameterNode(ASTNode constant)
    {
        IsVariable = false;
        Variable = null;
        Constant = constant;
    }

    /**
     * If IsVariable is true, it means the param was preceded by the "var" keyword when it was
     * passed in, meaning it can be changed in the function, and its new value will persist in
     * the caller's scope.
     */
    public ParameterNode(VariableReferenceNode variable, bool isVariable)
    {
        IsVariable = isVariable;
        Variable = variable;
        Constant = null;
    }

    public ASTNode? Constant { get; init; }
    public VariableReferenceNode? Variable { get; init; }
    public bool IsVariable { get; init; }

    public VariableReferenceNode GetVariableSafe() =>
        Variable ?? throw new InvalidOperationException("Expected Variable to not be null");

    public ASTNode GetConstantSafe() =>
        Constant ?? throw new InvalidOperationException("Expected Constant to not be null");

    // Original (bad) approach for implementing overloads. See 'EqualsWrtTypeAndVar' for
    // revised approach.
    public string ToStringForOverload(
        Dictionary<string, ASTNode> imports,
        Dictionary<string, RecordNode> records,
        Dictionary<string, VariableNode> variables
    ) =>
        "_"
        + (IsVariable ? "VAR_" : "")
        + (
            // TODO
            // If GetSpecificType would give us a user-created type, it should be Unknown, and then
            // we need to resolve that to its specific string.
            Variable
                ?.GetSpecificType(records, imports, variables, Variable.Name)
                .ToString()
                .ToUpper()
            ?? Parser
                .GetDataTypeFromConstantNodeType(
                    Constant
                        ?? throw new InvalidOperationException(
                            "A ParameterNode should not have both Variable and Constant set to"
                                + " null."
                        )
                )
                .ToString()
                .ToUpper()
        );

    /// <summary>
    /// Ensure this ParameterNode's invariants hold.
    /// </summary>
    /// <exception cref="InvalidOperationException">If this ParameterNode is in an invalid state
    /// </exception>
    /// <remarks>Author: Tim Gudlewski</remarks>
    private void ValidateState()
    {
        if (
            (Variable is not null && Constant is not null) || (Variable is null && Constant is null)
        )
        {
            throw new InvalidOperationException(
                "This ParameterNode is in an undefined state because Constant and Variable are"
                    + " both null, or both non-null."
            );
        }

        if (Constant is not null && IsVariable)
        {
            throw new InvalidOperationException(
                "This ParameterNode is in an undefined state because its value is stored in"
                    + " Constant, but it is also 'var'."
            );
        }
    }

    public bool ValueIsStoredInVariable()
    {
        ValidateState();
        return Variable is not null;
    }

    public bool EqualsWrtTypeAndVar(
        VariableNode givenVariable,
        Dictionary<string, VariableNode> variablesInScope
    )
    {
        return ValueIsStoredInVariable()
            ? VarStatusEquals(givenVariable) && VariableTypeEquals(givenVariable, variablesInScope)
            : ConstantTypeEquals(givenVariable);
    }

    public bool VarStatusEquals(VariableNode givenVariable)
    {
        return IsVariable != givenVariable.IsConstant;
    }

    public bool ConstantTypeEquals(VariableNode givenVariable)
    {
        return givenVariable.Type == GetConstantType();
    }

    public VariableNode.DataType GetConstantType()
    {
        return Parser.GetDataTypeFromConstantNodeType(GetConstantSafe());
    }

    public bool VariableTypeEquals(
        VariableNode givenVariable,
        Dictionary<string, VariableNode> variablesInScope
    )
    {
        // Check if the types are unequal.
        if (givenVariable.Type != GetVariableType(variablesInScope))
        {
            return false;
        }

        // The types are equal. If they're not Unknown, no further action needed.
        if (givenVariable.Type != VariableNode.DataType.Unknown)
        {
            return true;
        }

        // The types are equal and Unknown. Check their UnknownTypes (string comparison).
        return givenVariable
            .GetUnknownTypeSafe()
            .Equals(GetVariableDeclarationSafe(variablesInScope).GetUnknownTypeSafe());
    }

    public VariableNode GetVariableDeclarationSafe(
        Dictionary<string, VariableNode> variablesInScope
    )
    {
        if (variablesInScope.TryGetValue(GetVariableSafe().Name, out var varDec))
        {
            return varDec;
        }

        throw new InvalidOperationException("Could not find given variable in scope");
    }

    public VariableNode.DataType GetVariableType(Dictionary<string, VariableNode> variablesInScope)
    {
        return GetVariableDeclarationSafe(variablesInScope).Type;
    }

    public override string ToString()
    {
        if (Variable != null)
            return $"   {(IsVariable ? "var " : "")} {Variable.Name}";
        else
            return $"   {Constant}";
    }
}

public class IntNode : ASTNode
{
    public IntNode(int value)
    {
        Value = value;
    }

    public int Value { get; set; }

    public override string ToString()
    {
        return $"{Value}";
    }
}

public class FloatNode : ASTNode
{
    public FloatNode(float value)
    {
        Value = value;
    }

    public float Value { get; set; }

    public override string ToString()
    {
        return $"{Value}";
    }
}

public class BoolNode : ASTNode
{
    public BoolNode(bool value)
    {
        Value = value;
    }

    public bool Value { get; set; }

    public override string ToString()
    {
        return $"{Value}";
    }
}

public class CharNode : ASTNode
{
    public CharNode(char value)
    {
        Value = value;
    }

    public char Value { get; set; }

    public override string ToString()
    {
        return $"{Value}";
    }
}

public class StringNode : ASTNode
{
    public StringNode(string value)
    {
        Value = value;
    }

    public string Value { get; set; }

    public override string ToString()
    {
        return $"{Value}";
    }
}

[JsonDerivedType(typeof(FunctionNode))]
[JsonDerivedType(typeof(BuiltInFunctionNode))]
public abstract class CallableNode : ASTNode
{
    public string Name { get; set; }

    public string? parentModuleName { get; set; }

    public bool IsPublic { get; set; }

    public int LineNum { get; set; }

    public List<VariableNode> ParameterVariables { get; } = [];

    protected CallableNode(string name)
    {
        Name = name;
        IsPublic = false;
    }

    protected CallableNode(string name, string moduleName)
    {
        Name = name;
        parentModuleName = moduleName;
        IsPublic = false;
    }

    protected CallableNode(string name, BuiltInCall execute)
    {
        Name = name;
        Execute = execute;
        IsPublic = false;
    }

    protected CallableNode(string name, string moduleName, bool isPublicIn)
    {
        Name = name;
        parentModuleName = moduleName;
        IsPublic = isPublicIn;
    }

    public delegate void BuiltInCall(List<InterpreterDataType> parameters);
    public BuiltInCall? Execute;

    public bool IsValidOverloadOf(CallableNode cn) =>
        ParameterVariables.Where((pv, i) => !cn.ParameterVariables[i].EqualsForOverload(pv)).Any();
}

public class BuiltInFunctionNode : CallableNode
{
    public BuiltInFunctionNode(string name, BuiltInCall execute)
        : base(name, execute) { }

    public bool IsVariadic = false;
}

public class TestNode : FunctionNode
{
    public string targetFunctionName;
    public List<VariableNode> testingFunctionParameters = new();

    public TestNode(string name, string targetFnName)
        : base(name)
    {
        Name = name;
        targetFunctionName = targetFnName;
        IsPublic = false;
        Execute = (List<InterpreterDataType> paramList) =>
            Interpreter.InterpretFunction(this, paramList);
    }
}

public class FunctionNode : CallableNode
{
    public FunctionNode(string name, string moduleName, bool isPublic)
        : base(name, moduleName, isPublic)
    {
        // This is a delegate instance, like an anonymous interface implementation in Java.
        Execute = (List<InterpreterDataType> paramList) =>
            Interpreter.InterpretFunction(this, paramList);
    }

    public FunctionNode(string name, string moduleName)
        : base(name, moduleName)
    {
        Execute = (List<InterpreterDataType> paramList) =>
            Interpreter.InterpretFunction(this, paramList);
    }

    public FunctionNode(string name)
        : base(name)
    {
        Execute = (List<InterpreterDataType> paramList) =>
            Interpreter.InterpretFunction(this, paramList);
    }

    public string OverloadNameExt { get; set; } = "";

    public List<VariableNode> LocalVariables { get; set; } = [];

    public List<StatementNode> Statements { get; set; } = [];

    public Dictionary<string, TestNode> Tests { get; set; } = [];

    public List<string>? GenericTypeParameterNames { get; set; }

    public void ApplyActionToTests(
        Action<TestNode, List<InterpreterDataType>, ModuleNode?> action,
        ModuleNode module
    )
    {
        Tests.ToList().ForEach(testKvp => action(testKvp.Value, [], module));
    }

    public VariableNode GetVariableNodeByName(string searchName)
    {
        return LocalVariables
                .Concat(ParameterVariables)
                .FirstOrDefault(
                    vn =>
                        (
                            vn
                            ?? throw new InvalidOperationException(
                                "Something went wrong internally. There should not be"
                                    + " null entries in FunctionNode.LocalVariables or"
                                    + " FunctionNode.ParameterVariables."
                            )
                        ).Name?.Equals(searchName)
                        ?? throw new InvalidOperationException(vn + " has no Name."),
                    null
                )
            ?? throw new ArgumentOutOfRangeException(
                nameof(searchName),
                "No variable found with given searchName."
            );
    }

    public override string ToString()
    {
        var b = new StringBuilder();
        b.AppendLine($"Function {Name}:");
        if (ParameterVariables.Any())
        {
            b.AppendLine("Parameters:");
            ParameterVariables.ForEach(p => b.AppendLine($"   {p}"));
        }
        if (LocalVariables.Any())
        {
            b.AppendLine("Local Variables:");
            LocalVariables.ForEach(p => b.AppendLine($"   {p}"));
        }
        if (Statements.Any())
        {
            b.AppendLine("-------------------------------------");
            Statements.ForEach(p => b.AppendLine($"   {p}"));
            b.AppendLine("-------------------------------------");
        }

        return b.ToString();
    }

    /*
    For assignment statement
    EvalExpression() is called if the Right Hand Side is an expression.
    */
    public Dictionary<string, LLVMValueRef> Exec_Assignment(
        LLVMBuilderRef builder,
        Dictionary<string, LLVMValueRef> hash_variables,
        object[] s_tokens,
        LLVMContextRef context
    )
    {
        string[] rhsTokens = ((string)s_tokens[2]).Split(' ');

        LLVMValueRef allocated_right = EvalExpression(builder, hash_variables, rhsTokens, context);

        var allocated_left = hash_variables[(string)s_tokens[1]];

        builder.BuildStore(allocated_right, allocated_left);
        hash_variables[(string)s_tokens[1]] = allocated_left;

        return hash_variables;
    }

    /*
    When write() function is called
    */
    public void Exec_Function(
        LLVMBuilderRef builder,
        Dictionary<string, LLVMValueRef> hash_variables,
        object[] s_tokens,
        LLVMContextRef context,
        LLVMTypeRef writeFnTy,
        LLVMValueRef writeFn
    )
    {
        var allocated = builder.BuildLoad2(
            context.Int64Type,
            hash_variables[((string)s_tokens[2]).Trim()]
        ); //ex. s_tokens[2] is prev1 and allocated is the stored value of prev1
        builder.BuildCall2(
            writeFnTy,
            writeFn,
            new LLVMValueRef[] { allocated },
            (s_tokens[1]).ToString()
        );
    }

    /*
    This function is used to handle recursive case such that any expression can be assigned to a variable.
    */
    private LLVMValueRef EvalExpression(
        LLVMBuilderRef builder,
        Dictionary<string, LLVMValueRef> hash_variables,
        string[] tokens,
        LLVMContextRef context
    )
    {
        //Base case: if the tokens array has only one element, it's either a variable or a constant
        if (tokens.Length == 1)
        {
            if (hash_variables.ContainsKey(tokens[0])) // Variable
            {
                return builder.BuildLoad2(context.Int64Type, hash_variables[tokens[0]]);
            }
            else // Constant
            {
                return LLVMValueRef.CreateConstInt(
                    context.Int64Type,
                    ulong.Parse(tokens[0]),
                    false
                );
            }
        }
        else //Recursive case: evaluate the first operand, then the rest of the expression
        {
            LLVMValueRef firstOperand = EvalExpression(
                builder,
                hash_variables,
                new string[] { tokens[0] },
                context
            );
            string operation = tokens[1];
            LLVMValueRef secondOperand = EvalExpression(
                builder,
                hash_variables,
                tokens.Skip(2).ToArray(),
                context
            );

            switch (operation)
            {
                case "plus":
                    return builder.BuildAdd(firstOperand, secondOperand, "plus");
                case "minus":
                    return builder.BuildSub(firstOperand, secondOperand, "minus");
                case "times":
                    return builder.BuildMul(firstOperand, secondOperand, "times");
                case "divide":
                    return builder.BuildSDiv(firstOperand, secondOperand, "divide");
                default:
                    throw new ArgumentException($"Some unknown operation: {operation}");
            }
        }
    }

    public Dictionary<string, LLVMValueRef> Exec_For(
        LLVMBuilderRef builder,
        Dictionary<string, LLVMValueRef> hash_variables,
        object[] s_tokens,
        LLVMContextRef context,
        LLVMTypeRef writeFnTy,
        LLVMValueRef writeFn,
        LLVMValueRef mainFn
    )
    {
        /*
        s_tokens[0]: For
        s_tokens[1]: i
        s_tokens[2]: start
        s_tokens[3]: end
        s_tokens[4]: entire for loop body
        for i from start to end
        */

        //initialize i to start
        var i = hash_variables[s_tokens[1].ToString()]; //variable that will be assigned with a value, ex. i

        //if variable is being assigned
        if (hash_variables.ContainsKey(s_tokens[2].ToString()))
        {
            var start = builder.BuildLoad2(
                context.Int64Type,
                hash_variables[s_tokens[2].ToString()]
            ); //value to be assigned, ex. start
            builder.BuildStore(start, i); //store i with a value of start
        }
        else //if number is being assigned
        {
            var start = LLVMValueRef.CreateConstInt(
                context.Int64Type,
                ulong.Parse(s_tokens[2].ToString()),
                false
            );
            builder.BuildStore(start, i); //store i with a value of start
        }
        hash_variables[s_tokens[1].ToString()] = i;

        //Create the for loop condition
        var loopCondBlock = mainFn.AppendBasicBlock("for.condition");
        builder.BuildBr(loopCondBlock);

        //Create the for loop body
        var loopBodyBlock = mainFn.AppendBasicBlock("for.body");
        builder.PositionAtEnd(loopBodyBlock);

        //For loop body contains statements. So like before, go through each statement
        List<StatementNode> for_statements = (List<StatementNode>)s_tokens[4];

        foreach (var statement in for_statements)
        {
            object[] for_tokens = statement.returnStatementTokens();
            if (for_tokens[0] == "") // Assignment statement
            {
                string[] tokens = ((string)for_tokens[2].ToString()).Split(' ');
                LLVMValueRef allocated_right = EvalExpression(
                    builder,
                    hash_variables,
                    tokens,
                    context
                );
                LLVMValueRef alloca_left = hash_variables[(string)for_tokens[1]];
                builder.BuildStore(allocated_right, alloca_left);
                hash_variables[(string)for_tokens[1]] = alloca_left;
            }
            else if (for_tokens[0] == "FUNCTION") //if it is a function node, such as write()
            {
                Exec_Function(builder, hash_variables, for_tokens, context, writeFnTy, writeFn);
            }
            else
            {
                hash_variables = Exec_For(
                    builder,
                    hash_variables,
                    s_tokens,
                    context,
                    writeFnTy,
                    writeFn,
                    mainFn
                );
            }
        }

        // Increment 'i'
        var increment = LLVMValueRef.CreateConstInt(context.Int64Type, 1, false);
        i = builder.BuildLoad2(context.Int64Type, hash_variables[s_tokens[1].ToString()]);
        var incrementedValue = builder.BuildAdd(i, increment, "i");
        var allocated_left = hash_variables[s_tokens[1].ToString()];
        builder.BuildStore(incrementedValue, allocated_left);
        hash_variables[s_tokens[1].ToString()] = allocated_left; //we store the pointer variable referencing the memory location for i

        // Go back to the loop condition block, i.e. branch
        builder.BuildBr(loopCondBlock);

        builder.PositionAtEnd(loopCondBlock);

        var loopCond = builder.BuildICmp(
            LLVMIntPredicate.LLVMIntSLT,
            builder.BuildLoad2(context.Int64Type, hash_variables[s_tokens[1].ToString()]),
            builder.BuildLoad2(context.Int64Type, hash_variables[s_tokens[3].ToString()]),
            "loop.condition.cmp"
        );
        builder.BuildCondBr(loopCond, loopBodyBlock, mainFn.AppendBasicBlock("exit"));

        // Build the exit block
        builder.PositionAtEnd(mainFn.LastBasicBlock);

        return hash_variables;
    }

    public Dictionary<string, LLVMValueRef> Exec_While(
        LLVMBuilderRef builder,
        Dictionary<string, LLVMValueRef> hash_variables,
        object[] s_tokens,
        LLVMContextRef context,
        LLVMTypeRef writeFnTy,
        LLVMValueRef writeFn,
        LLVMValueRef mainFn
    )
    {
        // Create the condition and body basic blocks
        var conditionBlock = mainFn.AppendBasicBlock("while.cond");
        var bodyBlock = mainFn.AppendBasicBlock("while.body");
        var endBlock = mainFn.AppendBasicBlock("while.end");

        // Branch to the condition block from the current position
        builder.BuildBr(conditionBlock);

        // Generate the condition
        builder.PositionAtEnd(conditionBlock);

        var leftOpValue = LLVMValueRef.CreateConstInt(context.Int64Type, 0, false);
        var rightOpValue = LLVMValueRef.CreateConstInt(context.Int64Type, 0, false);

        // Check if the left operand is a variable or a number
        if (hash_variables.ContainsKey(s_tokens[1].ToString()))
            leftOpValue = builder.BuildLoad2(
                context.Int64Type,
                hash_variables[s_tokens[1].ToString()]
            );
        else if (ulong.TryParse(s_tokens[1].ToString(), out ulong leftOpNum))
            leftOpValue = LLVMValueRef.CreateConstInt(context.Int64Type, leftOpNum, false);

        // Check if the right operand is a variable or a number
        if (hash_variables.ContainsKey(s_tokens[3].ToString()))
            rightOpValue = builder.BuildLoad2(
                context.Int64Type,
                hash_variables[s_tokens[3].ToString()]
            );
        else if (ulong.TryParse(s_tokens[3].ToString(), out ulong rightOpNum))
            rightOpValue = LLVMValueRef.CreateConstInt(context.Int64Type, rightOpNum, false);

        var condition = builder.BuildICmp(
            LLVMIntPredicate.LLVMIntSLT,
            leftOpValue,
            rightOpValue,
            "cond"
        );
        builder.BuildCondBr(condition, bodyBlock, endBlock);

        // Generate the body
        builder.PositionAtEnd(bodyBlock);
        List<StatementNode> while_statements = (List<StatementNode>)s_tokens[4];
        /*
        Visit every statements within while loop
        */
        foreach (var statement in while_statements)
        {
            var ws_tokens = statement.returnStatementTokens();
            if (ws_tokens[0] == "")
            {
                hash_variables = Exec_Assignment(builder, hash_variables, ws_tokens, context);
            }
            else if (ws_tokens[0] == "FUNCTION")
            {
                Exec_Function(builder, hash_variables, ws_tokens, context, writeFnTy, writeFn);
            }
            else if (ws_tokens[0] == "FOR")
            {
                hash_variables = Exec_For(
                    builder,
                    hash_variables,
                    ws_tokens,
                    context,
                    writeFnTy,
                    writeFn,
                    mainFn
                );
            }
            else if (ws_tokens[0] == "WHILE")
            {
                hash_variables = Exec_While(
                    builder,
                    hash_variables,
                    ws_tokens,
                    context,
                    writeFnTy,
                    writeFn,
                    mainFn
                );
            }
        }

        //increment the condition variable for the while loop
        var increment = LLVMValueRef.CreateConstInt(context.Int64Type, 1, false);
        var incremented = builder.BuildAdd(increment, leftOpValue, "incre");
        var allocated_left = hash_variables[s_tokens[1].ToString()];
        builder.BuildStore(incremented, allocated_left);
        hash_variables[s_tokens[1].ToString()] = allocated_left;

        builder.BuildBr(conditionBlock);

        builder.PositionAtEnd(endBlock);

        return hash_variables;
    }

    public void LLVMCompile()
    {
        // Setup context, module, and builder

        // "When declared in a using declaration, a local variable is disposed
        // at the end of the scope in which it's declared"
        // ~ https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using

        // "LLVMContextRef: The top-level container for all LLVM global data."
        // ~ https://llvm.org/doxygen/group__LLVMCSupportTypes.html#ga9c43e01525516ff6b4feab5166c5b1da
        using var context = LLVMContextRef.Create();

        // "LLVMModuleRef: The top-level container for all other LLVM Intermediate Representation (IR) objects."
        // ~ https://llvm.org/doxygen/group__LLVMCSupportTypes.html#gad1d1bb5f901c903a0cf09c5a053c9c56
        using var module = context.CreateModuleWithName("main");

        // "A basic block is simply a container of instructions that execute sequentially."
        // ~ https://llvm.org/doxygen/classllvm_1_1BasicBlock.html#details
        // "LLVMBuilderRef: Represents an LLVM basic block builder."
        // ~ https://llvm.org/doxygen/group__LLVMCSupportTypes.html#gab13eecdec39366f9974f865d68011775
        using var builder = context.CreateBuilder();

        // Create the write() function

        // Get write() return type
        var writeFnRetTy = context.Int64Type;
        // Get write() param types
        var writeFnParamTys = new[] { context.Int64Type };
        // Get write() function type by combining return and param types
        var writeFnTy = LLVMTypeRef.CreateFunction(writeFnRetTy, writeFnParamTys);
        // Add write() function to module and save the function to a variable
        var writeFn = module.AddFunction("write", writeFnTy);

        // Create the main (or the start()) function
        // For void return, use context.VoidType

        // Get start() return type
        var mainRetTy = context.Int64Type;
        // Get start() param types
        var mainParamTys = Array.Empty<LLVMTypeRef>();
        // Get start() function type by combining return and param types
        var mainFnTy = LLVMTypeRef.CreateFunction(mainRetTy, mainParamTys);
        // Add start() function to module and save the function to a variable
        var mainFn = module.AddFunction("start", mainFnTy);
        // Add entry block to start() function and save the block to a variable
        var mainBlock = mainFn.AppendBasicBlock("entry");

        // Create the body of the main function
        builder.PositionAtEnd(mainBlock);

        // Use this for pointer type:
        // var int64PtrType = LLVMTypeRef.CreatePointer(context.Int64Type, 0)

        // Store local variables in memory with BuildAlloca() and BuildStore()

        // Contents of hash_variables:
        //     If variable IS NOT initialized:
        //         Variable name
        //         Variable with the allocated memory
        //     If variable IS initialized:
        //         Variable name
        //         Variable with the value stored in the allocated memory

        // Purpose of hash_variables:
        //     Needed to use BuildLoad2(), which loads the value of the variable stored in memory
        var hash_variables = new Dictionary<string, LLVMValueRef>();

        LocalVariables
            .Where(localVar => localVar.Type == VariableNode.DataType.Integer)
            .ToList()
            .ForEach(localVar =>
            {
                var allocated = builder.BuildAlloca(context.Int64Type, localVar.Name ?? "");
                allocated.SetAlignment(4);

                // 'if' statement passes if the local variable is declared but not initialized; for example:
                // variables i, j, result : integer
                // 'if' statement fails if the local variable is both declared and initialized; for example:
                // constants begin = 1, end = 5
                if (localVar.InitialValue == null)
                {
                    hash_variables.Add(localVar.Name ?? "", allocated);
                }
                var allocatedValue = LLVMValueRef.CreateConstInt(
                    context.Int64Type,
                    ulong.Parse(localVar.InitialValue?.ToString() ?? "")
                );
                builder.BuildStore(allocatedValue, allocated);

                hash_variables.Add(localVar.Name ?? "", allocated);
            });

        int count = 0;

        //Go through each statement
        foreach (var s in Statements)
        {
            object[] statementTokens = s.returnStatementTokens();

            //if not a function, i.e. if assignment node
            if (statementTokens[0] == "")
            {
                /*
                s_tokens[0]: ""
                s_tokens[1]: target.Name, ex) prev1 in prev1:=start
                s_tokens[2]: expression.ToString(), ex) start
                */

                hash_variables = Exec_Assignment(builder, hash_variables, statementTokens, context);
            }
            else if (statementTokens[0] == "FUNCTION") //if it is a function node, such as write()
            {
                /*
                s_tokens[0]: "FUNCTION"
                s_tokens[1]: Name, ex) write in write prev1
                s_tokens[2]: b.ToString(), ex) prev1
                */

                Exec_Function(
                    builder,
                    hash_variables,
                    statementTokens,
                    context,
                    writeFnTy,
                    writeFn
                );
            }
            else if (statementTokens[0] == "WHILE")
            {
                /*
                s_tokens[0]: "WHILE"
                s_tokens[1]: Expression.Left
                s_tokens[2]: Expression.Op
                s_tokens[3]: Expression.Right
                s_tokens[4]: Children
                */

                hash_variables = Exec_While(
                    builder,
                    hash_variables,
                    statementTokens,
                    context,
                    writeFnTy,
                    writeFn,
                    mainFn
                );
            }
            else //for loop
            {
                /*
                s_tokens[0]: "For"
                s_tokens[1]: Variable
                s_tokens[2]: From
                s_tokens[3]: To
                s_tokens[4]: Children
                */
                hash_variables = Exec_For(
                    builder,
                    hash_variables,
                    statementTokens,
                    context,
                    writeFnTy,
                    writeFn,
                    mainFn
                );
            }

            count++;
            //if we have reached the end of all the statements, then do return 0
            if (count == Statements.Count)
            {
                builder.PositionAtEnd(mainFn.LastBasicBlock);
                builder.BuildRet(LLVMValueRef.CreateConstInt(context.Int64Type, 0, false));
            }
        }

        Console.WriteLine($"LLVM IR\n=========\n{module}");

        //builder.BuildRetVoid();

        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
        LLVM.InitializeAllAsmParsers();
        LLVM.InitializeAllAsmPrinters();

        /*
        ptr -> i64*

        Save generatedIR.ll file. Then, change every ptr occurrence to i64*
        */
        var outPath = Path.Combine(Directory.GetCurrentDirectory(), "generatedIR.ll");
        module.PrintToFile(outPath);
        string irContent = File.ReadAllText(outPath);
        string updatedIrContent = irContent.Replace("ptr", "i64*");
        File.WriteAllText(outPath, updatedIrContent);
    }
}

public class RecordMemberNode : StatementNode
{
    public string Name { get; init; }

    public VariableNode.DataType Type { get; set; }

    public string? UnknownType { get; init; }
    public ASTNode? From { get; set; }
    public ASTNode? To { get; set; }

    public string GetUnknownTypeSafe() =>
        UnknownType ?? throw new InvalidOperationException("Expected UnknownType to not be null.");

    public RecordMemberNode(string name, VariableNode.DataType type)
    {
        Name = name;
        Type = type;
    }

    public RecordMemberNode(string name, string unknownType)
    {
        Name = name;
        Type = VariableNode.DataType.Unknown;
        UnknownType = unknownType;
    }

    public RecordMemberNode(string name, string dataType, string unknownType)
    {
        Name = name;
        Type = VariableNode.DataType.Reference;
        UnknownType = unknownType;
    }

    //public VariableNode.DataType GetTypeResolveUnknown(ModuleNode module)
    //{
    //    return Type != VariableNode.DataType.Unknown
    //        ? Type
    //        : (SemanticAnalysis.GetNamespaceOfRecordsAndEnumsAndImports(module)[GetUnknownTypeSafe()] as RecordNode)?.;
    //}
}

public class RecordNode : ASTNode
{
    public string Name { get; init; }

    public List<string>? GenericTypeParameterNames { get; init; }

    public string? ParentModuleName { get; init; }

    public List<StatementNode> Members { get; init; }
    public bool IsPublic { get; set; }

    public RecordNode(string name, string moduleName, List<string>? genericTypeParameterNames)
    {
        Name = name;
        ParentModuleName = moduleName;
        GenericTypeParameterNames = genericTypeParameterNames;
        Members = [];
        IsPublic = false;
    }

    public static RecordMemberNode ToMember(StatementNode? sn) =>
        (RecordMemberNode)(
            sn
            ?? throw new ArgumentNullException(nameof(sn), "Expected StatementNode to not be null")
        );

    public RecordMemberNode? GetFromMembersByName(string name) =>
        (RecordMemberNode?)Members.FirstOrDefault(s => ToMember(s).Name.Equals(name), null);

    public RecordMemberNode GetFromMembersByNameSafe(string name) =>
        GetFromMembersByName(name)
        ?? throw new ArgumentOutOfRangeException(
            nameof(name),
            "Member " + name + " not found on record."
        );

    public string GetParentModuleSafe()
    {
        return ParentModuleName ?? throw new Exception("Parent module name of RecordNode is null.");
    }
}

public class EnumNode : ASTNode
{
    public string Type { get; set; }
    public string ParentModuleName { get; set; }
    public LinkedList<String> EnumElements;
    public bool IsPublic { get; set; }

    public EnumNode(string type, string parentModuleName, LinkedList<String> enumElements)
    {
        Type = type;
        ParentModuleName = parentModuleName;
        EnumElements = new LinkedList<string>(enumElements);
        IsPublic = false;
    }
}

public class VariableNode : ASTNode
{
    public string? Name { get; set; }
    public string? ModuleName { get; set; }

    public enum DataType
    {
        Real,
        Integer,
        String,
        Character,
        Boolean,
        Array,
        Record,
        Enum,
        Reference,
        Unknown
    };

    public enum UnknownTypeResolver
    {
        Record,
        Enum,
        None,
        Multiple
    };

    public DataType Type { get; set; }

    // If Type is Array, then ArrayType is the type of its elements, or else it is null.
    public DataType? ArrayType { get; set; }

    // UnknownType is the base name of an enum or record.
    // If Type is not Unknown or Enum or Record, then UnknownType should be null.
    public string? UnknownType { get; set; }

    public List<DataType>? GenericTypeArgs { get; set; }

    public bool IsConstant { get; set; }
    public ASTNode? InitialValue { get; set; }
    public ASTNode? From { get; set; }
    public ASTNode? To { get; set; }

    public string GetNameSafe() =>
        Name ?? throw new InvalidOperationException("Expected Name to not be null");

    public DataType GetArrayTypeSafe()
    {
        return ArrayType
            ?? throw new InvalidOperationException("Expected ArrayType to not be null.");
    }

    public string GetUnknownTypeSafe()
    {
        return UnknownType
            ?? throw new InvalidOperationException(
                "Expected " + nameof(UnknownType) + " to not be null."
            );
    }

    public string GetModuleNameSafe() => ModuleName ?? "default";

    public string ToStringForOverloadExt() =>
        "_"
        + (IsConstant ? "" : "VAR_")
        + (Type == DataType.Unknown ? GetUnknownTypeSafe() : Type.ToString().ToUpper());

    public UnknownTypeResolver ResolveUnknownType(ModuleNode parentModule)
    {
        if (
            parentModule.getEnums().ContainsKey(GetUnknownTypeSafe())
            && parentModule.Records.ContainsKey(GetUnknownTypeSafe())
        )
        {
            return UnknownTypeResolver.Multiple;
        }

        if (parentModule.getEnums().ContainsKey(GetUnknownTypeSafe()))
        {
            return UnknownTypeResolver.Enum;
        }

        return parentModule.Records.ContainsKey(GetUnknownTypeSafe())
            ? UnknownTypeResolver.Record
            : UnknownTypeResolver.None;
    }

    /// <summary>
    /// Get this VariableNode's type based on what Extension the VariableReferenceNode that
    /// points to it might have.
    /// </summary>
    /// <param name="parentModule"></param>
    /// <param name="vrn">The VariableReferenceNode that points to this VariableNode</param>
    /// <returns></returns>
    public DataType GetSpecificType(ModuleNode parentModule, VariableReferenceNode vrn) =>
        vrn.ExtensionType switch
        {
            VrnExtType.ArrayIndex => GetArrayTypeSafe(),
            VrnExtType.RecordMember
                => GetRecordMemberType(
                    vrn.GetRecordMemberReferenceSafe().Name,
                    parentModule.Records
                ),
            VrnExtType.None
                => Type switch
                {
                    DataType.Record
                        => throw new NotImplementedException(
                            "It is not implemented yet to assign a record variable base to a target."
                        ),
                    DataType.Array
                        => throw new NotImplementedException(
                            "It is not implemented yet to assign an array variable base to a target."
                        ),
                    _ => Type
                },
            _ => throw new NotImplementedException("Unknown VrnExtType member.")
        };

    public DataType GetRecordMemberType(string memberName, Dictionary<string, RecordNode> records)
    {
        return records[GetUnknownTypeSafe()].GetFromMembersByNameSafe(memberName).Type;
    }

    public bool EqualsForOverload(VariableNode vn)
    {
        return vn.Type == Type && vn.IsConstant == IsConstant;
    }

    public override string ToString()
    {
        return Name
            + " : "
            + (Type == DataType.Array ? "Array of " + ArrayType : Type)
            + " "
            + (IsConstant ? "const" : string.Empty)
            + " "
            + (InitialValue == null ? string.Empty : InitialValue)
            + " "
            + (From == null ? string.Empty : " From: " + From)
            + " "
            + (To == null ? string.Empty : " To: " + To);
    }
}

public class MathOpNode : ASTNode
{
    public MathOpNode(ASTNode left, MathOpType op, ASTNode right)
    {
        Left = left;
        Op = op;
        Right = right;
    }

    public MathOpType Op { get; init; }
    public ASTNode Left { get; init; }
    public ASTNode Right { get; init; }

    public override string ToString()
    {
        return $"{Left.ToString()} {Op} {Right.ToString()}";
    }
}

[JsonDerivedType(typeof(RecordMemberNode))]
[JsonDerivedType(typeof(AssignmentNode))]
[JsonDerivedType(typeof(FunctionCallNode))]
[JsonDerivedType(typeof(IfNode))]
[JsonDerivedType(typeof(ForNode))]
[JsonDerivedType(typeof(WhileNode))]
[JsonDerivedType(typeof(RepeatNode))]
public class StatementNode : ASTNode
{
    protected static string StatementListToString(List<StatementNode> statements)
    {
        var b = new StringBuilder();
        statements.ForEach(c => b.Append("\t" + c));
        return b.ToString();
    }

    public virtual object[] returnStatementTokens()
    {
        object[] arr = { };
        return arr;
    }
}

public class VariableReferenceNode : ASTNode
{
    public VariableReferenceNode(string name)
    {
        Name = name;
        Extension = null;
        ExtensionType = VrnExtType.None;
    }

    public VariableReferenceNode(string name, ASTNode extension, VrnExtType extensionType)
    {
        Name = name;
        Extension = extension;
        ExtensionType = extensionType;
        if (extensionType == VrnExtType.RecordMember)
        {
            ((VariableReferenceNode)Extension).EnclosingVrnName = Name;
        }
    }

    public string Name { get; init; }

    /// <summary>
    /// Represents an extension of the base variable reference (e.g. an array subscript or a record member).
    /// </summary>
    public ASTNode? Extension { get; init; }

    public VrnExtType ExtensionType { get; set; }

    public string? EnclosingVrnName { get; set; }

    public ASTNode GetExtensionSafe() =>
        Extension ?? throw new InvalidOperationException("Expected Extension to not be null.");

    public VariableReferenceNode GetRecordMemberReferenceSafe() =>
        GetExtensionSafe() as VariableReferenceNode
        ?? throw new InvalidOperationException("Expected Extension to be a VariableReferenceNode.");

    public List<string> GetNestedNamesAsList()
    {
        List<string> ret = [Name];
        if (ExtensionType != VrnExtType.RecordMember)
        {
            return ret;
            //throw new InvalidOperationException(
            //    "Don't call this method on a VRN whose ExtensionType is anything other than "
            //        + "RecordMember."
            //);
        }

        if (EnclosingVrnName is not null)
        {
            return ret;
            //throw new InvalidOperationException("Don't call this method on an enclosed VRN.");
        }

        var ext = Extension;
        var extType = ExtensionType;
        while (extType == VrnExtType.RecordMember && ext is not null)
        {
            if (ext is VariableReferenceNode vrn)
            {
                ret.Add(vrn.Name);
                ext = vrn.Extension;
                extType = vrn.ExtensionType;
            }
            else
            {
                throw new InvalidOperationException(
                    "Expected " + ext.NodeName + " to be a VariableReferenceNode."
                );
            }
        }

        return ret;
    }

    public VariableNode.DataType GetSpecificType(
        Dictionary<string, RecordNode> records,
        Dictionary<string, ASTNode> imports,
        Dictionary<string, VariableNode> variables,
        string name
    )
    {
        var recordsAndImports = SemanticAnalysis.GetRecordsAndImports(records, imports);
        return variables[name].Type switch
        {
            VariableNode.DataType.Record
                => ((RecordNode)recordsAndImports[name])
                    .GetFromMembersByNameSafe(GetRecordMemberReferenceSafe().Name)
                    .Type,
            VariableNode.DataType.Array => variables[name].GetArrayTypeSafe(),
            _ => variables[name].Type
        };
    }

    public override string ToString()
    {
        return $"{Name + (Extension != null ? (", Index: " + Extension) : string.Empty)}";
    }
}

public class WhileNode : StatementNode
{
    public WhileNode(BooleanExpressionNode exp, List<StatementNode> children)
    {
        Expression = exp;
        Children = children;
    }

    public BooleanExpressionNode Expression { get; init; }
    public List<StatementNode> Children { get; set; }

    public override object[] returnStatementTokens()
    {
        object[] arr = { "WHILE", Expression.Left, Expression.Op, Expression.Right, Children };

        return arr;
    }

    public override string ToString()
    {
        return $" WHILE: {Expression} {StatementListToString(Children)}";
    }
}

public class RepeatNode : StatementNode
{
    public RepeatNode(BooleanExpressionNode exp, List<StatementNode> children)
    {
        Expression = exp;
        Children = children;
    }

    public BooleanExpressionNode Expression { get; init; }
    public List<StatementNode> Children { get; set; }

    public override string ToString()
    {
        return $" REPEAT: {Expression} {StatementListToString(Children)}";
    }
}

public class IfNode : StatementNode
{
    protected IfNode(List<StatementNode> children)
    {
        Expression = null;
        Children = children;
        NextIfNode = null;
    }

    public IfNode(
        BooleanExpressionNode expression,
        List<StatementNode> children,
        IfNode? nextIfNode = null
    )
    {
        Expression = expression;
        Children = children;
        NextIfNode = nextIfNode;
    }

    public BooleanExpressionNode? Expression { get; init; }
    public List<StatementNode> Children { get; init; }
    public IfNode? NextIfNode { get; init; }

    public override string ToString()
    {
        return $"If: {Expression} {StatementListToString(Children)} {((NextIfNode == null) ? string.Empty : Environment.NewLine + NextIfNode)}";
    }
}

public class ElseNode : IfNode
{
    public ElseNode(List<StatementNode> children)
        : base(children) { }

    public override string ToString()
    {
        return $" Else: {StatementListToString(Children)} {((NextIfNode == null) ? string.Empty : NextIfNode)}";
    }
}

public class ForNode : StatementNode
{
    public ForNode(
        VariableReferenceNode variable,
        ASTNode from,
        ASTNode to,
        List<StatementNode> children
    )
    {
        Variable = variable;
        From = from;
        To = to;
        Children = children;
    }

    public VariableReferenceNode Variable { get; init; }
    public ASTNode From { get; init; }
    public ASTNode To { get; init; }
    public List<StatementNode> Children { get; init; }

    public override object[] returnStatementTokens()
    {
        object[] arr = { "For", Variable, From, To, Children };
        return arr;
    }

    public override string ToString()
    {
        return $" For: {Variable} From: {From} To: {To} {Environment.NewLine} {StatementListToString(Children)}";
    }
}

public class BooleanExpressionNode : ASTNode
{
    public BooleanExpressionNode(ASTNode left, BooleanExpressionOpType op, ASTNode right)
    {
        Left = left;
        Op = op;
        Right = right;
    }

    public BooleanExpressionOpType Op { get; init; }
    public ASTNode Left { get; init; }
    public ASTNode Right { get; init; }

    public override string ToString()
    {
        return $"({Left.ToString()} {Op} {Right.ToString()})";
    }
}

/// <summary>
/// Models an assignment statement.
/// </summary>
public class AssignmentNode : StatementNode
{
    public AssignmentNode(VariableReferenceNode target, ASTNode expression)
    {
        Target = target;
        Expression = expression;
    }

    /// <summary>
    /// The target variable to which the expression is assigned (LHS of the :=).
    /// </summary>
    public VariableReferenceNode Target { get; init; }

    /// <summary>
    /// The expression assigned to the target variable (RHS of the :=).
    /// </summary>
    public ASTNode Expression { get; init; }

    public override object[] returnStatementTokens()
    {
        object[] arr = { "", Target.Name, Expression.ToString() };

        return arr;
    }

    public override string ToString()
    {
        return $"{Target} := {Expression}";
    }
}

public class ModuleNode : ASTNode
{
    public Dictionary<string, EnumNode> Enums { get; init; }
    public string Name { get; set; }
    public Dictionary<string, CallableNode> Functions { get; init; }
    public Dictionary<string, List<CallableNode>> Functions2 { get; } = [];
    public Dictionary<string, RecordNode> Records { get; init; }
    public Dictionary<string, VariableNode> GlobalVariables { get; } = [];

    //Dictionary associating names to something to be imported/exported
    //has a type of ASTNode? as references will later be added
    public Dictionary<string, ASTNode> Exported { get; set; }
    public Dictionary<string, ASTNode> Imported { get; set; }

    //ImportTargetNames holds a module and the list of functions that this module has imported
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
        Dictionary<string, VariableNode> variablesInScope
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

    public void AddToGlobalVariables(List<VariableNode> variables)
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
            Enums.Add(enumNode.Type, enumNode);
        }
    }

    public void addExportName(string? name)
    {
        ExportTargetNames.AddLast(name);
    }

    public void addExportNames(LinkedList<string> names)
    {
        foreach (var name in names)
        {
            ExportTargetNames.AddLast(name);
        }
    }

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
}

public class AssertResult
{
    public string parentTestName;
    public string? comparedValues;
    public bool passed;
    public int lineNum;

    public AssertResult(string parentTestName, bool passed)
    {
        this.parentTestName = parentTestName;
        this.passed = passed;
    }

    public AssertResult(string parentTestName)
    {
        this.parentTestName = parentTestName;
    }

    public override string ToString()
    {
        return parentTestName
            + " assertIsEqual (line: "
            + lineNum
            + ") "
            + comparedValues
            + " : "
            + (passed ? "passed" : "failed");
    }
}

public class TestResult
{
    public string testName;
    public string parentFunctionName;
    public int lineNum;
    public LinkedList<AssertResult> Asserts = new LinkedList<AssertResult>();

    public TestResult(string testName, string parentFunctionName)
    {
        this.testName = testName;
        this.parentFunctionName = parentFunctionName;
    }

    public override string ToString()
    {
        return "Test "
            + testName
            + " (line: "
            + lineNum
            + " ) results:\n"
            + string.Join("\n", Asserts);
    }
}

public enum CrossFileInteraction
{
    Module,
    Export,
    Import
}
