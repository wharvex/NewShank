using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShankUnitTests
{
    [TestClass]
    public class SemanticAnalysisModuleTest
    {
        public void initializeInterpreter(LinkedList<string[]> files)
        {
            ModuleBeforeInterpreterTests.initializeInterpreter(files);
        }

        [TestMethod]
        public void preliminaryTest()
        {
            //write an interpreter test where a function in a different module from start() used a built in function
            string[] code =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(code);
            initializeInterpreter(files);
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }

        [TestMethod]
        public void simpleImportAndExport()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2\n",
                "define start()\n",
                "variables p : integer\n",
                "\tadd 4,2, var p\n",
                "\twriteToTest p\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "export add\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddFirst(file1);
            files.AddLast(file2);
            initializeInterpreter(files);
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }

        //importing from a module that also imports
        //ensures that test1 can see the functions of test3, but cannot use them
        //test2 can use the functions from test3, as it imported test3
        [TestMethod]
        public void chainedImportInterpreter()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2\n",
                "define start()\n",
                "variables p : integer\n",
                "\tadd 4, 2, var p\n",
                "\twriteToTest p\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "export add\n",
                "import test3\n",
                "define add(a, b : integer; var c : integer)\n",
                "variables p : integer\n",
                "\taddFunc a, b, var p\n",
                "\tc := p\n"
            };
            string[] file3 =
            {
                "module test3\n",
                "export addFunc\n",
                "define addFunc(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddFirst(file1);
            files.AddLast(file2);
            files.AddLast(file3);

            initializeInterpreter(files);
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }

        [TestMethod]
        //Passes the tests into the interpreter out of order. This simulates if the directory had file3, then file2, then file1.
        public void chainImportOutOfOrder()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2\n",
                "define start()\n",
                "variables p : integer\n",
                "\tadd 4, 2, var p\n",
                "\twriteToTest p\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "export add\n",
                "import test3\n",
                "define add(a, b : integer; var c : integer)\n",
                "variables p : integer\n",
                "\taddFunc a, b, var p\n",
                "\tc := p\n"
            };
            string[] file3 =
            {
                "module test3\n",
                "export addFunc\n",
                "define addFunc(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file3);
            files.AddLast(file2);
            files.AddLast(file1);

            initializeInterpreter(files);
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }

        [TestMethod]
        public void importDirectlyAndThroughChain()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2\n",
                "import test3 [sub]\n",
                "define start()\n",
                "variables p, j : integer\n",
                "\tadd 4, 2, var p\n",
                "\twriteToTest p\n",
                "\tsub 7, 3, var j\n",
                "\twriteToTest j\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "export add\n",
                "import test3 [addFunc]\n",
                "define add(a, b : integer; var c : integer)\n",
                "variables p : integer\n",
                "\taddFunc a, b, var p\n",
                "\tc := p\n"
            };
            string[] file3 =
            {
                "module test3\n",
                "export addFunc, sub\n",
                "define addFunc(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "define sub(a, b : integer; var c : integer)\n",
                "\tc:= a - b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file3);
            files.AddLast(file2);
            files.AddLast(file1);

            initializeInterpreter(files);
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }

        [TestMethod]
        public void callBuiltInFuncFromDifferentModule()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2\n",
                "define start()\n",
                "variables p, j : integer\n",
                "\tadd 4, 2, var p\n",
                "\twriteToTest p\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "export add\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "\twriteToTest c\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            files.AddLast(file2);
            initializeInterpreter(files);
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }

        //CATCHING ERRORS TESTS
        [TestMethod]
        [ExpectedException(
            typeof(Exception),
            "Could not find a definition for the function add. Make sure it was defined and properly exported if it was imported."
        )]
        public void undefinedFunctionCall()
        {
            string[] file1 =
            {
                "define start()\n",
                "variables p, j : integer\n",
                "\tadd 4, 2, var p\n",
                "\twriteToTest p\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            initializeInterpreter(files);
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }

        [TestMethod]
        [ExpectedException(
            typeof(Exception),
            "Cannot export addFunc from the module test2 as it wasn't defined in that file."
        )]
        public void catchBadExport()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2\n",
                "define start()\n",
                "variables p : integer\n",
                "\tadd 4,2, var p\n",
                "\twriteToTest p\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "export addFunc\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            files.AddLast(file2);
            initializeInterpreter(files);
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "Module test3 does not exist")]
        public void catchBadImportWholeModule()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test3\n",
                "define start()\n",
                "variables p : integer\n",
                "\tadd 4,2, var p\n",
                "\twriteToTest p\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "export add\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            files.AddLast(file2);
            initializeInterpreter(files);
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }

        [TestMethod]
        [ExpectedException(
            typeof(Exception),
            "The function addFunc does not exist in module test2."
        )]
        public void catchBadImportSingleFunction()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2 [addFunc]\n",
                "define start()\n",
                "variables p : integer\n",
                "\tadd 4,2, var p\n",
                "\twriteToTest p\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "export add\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            files.AddLast(file2);
            initializeInterpreter(files);
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "The module test2 doesn't export the function add.")]
        public void catchNotExported()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2 [add]\n",
                "define start()\n",
                "variables p : integer\n",
                "\tadd 4,2, var p\n",
                "\twriteToTest p\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            files.AddLast(file2);
            initializeInterpreter(files);
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }

        [TestMethod]
        public void mergeUnnamedModules()
        {
            string[] file1 =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n",
            };
            string[] file2 =
            {
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "\n",
                "define addThree(a : integer; var c : integer)\n",
                "\t c := a + 3\n"
            };
            Interpreter.reset();
            ModuleNode module1 = ModuleParserTests.getModuleFromLexer(file1);
            ModuleNode module2 = ModuleParserTests.getModuleFromLexer(file2);
            module1.mergeModule(module2);
            module1.setName("default");
            Interpreter.Modules.Add("default", module1);
            Interpreter.setStartModule();
            BuiltInFunctions.Register(Interpreter.getStartModule().getFunctions());
            SemanticAnalysis.checkModules(Interpreter.getModules());
        }
    }
}
