using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Optional;
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

        public void RunInterpreter(string file)
        {
            var files = new List<string> { file };
            CreateParser(files);
            InterpretOptions options = new InterpretOptions();
            options.InputFiles = files;

            var program = parser.Parse();

            program.Modules = TRANsformer.Walk(program.Modules);
            CommandLineArgsParser.RunSemanticAnalysis(options, program);

            Interpreter.ActiveInterpretOptions = options;
            Interpreter.Modules = program.Modules;
            Interpreter.StartModule = program.GetStartModuleSafe();
            Interpreter.InterpretFunction(
                program.GetStartModuleSafe().GetStartFunctionSafe(),
                [],
                program.GetStartModuleSafe()
            );
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

        public void RunInterpreter(List<string> files)
        {
            CreateParser(files);
            InterpretOptions options = new InterpretOptions();
            options.InputFiles = files;

            var program = parser.Parse();

            program.Modules = TRANsformer.Walk(program.Modules);
            CommandLineArgsParser.RunSemanticAnalysis(options, program);

            Interpreter.ActiveInterpretOptions = options;
            Interpreter.Modules = program.Modules;
            Interpreter.StartModule = program.GetStartModuleSafe();
            Interpreter.InterpretFunction(
                program.GetStartModuleSafe().GetStartFunctionSafe(),
                [],
                program.GetStartModuleSafe()
            );
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
            RunInterpreter(
                @"
class start
    start()
        number x
        x = 1+1
        console.print(x)".Replace("    ", "\t")
            );
        }

        [TestMethod]
        public void InterpreterTestIf()
        {
            RunInterpreter(
                @"
class start
    start()
        number a
        a = 100

        if a == 50
            console.print(a)
        else if a == 100
            console.print(""Success"")

        console.print(""crazy"")
        ".Replace("    ", "\t")
            );
        }

        [TestMethod]
        public void InterpreterTestFunctionCall()
        {
            RunInterpreter(
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
        }

        [TestMethod]
        public void InterpreterTestMultiAssignment()
        {
            RunInterpreter(
                @"
class start
    start()
        number x
        number y
        x, y = getValues()
        console.print(x)
        console.print(y)
    
    getValues() : number a, number b
        a = 20
        b = 30".Replace("    ", "\t")
            );
        }

        [TestMethod]
        public void InterpreterTestFunctionCallTransformation1()
        {
            RunInterpreter(
                @"
class start
    start()
        number x
        x = getValue() + getValue() + getValue()
        console.print(x)
    
    getValue() : number a 
        a = 100".Replace("    ", "\t")
            );
        }

        [TestMethod]
        public void InterpreterTestFunctionCallTransformation2()
        {
            RunInterpreter(
                @"
class start
    number y
        accessor:
            value = 100

    start()
        number x
        x = getValue1() + getValue2() + y
        console.print(x)
    
    getValue1() : number a
        a = 1

    getValue2() : number a
        a = 10".Replace("    ", "\t")
            );
        }

        [TestMethod]
        public void InterpreterTestPrint()
        {
            RunInterpreter(
                @"
class start
    start()
        console.print(""tran"")".Replace("    ", "\t")
            );
        }

        [TestMethod]
        public void InterpreterTestFields()
        {
            RunInterpreter(
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
        }

        [TestMethod]
        public void InterpreterTestAccessorAssignment()
        {
            RunInterpreter(
                @"
class start
    number x
        accessor:
            value = 100

    start()
        number a
        a = x + 10
        console.print(a)
        a = a + x + 100 + (x*2)
        a = (x+(x+x)+x)".Replace("    ", "\t")
            );
        }

        [TestMethod]
        public void InterpreterTestAccessorFunctionCall()
        {
            RunInterpreter(
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
        console.print(x)

    doStuff(number a, number b)
        number c
        c = a + b".Replace("    ", "\t")
            );
        }

        [TestMethod]
        public void InterpreterTestAccessorLoop()
        {
            RunInterpreter(
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
        }

        [TestMethod]
        public void InterpreterTestAccessorIf()
        {
            RunInterpreter(
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
        }

        [TestMethod]
        public void InterpreterTestMutators()
        {
            RunInterpreter(
                @"
class start
    number x
        accessor:
            value = x
        mutator:
            x = value

    start()
        x = 999
        console.print(x)".Replace("    ", "\t")
            );
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
            x = 100
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
            RunInterpreter(files);
        }

        [TestMethod]
        public void InterpreterTestObjects()
        {
            List<string> files = new List<string>();
            files.Add(
                @"
class start
    start()
        Student s = new()
        s.addGrade(100)
        console.print(s.grade)".Replace("    ", "\t")
            );
            files.Add(
                @"
class Student
    number grade
        accessor:
            value = grade
        mutator:
            grade = value

    construct()
        grade = 0

    addGrade(number score)
        grade = grade + score".Replace("    ", "\t")
            );
            RunInterpreter(files);
        }

        [TestMethod]
        public void InterpreterTestComplexExpressions()
        {
            RunInterpreter(
                @"
class tran
    number x
        accessor:
            value = x
        mutator:
            x = value

    start()
        number a
        a = 50
        x = 100
        a = x/a
        console.print(a)
        console.print(x)
        {a = ((x/a) + (a*2))/2
        console.print(a)
        a = getNumber()/2 + x+100 * (getNumber()*getNumber())
        console.print(a)
        number b
        a, b = getNumbers()
        console.print(a)
        console.print(b)}
        

    getNumber() : number retVal
        retVal = (x*5)/2

    getNumbers() : number ret1, number ret2
        ret1 = 100/2
        ret2 = 777+777".Replace("    ", "\t")
            );
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
        number a
        a = 1".Replace("    ", "\t")
            );
            RunInterpreter(files);
        }

        [TestMethod]
        public void InterpreterTestIfsAndLoops()
        {
            RunInterpreter(
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
        }

        [TestMethod]
        public void InterpreterTestFibonacci()
        {
            RunInterpreter(
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
        }
    }
}
