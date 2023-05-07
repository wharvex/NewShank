using System.Text;
using LLVMSharp.Interop;
using LLVMSharp;
using System.Collections.Generic;
using System.Linq;
using System.IO;

//To compile to RISC-V: llc -march=riscv64 output_ir_3.ll -o out3.s

namespace Shank {
    public abstract class ASTNode {
    }

    public class FunctionCallNode : StatementNode {
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
                Parameters.ForEach(p=>b.AppendLine($"   {p}"));
            }
            object [] arr = {"FUNCTION", Name, b.ToString()};
            return arr;
        }

        public override string ToString()
        {

            var b = new StringBuilder();
            b.AppendLine($"Function {Name}:");
            if (Parameters.Any())
            {
                b.AppendLine("Parameters:");
                Parameters.ForEach(p=>b.AppendLine($"   {p}"));


            }

            return b.ToString();
        }
    }

    public class ParameterNode : ASTNode {
        public ParameterNode(ASTNode constant)
        {
            IsVariable = false;
            Variable = null;
            Constant = constant;
        }

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

    public class IntNode : ASTNode {
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

    public class FloatNode : ASTNode {
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

    public class BoolNode : ASTNode {
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

    public class CharNode : ASTNode {
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

    public class StringNode : ASTNode {
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

    public abstract class CallableNode : ASTNode {
        public string Name { get; init; }
        public List<VariableNode> ParameterVariables = new();

        protected CallableNode(string name)
        {
            Name = name;
        }

        protected CallableNode(string name, BuiltInCall execute)
        {
            Name = name;
            Execute = execute;
        }

        public delegate void BuiltInCall(List<InterpreterDataType> parameters);
        public BuiltInCall? Execute;

    }

    public class BuiltInFunctionNode : CallableNode {
        public BuiltInFunctionNode(string name, BuiltInCall execute) : base(name,execute ) { }
        public bool IsVariadic = false;
    }

    public class FunctionNode : CallableNode {
        public FunctionNode(string name) : base(name)
        {
            Execute = (List<InterpreterDataType> paramList) =>Interpreter.InterpretFunction(this,paramList);
        }

        public List<VariableNode> LocalVariables = new(); 
        //LocalVariables (scroll above) contain start, end, i, prev1
        //alloca() for constatns and variables 
        //store() for assignment (Currently, I did both. BUt I just need to use store())
        public List<StatementNode> Statements = new();

        public override string ToString()
        {

            var b = new StringBuilder();
            b.AppendLine($"Function {Name}:");
            if (ParameterVariables.Any())
            {
                b.AppendLine("Parameters:");
                ParameterVariables.ForEach(p=>b.AppendLine($"   {p}"));
            }
            if (LocalVariables.Any())
            {
                b.AppendLine("Local Variables:");
                LocalVariables.ForEach(p=>b.AppendLine($"   {p}"));
            }
            if (Statements.Any())
            {
                b.AppendLine("-------------------------------------");
                Statements.ForEach(p=>b.AppendLine($"   {p}"));
                b.AppendLine("-------------------------------------");
            }

            return b.ToString();
        }

        public Dictionary<string, LLVMValueRef> Exec_Assignment(LLVMBuilderRef builder, Dictionary<string, LLVMValueRef>hash_variables, object[] s_tokens, LLVMContextRef context)
        {
            var allocated_right = LLVMValueRef.CreateConstInt(context.Int64Type, 0, false);

            try //if variable is being assigned
            {
                allocated_right = builder.BuildLoad2(context.Int64Type, hash_variables[(string)s_tokens[2]]); //value to be assigned, i.e. right side of equality. ex. s_tokens[2] is start in prev1 = start
            }
            catch (Exception ex) //if number is being assigned
            {
                allocated_right = LLVMValueRef.CreateConstInt(context.Int64Type, ulong.Parse(s_tokens[2].ToString()), false); 
            }
            var allocated_left = hash_variables[(string)s_tokens[1]]; //variable that will be assigned with a value, i.e. left side of equality. ex. prev1 in prev1:= start
            builder.BuildStore(allocated_right, allocated_left); //stored allocated_right in allocated_left, as this is assignment node.
            hash_variables[(string)s_tokens[1]] = allocated_left; //the value, i.e. allocated_value
            
            return hash_variables;
        }

        public void Exec_Function(LLVMBuilderRef builder, Dictionary<string, LLVMValueRef>hash_variables, object[] s_tokens, LLVMContextRef context, LLVMTypeRef writeFnTy, LLVMValueRef writeFn)
        {
            var allocated = builder.BuildLoad2(context.Int64Type,hash_variables[((string)s_tokens[2]).Trim()]); //ex. s_tokens[2] is prev1 and allocated is the stored value of prev1
            builder.BuildCall2(writeFnTy, writeFn, new LLVMValueRef[] { allocated }, (s_tokens[1]).ToString());
        }

        public Dictionary<string, LLVMValueRef> Exec_For(LLVMBuilderRef builder, Dictionary<string, LLVMValueRef>hash_variables, object[] s_tokens, LLVMContextRef context, LLVMTypeRef writeFnTy, LLVMValueRef writeFn, LLVMValueRef mainFn)
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
            var i = hash_variables[s_tokens[1].ToString()]; //variable that will be assigned with a value, i.e. i
            try //if variable is being assigned
            {
                var start = builder.BuildLoad2(context.Int64Type, hash_variables[s_tokens[2].ToString()]); //value to be assigned, i.e. start
                builder.BuildStore(start, i); //store i with a value of start
                hash_variables[s_tokens[1].ToString()] = i; 
            }
            catch (Exception ex) //if number is being assigned
            {
                var start = LLVMValueRef.CreateConstInt(context.Int64Type, ulong.Parse(s_tokens[2].ToString()), false); 
                builder.BuildStore(start, i); //store i with a value of start
                hash_variables[s_tokens[1].ToString()] = i; 
            }

            //Create the for loop condition 
            var loopCondBlock = mainFn. AppendBasicBlock("for.condition");
            builder.BuildBr(loopCondBlock);

            //Create the for loop body 
            var loopBodyBlock = mainFn. AppendBasicBlock("for.body");
            builder.PositionAtEnd(loopBodyBlock);

            //For loop body contains statements. So like before, go through each statement
            List<StatementNode> for_statements = (List<StatementNode>)s_tokens[4];

            foreach (var statement in for_statements)
            {
                object[] for_tokens = statement.returnStatementTokens();
                if (for_tokens[0] == ""){ //if not a function, i.e. assignment

                    try //if the assignment value is only 1 value. ex.) prev1
                    {
                        hash_variables = Exec_Assignment(builder, hash_variables, for_tokens, context);
                    } 
                    catch (Exception ex) //if the assignment value is multiple. ex.) prev1 + prev2
                    {
                        string[] tokens = ((string)for_tokens[2].ToString()).Split(' ');
                        var allocated_right = LLVMValueRef.CreateConstInt(context.Int64Type, 0, false); //initialize to 0
                                
                        switch (tokens[1])
                        {            
                            case "plus":
                                try //if variable is being assigned
                                {
                                    allocated_right = builder.BuildAdd(builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[0]]), builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[2]]), "plus");
                                }
                                catch (Exception exception) //if number is being assigned
                                {
                                    allocated_right = builder.BuildAdd(LLVMValueRef.CreateConstInt(context.Int64Type, ulong.Parse(tokens[0].ToString())), builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[2]]), "plus");
                                }
                                break;
                                    
                            case "minus":
                                try //if variable is being assigned
                                {
                                    allocated_right = builder.BuildSub(builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[0]]), builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[2]]), "minus");
                                }
                                catch (Exception exception) //if number is being assigned
                                {
                                    allocated_right = builder.BuildSub(LLVMValueRef.CreateConstInt(context.Int64Type, ulong.Parse(tokens[0].ToString())), builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[2]]), "minus");
                                }
                                break;

                            case "times":
                                try //if variable is being assigned
                                {
                                    allocated_right = builder.BuildMul(builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[0]]), builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[2]]), "times");
                                }
                                catch (Exception exception) //if number is being assigned
                                {
                                    allocated_right = builder.BuildMul(LLVMValueRef.CreateConstInt(context.Int64Type, ulong.Parse(tokens[0].ToString())), builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[2]]), "times");
                                }
                                break;

                            case "divide":
                                try //if variable is being assigned
                                {
                                    allocated_right = builder.BuildSDiv(builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[0]]), builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[2]]), "divide");
                                }
                                catch (Exception exception) //if number is being assigned
                                {
                                    allocated_right = builder.BuildSDiv(LLVMValueRef.CreateConstInt(context.Int64Type, ulong.Parse(tokens[0].ToString())), builder.BuildLoad2(context.Int64Type,hash_variables[(string)tokens[2]]), "divide");
                                }
                                break;

                            default:
                                break;
                        }

                        var allocated_left_var = hash_variables[(string)for_tokens[1]];
                        builder.BuildStore(allocated_right, allocated_left_var); 
                        hash_variables[(string)for_tokens[1]] = allocated_left_var;
                    }
                }
                else if (for_tokens[0] == "FUNCTION") //if it is a function node, such as write()
                {
                    Exec_Function(builder, hash_variables, for_tokens, context, writeFnTy, writeFn);
                }
                else
                {
                    hash_variables = Exec_For(builder, hash_variables, s_tokens, context, writeFnTy, writeFn, mainFn);
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

            //hash_variables[s_tokens[3].ToString()] is 20 as can be seen in the generated IR.
            var loopCond = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, builder.BuildLoad2(context.Int64Type, hash_variables[s_tokens[1].ToString()]), builder.BuildLoad2(context.Int64Type, hash_variables[s_tokens[3].ToString()]), "loop.condition.cmp");
            builder.BuildCondBr(loopCond, loopBodyBlock, mainFn.AppendBasicBlock("exit"));

            // Build the exit block
            builder.PositionAtEnd(mainFn.LastBasicBlock);
            builder.BuildRet(LLVMValueRef.CreateConstInt(context.Int64Type, 0, false));

            return hash_variables;
        }

        public unsafe void LLVMCompile()
        {
            // Setup context, module, and builder
            using var context = LLVMContextRef.Create();
            using var module = context.CreateModuleWithName("main");
            using var builder = context.CreateBuilder();

            // Create the write() function
            var writeFnRetTy = context.Int64Type;
            //Int8Type: for string type
            var writeFnParamTys = new LLVMTypeRef[] {context.Int64Type};
            var writeFnTy = LLVMTypeRef.CreateFunction(writeFnRetTy, writeFnParamTys);
            var writeFn = module.AddFunction("write", writeFnTy);

            // Create the main (or the start()) function. For void return, use context.VoidType;
            var mainRetTy = context.Int64Type;
            var mainParamTys = new LLVMTypeRef[] {};
            var mainFnTy = LLVMTypeRef.CreateFunction(mainRetTy, mainParamTys);
            var mainFn = module.AddFunction("start", mainFnTy);
            var mainBlock = mainFn.AppendBasicBlock("entry"); //the entry block within the start() function

            // Create the body of the main function
            builder.PositionAtEnd(mainBlock);
            //var int64PtrType = LLVMTypeRef.CreatePointer(context.Int64Type, 0);

            /*
            Store Local Variables to Memory through BuildAlloca() & BuildStore() 

            hash_variables will contain
            1. If the variable is not initialized, then it will have (i) the variable name itself, (ii) variable referencing to the allocated memory.
            2. If the variable is initialized, then it will have (i) the variable name itself, and (ii) variable referencing to the value stored in the allocated memory.

            To use BuildLoad2(), which loads the value of the variable stored in memory, we utilize this hash_variables.
            Although I tried various approaches, BuildLoad2() must be used.
            */
            var hash_variables = new Dictionary<string, LLVMValueRef>();

            if (LocalVariables.Any())
            {
                for (int i = 0; i < LocalVariables.Count; i++)
                {
                    if (LocalVariables[i].Type.ToString() == "Integer")
                    {
                        if (LocalVariables[i].InitialValue == null) //if the local variable is declared but not initialized
                        {
                            var allocated = builder.BuildAlloca(context.Int64Type, LocalVariables[i].Name);
                            allocated.SetAlignment(4);
                            hash_variables.Add(LocalVariables[i].Name, allocated);
                        }
                        else //if the local variable is both declared and initialized
                        {
                            var allocated = builder.BuildAlloca(context.Int64Type, LocalVariables[i].Name);
                            allocated.SetAlignment(4);

                            //To create string: var stringExample = builder.BuildGlobalStringPtr("%d ", "fmt");
                            var allocated_value = LLVMValueRef.CreateConstInt(context.Int64Type, ulong.Parse(LocalVariables[i].InitialValue.ToString()), false);
                            builder.BuildStore(allocated_value, allocated);
                            //Console.WriteLine("Data type of allocated_value is: {0}", allocated_value.GetType().Name);

                            hash_variables.Add(LocalVariables[i].Name, allocated);
                        }
                    }
                }
            }

            //Go through each statement
            foreach (var s in Statements)
            {
                object[] s_tokens = s.returnStatementTokens(); //returnStatementTokens() returns variables within the statement. For instance, prev1 and start for prev1:=start statement.

                //if not a function, i.e. if assignment node
                if (s_tokens[0] == ""){
                    /*
                    s_tokens[0]: ""
                    s_tokens[1]: target.Name, ex) prev1 in prev1:=start
                    s_tokens[2]: expression.ToString(), ex) start
                    */

                    hash_variables = Exec_Assignment(builder, hash_variables, s_tokens, context);
                }
                else if (s_tokens[0] == "FUNCTION") //if it is a function node, such as write()
                {
                    /*
                    s_tokens[0]: "FUNCTION"
                    s_tokens[1]: Name, ex) write in write prev1 
                    s_tokens[2]: b.ToString(), ex) prev1
                    */

                    Exec_Function(builder, hash_variables, s_tokens, context, writeFnTy, writeFn);
                }
                else //for loop
                {
                    hash_variables = Exec_For(builder, hash_variables, s_tokens, context, writeFnTy, writeFn, mainFn);
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
            I believe this is due to the library version, and thus no code change could be done.

            Save generatedIR.ll file. Then, change every ptr occurrence to i64*
            */
            module.PrintToFile("IR/generated_IR.ll");
            string irContent = File.ReadAllText("IR/generated_IR.ll");
            string updatedIrContent = irContent.Replace("ptr", "i64*");
            File.WriteAllText("IR/generated_IR.ll", updatedIrContent);
        }
    }

    public class VariableNode : ASTNode {
        public string? Name;

        public enum DataType { Real, Integer, String, Character, Boolean/*, Array*/ };

        public DataType Type;
//        public DataType ArrayType;
        public bool IsConstant;
        public ASTNode? InitialValue;
 //       public ASTNode? From;
 //       public ASTNode? To;

        public override string ToString()
        {

            return
                $"{Name} : {Type} {(IsConstant ? "const" : string.Empty)} {(InitialValue == null ? string.Empty : InitialValue)}";
                //$"{Name} : {(Type == DataType.Array ? "Array of " + ArrayType:Type)} {(IsConstant ? "const" : string.Empty)} {(InitialValue == null ? string.Empty : InitialValue)} {(From == null ? string.Empty : " From: "+ From)} {(To == null ? string.Empty : " To: "+ To)}";
        }
    }

    public class MathOpNode : ASTNode {
        public MathOpNode(ASTNode left, OpType op, ASTNode right)
        {
            Left = left;
            Op = op;
            Right = right;
        }
        public enum OpType { plus, minus, times, divide, modulo };

        public OpType Op { get; init; }
        public ASTNode Left { get; init; }
        public ASTNode Right { get; init; }

        public override string ToString()
        {

            return $"{Left.ToString()} {Op} {Right.ToString()}";
        }
    }

    public class StatementNode : ASTNode {
        protected static string StatementListToString(List<StatementNode> statements)
        {

            var b = new StringBuilder();
            statements.ForEach(c => b.Append("\t" + c));
            return b.ToString();
        }

        virtual public object[] returnStatementTokens()
        {
            object[] arr={};
            return arr;
        }
    }

    public class VariableReferenceNode : ASTNode {
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
        public ASTNode? Index { get; init; }
        public override string ToString()
        {

            return $"{Name}{(Index!= null? " Index:" + Index : string.Empty)}";
        }
    }

    public class WhileNode : StatementNode {
        public WhileNode(BooleanExpressionNode exp, List<StatementNode> children)
        {
            Expression = exp;
            Children = children;
        }
        public BooleanExpressionNode Expression { get; init; }
        public List<StatementNode> Children;

        public override string ToString()
        {

            return $" WHILE: {Expression} {StatementListToString(Children)}";
        }
    }

    public class RepeatNode : StatementNode {
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

    public class IfNode : StatementNode {
        protected IfNode(List<StatementNode> children)
        {
            Expression = null;
            Children = children;
            NextIfNode = null;
        }

        public IfNode(BooleanExpressionNode expression, List<StatementNode> children, IfNode? nextIfNode = null)
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

            return $"If: {Expression} {StatementListToString(Children)} {((NextIfNode == null)?string.Empty : Environment.NewLine + NextIfNode)}";
        }
    }

    public class ElseNode : IfNode {
        public ElseNode(List<StatementNode> children) : base(children)
        {
        }
        public override string ToString()
        {

            return $" Else: {StatementListToString(Children)} {((NextIfNode == null)?string.Empty : NextIfNode)}";
        }
    }

    public class ForNode : StatementNode {
        public ForNode(VariableReferenceNode variable, ASTNode from, ASTNode to, List<StatementNode> children)
        {
            Variable = variable;
            From = from;
            To = to;
            Children = children;
        }
        
        public VariableReferenceNode Variable { get; init; }
        public ASTNode From { get; init; } 
        public ASTNode To{ get; init; }
        public List<StatementNode> Children { get; init; }

        public override object[] returnStatementTokens()
        {
            object [] arr = {"For", Variable, From, To, Children};
            return arr;
        }

        public override string ToString()
        {
 
            return $" For: {Variable} From: {From} To: {To} {Environment.NewLine} {StatementListToString(Children)}";
        }
    }

    public class BooleanExpressionNode : ASTNode {
        public BooleanExpressionNode (ASTNode left, OpType op, ASTNode right)
        {
            Left = left;
            Op = op;
            Right = right;
        }

        public enum OpType { lt, le, gt, ge, eq, ne };

        public OpType Op { get; init; }
        public ASTNode Left { get; init; }
        public ASTNode Right { get; init; }

        public override string ToString()
        {

            return $"({Left.ToString()} {Op} {Right.ToString()})";
        }

    }

    public class AssignmentNode : StatementNode {
        public AssignmentNode(VariableReferenceNode target, ASTNode expression)
        {
            this.expression = expression;
            this.target = target;
        }

        public VariableReferenceNode target { get; set; }
        public ASTNode expression { get; set; }

        public override object[] returnStatementTokens()
        {
            object [] arr = {"", target.Name, expression.ToString()};

            return arr;
        }

        public override string ToString()
        {

            return $"{target} := {expression}";
        }
    }
    
}
