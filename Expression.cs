using System.Dynamic;
using System.Text;
using LLVMSharp.Interop;

//To compile to RISC-V: llc -march=riscv64 output_ir_3.ll -o out3.s

namespace Shank
{
    public abstract class ASTNode { }

    public class FunctionCallNode : StatementNode
    {
        public string Name;
        public List<ParameterNode> Parameters = new();

        public FunctionCallNode(string name)
        {
            Name = name;
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
         * IsVariable: the param was preceded by the "var" keyword when it was passed in,
         * meaning it can be changed in the function and its new value will persist in the
         * caller's scope.
         *
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

        public int Value;

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

        public float Value;

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

        public bool Value;

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

        public char Value;

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

        public string Value;

        public override string ToString()
        {
            return $"{Value}";
        }
    }

    public abstract class CallableNode : ASTNode
    {
        public string Name { get; set; }
        public string parentModuleName { get; set; }
        public bool IsPublic { get; set; }
        public string OrigName { get; set; }
        public List<VariableNode> ParameterVariables = new();

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
            OrigName = name;
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
    }

    public class BuiltInFunctionNode : CallableNode
    {
        public BuiltInFunctionNode(string name, BuiltInCall execute)
            : base(name, execute) { }

        public bool IsVariadic = false;
    }

    public class FunctionNode : CallableNode
    {
        public FunctionNode(string name, string moduleName, bool isPublic)
            : base(name, moduleName, isPublic)
        {
            // This is a delegate instance, similar to an abstract method implementation in Java.
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

        public List<VariableNode> LocalVariables = new();

        public List<StatementNode> Statements = new();

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

            LLVMValueRef allocated_right = EvalExpression(
                builder,
                hash_variables,
                rhsTokens,
                context
            );

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

        public unsafe void LLVMCompile()
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

                    hash_variables = Exec_Assignment(
                        builder,
                        hash_variables,
                        statementTokens,
                        context
                    );
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

    public class RecordMemberNode : ASTNode
    {
        public string Name { get; init; }
        public VariableNode.DataType MemberType { get; init; }

        public RecordMemberNode(string name, VariableNode.DataType memberType)
        {
            Name = name;
            MemberType = memberType;
        }
    }

    public class RecordNode : ASTNode
    {
        public string Name { get; init; }
        private readonly List<RecordMemberNode> _members;

        public RecordNode(string name, params RecordMemberNode[] members)
        {
            if (members.Contains(null) || !members.Any())
            {
                throw new ArgumentNullException(nameof(members));
            }
            Name = name;
            _members = new List<RecordMemberNode>(members);
        }

        public RecordMemberNode GetMemberNodeByName(string name)
        {
            return _members.FirstOrDefault(m => m.Name.Equals(name), null);
        }
    }

    public class VariableNode : ASTNode
    {
        public string? Name;

        public enum DataType
        {
            Real,
            Integer,
            String,
            Character,
            Boolean,
            Array
        };

        public DataType Type;

        // If Type is Array, then ArrayType is the type of its elements.
        // If Type is not Array, then ArrayType should be null.
        public DataType ArrayType;
        public bool IsConstant;
        public ASTNode? InitialValue;

        public ASTNode? From;
        public ASTNode? To;

        public override string ToString()
        {
            return $"{Name} : {(Type == DataType.Array ? "Array of " + ArrayType : Type)} {(IsConstant ? "const" : string.Empty)} {(InitialValue == null ? string.Empty : InitialValue)} {(From == null ? string.Empty : " From: " + From)} {(To == null ? string.Empty : " To: " + To)}";
        }
    }

    public class MathOpNode : ASTNode
    {
        public MathOpNode(ASTNode left, OpType op, ASTNode right)
        {
            Left = left;
            Op = op;
            Right = right;
        }

        public enum OpType
        {
            plus,
            minus,
            times,
            divide,
            modulo
        };

        public OpType Op { get; init; }
        public ASTNode Left { get; init; }
        public ASTNode Right { get; init; }

        public override string ToString()
        {
            return $"{Left.ToString()} {Op} {Right.ToString()}";
        }
    }

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
            Index = null;
        }

        public VariableReferenceNode(string name, ASTNode index)
        {
            Name = name;
            Index = index;
        }

        public string Name { get; init; }

