using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Shank;
using Shank.ASTNodes;
using Shank.Tran;
using Tran;

namespace TranUnitTests
{
    [TestClass]
    public class InterpreterTests
    {
        private Shank.Tran.Parser parser = null!;
        private Shank.Tran.Lexer lexer = null!;
        private LinkedList<Shank.Tran.Token> tokens = null!;

        [TestInitialize]
        public void Setup()
        {
            tokens = new LinkedList<Shank.Tran.Token>();
        }

        private void CreateParser(List<string> files)
        {
            lexer = new Shank.Tran.Lexer(files);
            parser = new Shank.Tran.Parser(lexer.Lex());
        }

        public void InitializeInterpreter(string file)
        {
            Interpreter.Reset();
            SemanticAnalysis.reset();
            var programs = new List<string>();
            programs.Add(file);
            CreateParser(programs);
            Dictionary<string, ModuleNode> modules = parser.Parse().Modules;
            modules = TRANsformer.Walk(modules);
            Interpreter.setModules(modules);
            var startModule = Interpreter.setStartModule();
            SemanticAnalysis.setStartModule();
            if (startModule != null)
                BuiltInFunctions.Register(startModule.getFunctions());
        }

        private Dictionary<string, ModuleNode> GetModules(List<string> programs)
        {
            Dictionary<string, ModuleNode> modules = new Dictionary<string, ModuleNode>();
            foreach (var program in programs)
            {
                CreateParser(programs);
                var module = parser.Parse().Modules.First();
                modules.Add(module.Key, module.Value);
            }
            return modules;
        }

        public void InitializeInterpreter(List<string> files)
        {
            Interpreter.Reset();
            SemanticAnalysis.reset();
            CreateParser(files);
            Dictionary<string, ModuleNode> modules = parser.Parse().Modules;
            modules = TRANsformer.Walk(modules);
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
                // Console.WriteLine($"\nOutput of {currentModule.getName()}:\n");

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
        x = 1+1
        console.print(x)".Replace("    ", "\t")
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
        number retVal
        retVal = addStuff(x, y) { becomes in Shank addStuff ( x,y,var retval) }
        console.print(retVal)
    
    addStuff(number a, number b) : number retVal  { becomes in Shank addStuff ( x,y,var retval) }
        retVal = a+b".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestMultiAssignment()
        {
            InitializeInterpreter(
                @"
class start
    start()
        number x
        number y
        x, y = getValues()
        console.print(x)
        console.print(y)
    
    getValues() : number a, number b  { becomes in Shank addStuff ( x,y,var retval) }
        a = 20
        b = 30".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestFunctionCallExpression()
        {
            InitializeInterpreter(
                @"
class start
    start()
        number x
        x = getValue() + getValue()
    
    getValue() : number a  { becomes in Shank addStuff ( x,y,var retval) }
        a = 100".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestPrint()
        {
            InitializeInterpreter(
                @"
class start
    start()
        console.print(""tran"")".Replace("    ", "\t")
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
        mutator:
            x = value
    string y
        mutator:
            y = value

    start()
        x = 100
        y = ""helloworld""".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestAccessorAssignment()
        {
            InitializeInterpreter(
                @"
class start
    number x
        accessor:
            value = 100

    start()
        number a
        a = x + 10
        a = a + x + 100 + (x*2)
        a = (x+(x+x)+x)".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestAccessorFunctionCall()
        {
            InitializeInterpreter(
                @"
class start
    number x
        accessor:
            value = 100
    number y
        accessor:
            value = 99

    start()
        doStuff(x, y)

    doStuff(number a, number b)
        number c
        c = a + b".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestAccessorLoop()
        {
            InitializeInterpreter(
                @"
class start
    number x
        accessor:
            value = 100

    start()
        number sum
        loop x.times()
            sum = sum + x".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestAccessorIf()
        {
            InitializeInterpreter(
                @"
class start
    number x
        accessor:
            value = 100

    start()
        number a
        a = 100
        if x == a
            a = 50
        else if x == 50
            a = 25
        else if x == (x+100)
            a = 10
        ".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestMutators()
        {
            InitializeInterpreter(
                @"
class start
    number x
        mutator:
            x = value

    start()
        x = 999".Replace("    ", "\t")
            );
            RunInterpreter();
        }

        [TestMethod]
        public void InterpreterTestMultiClass()
        {
            List<string> files = new List<string>();
            files.Add(
                @"
class start
    number x
        mutator:
            x = value
    string y
        mutator:
            y = value

    start()
        x = 100
        y = ""helloworld""".Replace("    ", "\t")
            );
            files.Add(
                @"
class test
    doStuff()
        number a
        a = 9000 * 1000".Replace("    ", "\t")
            );
            InitializeInterpreter(files);
            RunInterpreter();
        }

        //TODO: we can worry about this at a later date
        [TestMethod]
        public void InterpreterTestInterfaces()
        {
            List<string> files = new List<string>();
            files.Add(
                @"
interface someName
    square() : number s".Replace("    ", "\t")
            );
            files.Add(
                @"
class test implements someName
    start()
        number x 
        x = 10
        number y
        y = square(x)
        console.print(y)

    square(number x) : number s
        s = x*x".Replace("    ", "\t")
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
