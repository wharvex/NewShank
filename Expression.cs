using System.Text;
using LLVMSharp.Interop;
using LLVMSharp;

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

        public override string[] codeGen()
        {
            var b = new StringBuilder();
            if (Parameters.Any())
            {
                Parameters.ForEach(p=>b.AppendLine($"   {p}"));
            }
            string [] arr = {Name, b.ToString()};
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
            // Setup context, etc
            using var context = LLVMContextRef.Create();
            using var module = context.CreateModuleWithName("main");
            using var builder = context.CreateBuilder();

            
            // Create the puts function
            var putsRetTy = context.Int64Type;
            var putsParamTys = new LLVMTypeRef[] {
                LLVMTypeRef.CreatePointer(context.Int8Type, 0)
            };

            // Create the main function
            var mainRetTy = context.VoidType;
            var mainParamTys = new LLVMTypeRef[] { };
            var mainFnTy = LLVMTypeRef.CreateFunction(mainRetTy, mainParamTys);
            var mainFn = module.AddFunction("main", mainFnTy);
            var mainBlock = mainFn.AppendBasicBlock("entry");

            // Create the body of the main function
            builder.PositionAtEnd(mainBlock);
            //var message = builder.BuildGlobalStringPtr("Hello, World!");
            //builder.BuildCall2(putsFnTy, putsFn, new LLVMValueRef[] { message }, "");
            //builder.BuildRetVoid();

            foreach (var s in Statements)
            {
                Console.Write(s.ToString());
                string[] arr = s.codeGen();

                //if not a function
                if (arr[0] ==""){
                    var message = builder.BuildGlobalStringPtr(arr[2]);
                    //builder.BuildCall2(putsFnTy, putsFn, new LLVMValueRef[] { message }, "");    
                    //builder.BuildStore(message, message);
                    var var_message = builder.BuildAlloca(putsRetTy);
                    // Store the value of a in memory. message: value of a
                    builder.BuildStore(message, var_message);

                    // Define an i32 integer type
                    var intType = context.Int64Type;

                    // Create a constant integer value with value 42
                    var constInt = LLVMValueRef.CreateConstInt(intType, 42, false);

                    /*
                    // Allocate memory for variables 'a' and 'x'
                    var a = builder.BuildAlloca(putsRetTy, "a");
                    var x = builder.BuildAlloca(putsRetTy, "x");

                    // Multiply 'a' by 5 and add 3
                    var five = LLVM.ConstInt(context.Int64Type, 5);
                    var three = LLVM.ConstInt(putsRetTy, 3ul, false);
                    var aVal = builder.BuildLoad(a, "a_val");
                    var aTimesFive = builder.BuildMul(aVal, five, "a_times_five");
                    var xVal = builder.BuildAdd(aTimesFive, three, "x_val");
                    */
                }
                else{
                    var message = builder.BuildGlobalStringPtr(arr[1]);
                    var FnTy = LLVMTypeRef.CreateFunction(putsRetTy, putsParamTys);
                    var Fn = module.AddFunction(arr[0], FnTy);
                    builder.BuildCall2(FnTy, Fn, new LLVMValueRef[] { message }, "");
                    var x = builder.BuildAdd(message, message, "message");
                }
                
                Console.Write("\n*********************\n");
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
            statements.ForEach(c => b.Append(Environment.NewLine + "\t" + c));
            return b.ToString();
        }

        virtual public string[] codeGen()
        {
            string[] arr={};
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

        public override string[] codeGen()
        {
            string [] arr = {"", target.Name, expression.ToString()};

            return arr;
        }

        public override string ToString()
        {

            return $"{target} := {expression}";
        }
    }
    
}