        /// <summary>
        /// Holds the variable's array index expression if it exists.
        /// </summary>
        public ASTNode? Index { get; init; }

        public override string ToString()
        {
            return $"{Name + (Index != null ? (", Index: " + Index) : string.Empty)}";
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
        public List<StatementNode> Children;

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
        public List<StatementNode> Children;

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
        public BooleanExpressionNode(ASTNode left, OpType op, ASTNode right)
        {
            Left = left;
            Op = op;
            Right = right;
        }

        public enum OpType
        {
            lt,
            le,
            gt,
            ge,
            eq,
            ne
        };

        public OpType Op { get; init; }
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
            this.expression = expression;
            this.target = target;
        }

        /// <summary>
        /// The variable to be assigned to (Left hand side of assignment statement).
        /// </summary>
        public VariableReferenceNode target { get; set; }

        /// <summary>
        /// The expression to be assigned to the variable (Right hand side of assignment statement).
        /// </summary>
        public ASTNode expression { get; set; }

        public override object[] returnStatementTokens()
        {
            object[] arr = { "", target.Name, expression.ToString() };

            return arr;
        }

        public override string ToString()
        {
            return $"{target} := {expression}";
        }
    }

    public class ModuleNode
    {
        private string name;
        private Dictionary<string, CallableNode> Functions;

        // Why are we allowing null exports and imports?
        private Dictionary<string, ASTNode?> Exports;
        private Dictionary<string, ASTNode?> Imports;
        private Dictionary<string, LinkedList<string>> ImportTargetNames;

        //private LinkedList<string> ImportTargetNames;
        private LinkedList<string> ExportTargetNames;

        public ModuleNode(string name)
        {
            this.name = name;
            Functions = new Dictionary<string, CallableNode>();
            Exports = new Dictionary<string, ASTNode?>();
            Imports = new Dictionary<string, ASTNode?>();
            ImportTargetNames = new Dictionary<string, LinkedList<string>>();
            //ImportTargetNames = new LinkedList<string>();
            ExportTargetNames = new LinkedList<string>();
        }

        public void updateImports(
            Dictionary<string, CallableNode> recievedFunctions,
            Dictionary<string, ASTNode?> recievedExports
        )
        {
            foreach (var function in recievedFunctions)
            {
                if (!Imports.ContainsKey(function.Key))
                    Imports.Add(function.Key, function.Value);
                if (recievedExports.ContainsKey(function.Key))
                {
                    ((CallableNode)Imports[function.Key]).IsPublic = true;
                }
                if (ImportTargetNames.ContainsKey(function.Value.parentModuleName))
                {
                    if (ImportTargetNames[function.Value.parentModuleName] != null)
                    {
                        if (
                            !ImportTargetNames[function.Value.parentModuleName].Contains(
                                function.Key
                            )
                        )
                        {
                            ((CallableNode)Imports[function.Key]).IsPublic = false;
                        }
                    }
                }
            }
        }

        public void updateExports()
        {
            foreach (var exportFunctionName in ExportTargetNames)
            {
                if (Functions.ContainsKey(exportFunctionName))
                {
                    Exports.Add(exportFunctionName, Functions[exportFunctionName]);
                }
                else
                {
                    throw new Exception(
                        "Could not find "
                            + exportFunctionName
                            + " in the current list of functions in module "
                            + name
                    );
                }
            }
        }

        public void addFunction(CallableNode? function)
        {
            if (function != null)
            {
                Functions.Add(function.Name, function);
            }
        }

        public void AddRecord() { }

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

        public void addImportName(string moduleName, LinkedList<string> functions)
        {
            ImportTargetNames.Add(moduleName, functions);
        }

        public LinkedList<string> getExportList()
        {
            return ExportTargetNames;
        }

        public Dictionary<string, LinkedList<string>> getImportDict()
        {
            return ImportTargetNames;
        }

        public Dictionary<string, ASTNode?> getExports()
        {
            return Exports;
        }

        public Dictionary<string, ASTNode?> getImports()
        {
            return Imports;
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

        public string getName()
        {
            return name;
        }

        public void setName(string nameIn)
        {
            name = nameIn;
        }
    }

    public enum CrossFileInteraction
    {
        Module,
        Export,
        Import
    }
}
