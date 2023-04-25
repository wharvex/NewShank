using System.Text;
using LLVMSharp.Interop;
using LLVMSharp;
using System.Collections.Generic;
using System.Linq;

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

        public override object[] codeGen()
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

        public void LLVMCompile()
        {
            // Setup context, module, and builder
            using var context = LLVMContextRef.Create();
            using var module = context.CreateModuleWithName("main");
            using var builder = context.CreateBuilder();

            // Create the write() function
            var writeFnRetTy = context.VoidType;
            var writeFnParamTys = new LLVMTypeRef[] {LLVMTypeRef.CreatePointer(context.Int64Type, 0)};
            var writeFnTy = LLVMTypeRef.CreateFunction(writeFnRetTy, writeFnParamTys);
            var writeFn = module.AddFunction("write", writeFnTy);

            // Create the main (or the start()) function
            var mainRetTy = context.VoidType;
            var mainParamTys = new LLVMTypeRef[] {};
            var mainFnTy = LLVMTypeRef.CreateFunction(mainRetTy, mainParamTys);
            var mainFn = module.AddFunction("start", mainFnTy);
            var mainBlock = mainFn.AppendBasicBlock("entry"); //the entry block within the start() function

            // Create the body of the main function
            builder.PositionAtEnd(mainBlock);

            /*
            varArray will contain
            1. If the variable is not initialized, then it will have (i) variable referencing to the allocated memory, and (ii) the variable name itself.
            2. If the variable is initialized, then it will have (i) variable referencing to the value stored in the allocated memory, and (ii) the variable name itself.
            */
            object[,] varArray = new object[LocalVariables.Count, 2];

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
                            varArray[i, 0] = allocated;
                            varArray[i, 1] = LocalVariables[i].Name;
                        }
                        else //if the local variable is both declared and initialized
                        {
                            
                            var allocated = builder.BuildAlloca(context.Int64Type, LocalVariables[i].Name);
                            allocated.SetAlignment(4);
                            
                            var allocated_value = LLVMValueRef.CreateConstInt(context.Int64Type, ulong.Parse(LocalVariables[i].InitialValue.ToString()), false);
                            builder.BuildStore(allocated_value, allocated);

                            varArray[i, 0] = allocated_value;
                            varArray[i, 1] = LocalVariables[i].Name;

                        }
                    }
                }
            }

            foreach (var s in Statements)
            {
                object[] arr = s.codeGen();

                //if not a function, i.e. if assignment node
                if (arr[0] == ""){

                    for (int i = 0; i < varArray.GetLength(0); i++)
                    {
                        if ((string)arr[2] == (string)varArray[i,1]) //ex. arr[2] is start in prev1 = start
                        {
                            var allocated_left = varArray[i,0];

                            for (int j = 0; j < varArray.GetLength(0); j++)
                            {
                                if ((string)arr[1] == (string)varArray[j,1]) //ex. prev1 in prev1 = start. Searching for allocated memory through BuildAlloca() for prev1 during the variable declaration which is stored in varArray.
                                {
                                    var allocated_right = varArray[j,0];
                                    builder.BuildStore((LLVMValueRef)allocated_left, (LLVMValueRef)allocated_right);

                                    varArray[j,0] = allocated_right; //Before, a variable referencing to allocated memory was stored in varArray[j,0]. Now we replace with allocated_right which stores the value of allocated_left.
                                }
                            }
                        }
                    }
                }
                else if (arr[0] == "FUNCTION") //if it is a function node, such as write()
                {
                    for (int i = 0; i < varArray.GetLength(0); i++)
                    {
                        if (((string)arr[2]).Trim() == (string)varArray[i,1]) //ex. prev1
                        {
                            var allocated = (LLVMValueRef) varArray[i,0];
                            builder.BuildCall2(writeFnTy, writeFn, new LLVMValueRef[] { allocated }, "");
                        }
                    }
                }
                else
                {
                    /*
                    arr[0]: For
                    arr[1]: i
                    arr[2]: start
                    arr[3]: end
                    arr[4]: entire for loop body
                    for i from start to end
                    */

                    builder.PositionAtEnd(mainBlock);// Set IR builder will start from the end of the mainBlock

                    // Create the loop
                    var loopBlock = mainFn.AppendBasicBlock("loop");
                    builder.BuildBr(loopBlock);

                    builder.PositionAtEnd(loopBlock);

                    // Create the loop counter variable
                    var counter = builder.BuildAlloca(context.Int64Type, "counter");
                    counter.SetAlignment(4);
                    ulong start_index=0;
                    ulong.TryParse(arr[2].ToString(), out start_index);
                    builder.BuildStore(LLVMValueRef.CreateConstInt(context.Int64Type, start_index, false), counter);

                    // Create the condition for the loop
                    ulong end_index=0;
                    ulong.TryParse(arr[3].ToString(), out end_index);
                    var endValue = LLVMValueRef.CreateConstInt(context.Int64Type, end_index, false);
                    var condition = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, counter, endValue, "condition");
                    var endBlock = mainFn.AppendBasicBlock("end");

                    builder.BuildCondBr(condition, loopBlock, endBlock);

                    // Add the loop body
                    builder.PositionAtEnd(loopBlock);

                    builder.BuildCall2(writeFnTy, writeFn, new LLVMValueRef[] { (LLVMValueRef) varArray[0,0] }, "");

                    /*

                    List<StatementNode> for_statements = (List<StatementNode>)arr[4];

                    foreach (var statement in for_statements)
                    {
                        object[] for_arr = statement.codeGen();
                    }
                    
                    //Should be something like below. But make it general:
                    LLVMValueRef prev1 = (LLVMValueRef)varArray[2, 0];
                    LLVMValueRef prev2 = (LLVMValueRef)varArray[3, 0];

                    // curr := prev1 + prev2 statement
                    var curr = builder.BuildAdd(prev1, prev2, "curr");

                    // write curr statement
                    builder.BuildCall2(writeFnTy, writeFn, new LLVMValueRef[] { curr }, "");

                    // prev2 := prev1 statement
                    prev2 = prev1;

                    // prev1 := curr statement
                    prev1 = curr;

                    // Increment the loop counter
                    var one = LLVMValueRef.CreateConstInt(context.Int64Type, 1, false);
                    var nextCounterValue = builder.BuildAdd(builder.BuildLoad(context.Int64Type, counter), one, "nextCounter");
                    builder.BuildStore(nextCounterValue, counter);

                    builder.BuildBr(loopBlock);

                    builder.PositionAtEnd(endBlock);
                    */
                }
            }
  
            Console.WriteLine($"LLVM IR\n=========\n{module}");
            
            builder.BuildRetVoid();
            // Initialize LLVM
            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();
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

            return $"({Left.ToString()} {Op} {Right.ToString()})";
        }
    }

    public class StatementNode : ASTNode {
        protected static string StatementListToString(List<StatementNode> statements)
        {

            var b = new StringBuilder();
            statements.ForEach(c => b.Append("\t" + c));
            return b.ToString();
        }

        virtual public object[] codeGen()
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

        public override object[] codeGen()
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

        public override object[] codeGen()
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
