using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shank.ASTNodes;

namespace ShankUnitTests
{
    [TestClass]
    public class SemanticAnalysisModuleTest
    {
        public void initializeInterpreter(LinkedList<string[]> files)
        {
            ModuleBeforeInterpreterTests.initializeInterpreter(files);
        }

        public void runInterpreter()
        {
            ModuleInterpreterTests.runInterpreter();
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
            SemanticAnalysis.CheckModules();
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
            SemanticAnalysis.CheckModules();
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
            SemanticAnalysis.CheckModules();
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
            SemanticAnalysis.CheckModules();
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
            SemanticAnalysis.CheckModules();
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
            SemanticAnalysis.CheckModules();
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
            SemanticAnalysis.CheckModules();
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
            SemanticAnalysis.CheckModules();
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
            SemanticAnalysis.CheckModules();
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
            SemanticAnalysis.CheckModules();
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
            SemanticAnalysis.CheckModules();
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
            Interpreter.Reset();
            SemanticAnalysis.reset();
            ModuleNode module1 = ModuleParserTests.getModuleFromParser(file1);
            ModuleNode module2 = ModuleParserTests.getModuleFromParser(file2);
            module1.mergeModule(module2);
            module1.setName("default");
            Interpreter.Modules.Add("default", module1);
            Interpreter.setStartModule();
            BuiltInFunctions.Register(Interpreter.getStartModule().getFunctions());
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        // TODO: better bad import exceptions
        [ExpectedException(
            typeof(SemanticErrorException)
            // "Cannot create an enum of type colors as it was never exported"
        )]
        public void ImportedEnumPrivacy()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2\n",
                "define start()\n",
                "variables e : colors\n",
                "variables p : integer\n",
                "\tadd 1, 2, var p\n",
                "\twriteToTest p\n",
            };

            string[] file2 =
            {
                "module test2\n",
                "enum colors = [red, green, blue]\n",
                "export add\n",
                "define add(a, b : integer; var p : integer)\n",
                "\tp := a + b\n"
            };
            SemanticAnalysis.reset();
            Interpreter.Reset();
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            files.AddLast(file2);
            initializeInterpreter(files);
            //SemanticAnalysis.checkModules();
            runInterpreter();
        }

        [TestMethod]
        [ExpectedException(
            typeof(SemanticErrorException)
            // "Enums can only be compared to enums or enum variables of the same type."
        )]
        public void compareTwoDifferentEnumTypes()
        {
            string[] file1 =
            {
                "enum colors = [red, green, blue]\n",
                "enum tokens = [WORD, NUMBER]\n",
                "define start()\n",
                "variables c : colors\n",
                "variables t : tokens\n",
                "\tc := red\n",
                "\tt := WORD\n",
                "\tif c = t then\n",
                "\t\twriteToTest \"failed\"\n",
                "\telse\n",
                "\t\twriteToTest \"passed\"\n",
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void simpleRangeAssignmentCheck()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 1 to 10\n",
                "\ti := 4\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        [ExpectedException(
            typeof(Exception),
            "The variable i can only be assigned values from 1 to 10."
        )]
        public void simpleRangeAssignmentCheckFail()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 1 to 10\n",
                "\ti := 11\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void simpleRangeStringAssignmentCheck()
        {
            string[] file =
            {
                "define start()\n",
                "variables s : string from 0 to 3\n",
                "\ts := \"hi!\"\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "The variable s can only be a length from 0 to 10.")]
        public void simpleRangeStringAssignmentCheckFail()
        {
            string[] file =
            {
                "define start()\n",
                "variables s : string from 0 to 3\n",
                "\ts := \"helloworld!\"\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void RangeIntAssignedSingleVariable()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 0 to 7\n",
                "variables j : integer from 1 to 6\n",
                "\ti := j\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void RangeIntAssignedVariableExpression()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 0 to 10\n",
                "variables j : integer from 1 to 4\n",
                "variables k : integer from 0 to 2",
                "\ti := j + k\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void RangeIntFunctionParams()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 0 to 10\n",
                "\tsmallAdd 1, 2, var i\n",
                "define smallAdd(a, b : integer from 0 to 5; var c : integer from 0 to 10)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void RangeIntAssignedVariableAndInt()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 0 to 7\n",
                "variables j : integer from 1 to 4\n",
                "\ti := j + 2\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void RangeIntAssignedSingleVariableFail()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 0 to 7\n",
                "variables j : integer from 1 to 8\n",
                "\ti := j\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void RangeIntAssignedVariableExpressionFail()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 0 to 10\n",
                "variables j : integer from 1 to 4\n",
                "variables k : integer from 0 to 7\n",
                "\ti := j + k\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void RangeIntFunctionParamsFail()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 0 to 11\n",
                "\tsmallAdd 6, 2, var i\n",
                "define smallAdd(a, b : integer from 0 to 5; var c : integer from 0 to 10)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void RangeIntFunctionParamPass()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 0 to 10\n",
                "\tsmallAdd 1, 2, var i\n",
                "define smallAdd(a, b : integer from 0 to 5; var c : integer from 0 to 10)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void RangeStringFunctionParamFail()
        {
            string[] file =
            {
                "define start()\n",
                "variables s : string from 0 to 20\n",
                "\ts := \"panda\"",
                "\twriteStr s\n",
                "define writeStr(s : string from 0 to 15)\n",
                "\twrite s\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void RangeStringFunctionParamPass()
        {
            string[] file =
            {
                "define start()\n",
                "variables s : string from 0 to 8\n",
                "\ts := \"panda\"",
                "\twriteStr s\n",
                "define writeStr(s : string from 0 to 15)\n",
                "\twrite s\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void RangeIntAssignedVariableAndIntFail()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 0 to 7\n",
                "variables j : integer from 1 to 4\n",
                "\ti := j + 7\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void RangeVariableAssignedBigExpression()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 0 to 20\n",
                "variables j : integer from 1 to 4\n",
                "variables k : integer from 3 to 5\n",
                "\tj := 2\n",
                "\tk := 3\n",
                "\ti := j + 7 / k * 2\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void RangeVariableModuleInt()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer from 0 to 7\n",
                "variables j : integer from 1 to 4\n",
                "\ti :=  1293891 mod j\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }
    }
}
