using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shank;
using Shank.ASTNodes;
using Shank.Tran;

namespace TranUnitTests
{
    [TestClass]
    public class InterpreterTests
    {
        private Shank.Tran.Parser parser = null!;
        private Shank.Tran.Lexer lexer = null!;
        private Shank.Tran.TokenHandler handler = null!;
        private LinkedList<Shank.Tran.Token> tokens = null!;

        [TestInitialize]
        public void Setup()
        {
            tokens = new LinkedList<Shank.Tran.Token>();
            handler = new TokenHandler(tokens);
        }

        private void CreateParser(string program)
        {
            lexer = new Shank.Tran.Lexer(program);
            tokens = lexer.Lex();
            parser = new Shank.Tran.Parser(tokens);
        }

        public void InitializeInterpreter(string file)
        {
            Interpreter.Reset();
            SemanticAnalysis.reset();
            CreateParser(file);
            Dictionary<string, ModuleNode> modules = parser.Parse().Modules;
            Interpreter.setModules(modules);
            var startModule = Interpreter.setStartModule();
            SemanticAnalysis.setStartModule();
            if (startModule != null)
                BuiltInFunctions.Register(startModule.getFunctions());
        }

        private Dictionary<string, ModuleNode> GetModules(LinkedList<string> programs)
        {
            Dictionary<string, ModuleNode> modules = new Dictionary<string, ModuleNode>();
            foreach (var program in programs)
            {
                CreateParser(program);
                var module = parser.Parse().Modules.First();
                modules.Add(module.Key, module.Value);
            }
            return modules;
        }

        public void InitializeInterpreter(LinkedList<string> files)
        {
            Interpreter.Reset();
            SemanticAnalysis.reset();
            Dictionary<string, ModuleNode> modules = GetModules(files);
            Interpreter.setModules(modules);
            var startModule = Interpreter.setStartModule();
            SemanticAnalysis.setStartModule();
            if (startModule != null)
                BuiltInFunctions.Register(startModule.getFunctions());
        }

        public void RunInterpreter()
        {
            foreach (KeyValuePair<string, ModuleNode> currentModulePair in Interpreter.Modules)
            {
                var currentModule = currentModulePair.Value;
                //Console.WriteLine($"\nOutput of {currentModule.getName()}:\n");

                if (
                    currentModule.getFunctions().ContainsKey("start")
                    && currentModule.getFunctions()["start"] is FunctionNode s
                )
                {
                    var interpreterErrorOccurred = false;
                    BuiltInFunctions.Register(currentModule.getFunctions());
                    SemanticAnalysis.CheckModules();
                    Interpreter.InterpretFunction(s, new List<InterpreterDataType>());

                    if (interpreterErrorOccurred)
                    {
                        continue;
                    }
                }
            }
        }

        [TestMethod]
        public void InterpreterTestBasic()
        {
            InitializeInterpreter(
                @"
class start
    start()
        number x
        x = 1+1".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestFunctionCall()
        {
            InitializeInterpreter(
                @"
class start
    start()
        number x
        x = 10
        number y
        y = 15
        number temp
        temp = 0
        addStuff(x, y, temp)
    
    addStuff(number a, number b) : number ret
        ret = a+b".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestFields()
        {
            InitializeInterpreter(
                @"
class start
    number x
    string y

    start()
        x = 100
        y = ""helloworld""".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestMultiClass()
        {
            LinkedList<string> files = new LinkedList<string>();
            files.AddLast(
                @"
class start
    number x
    string y

    start()
        x = 100
        y = ""helloworld""".Replace("    ", "\t")
            );
            files.AddLast(
                @"
class test
    doStuff()
        number a
        a = 9000 * 1000".Replace("    ", "\t")
            );
            InitializeInterpreter(files);
            RunInterpreter();
        }
        
        [TestMethod]
        public void InterpreterTest5()
        {
            LinkedList<string> files = new LinkedList<string>();
            files.AddLast(
                @"
interface someName
    number x
    string y".Replace("    ", "\t")
            );
            files.AddLast(
                @"
class test implements someName
    number x
        x = 9000 * 1000
        y = ""agc""".Replace("    ", "\t")
            );
            InitializeInterpreter(files);
            RunInterpreter();
        }
        
        [TestMethod]
        public void InterpreterTestIfsAndLoops()
        {
            InitializeInterpreter(
                @"
class start
    start()
        number x
        number n
        n=101
        if n > 100
            keepGoing = false
            x=5%100
            temp = loop x.times()
                console.print(temp)
        console.print(n)".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestFibonacci()
        {
            InitializeInterpreter(
                @"
class start
    start()
        number x
        number y
        number z
        number totalCount
        x=0
        y=1
        z=0
        totalCount=8
        console.print(x + "" "" + y)
        loop totalCount.times()
            z = x+y
            console.print("" "" + z)
             x = y
             y = z".Replace("    ", "\t")
            );
            RunInterpreter();
        }
    }
}
