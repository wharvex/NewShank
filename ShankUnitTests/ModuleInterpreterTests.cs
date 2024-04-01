using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShankUnitTests
{
    [TestClass]
    public class ModuleInterpreterTests
    {
        private int unnamedModuleCount = 0;

        public Dictionary<string, ModuleNode> getModulesFromParser(LinkedList<string[]> list)
        {
            return ModuleBeforeInterpreterTests.getModulesFromParser(list);
        }

        public void initializeInterpreter(LinkedList<string[]> files)
        {
            Interpreter.reset();
            SemanticAnalysis.reset();
            Dictionary<string, ModuleNode> modules = getModulesFromParser(files);
            Interpreter.setModules(modules);
            Interpreter.setStartModule();
            SemanticAnalysis.setStartModule();
        }

        public void runInterpreter()
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
                    SemanticAnalysis.checkModules();
                    try
                    {
                        Interpreter.InterpretFunction(s, new List<InterpreterDataType>());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(
                            $"\nInterpretation error encountered in file {currentModulePair.Key}:\n{e}\nskipping..."
                        );
                        interpreterErrorOccurred = true;
                    }

                    if (interpreterErrorOccurred)
                    {
                        continue;
                    }
                }
            }
        }

        //ensuring the base case still works
        [TestMethod]
        public void simpleInterpreterTest()
        {
            string[] file1 =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twriteToTest p\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddFirst(file1);
            initializeInterpreter(files);
            runInterpreter();
            int.TryParse(Interpreter.testOutput[0].ToString(), out int j);
            Assert.AreEqual(j, 3);
        }

        //one function export to a simple import
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
            runInterpreter();
            int.TryParse(Interpreter.testOutput[0].ToString(), out int j);
            Assert.AreEqual(j, 6);
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
            runInterpreter();
            int.TryParse(Interpreter.testOutput[0].ToString(), out int j);
            Assert.AreEqual(j, 6);
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
            runInterpreter();
            int.TryParse(Interpreter.testOutput[0].ToString(), out int j);
            Assert.AreEqual(j, 6);
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
            runInterpreter();
            int.TryParse(Interpreter.testOutput[0].ToString(), out int j);
            Assert.AreEqual(j, 6);
            int.TryParse(Interpreter.testOutput[2].ToString(), out int f);
            Assert.AreEqual(f, 4);
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
            runInterpreter();
            int.TryParse(Interpreter.testOutput[0].ToString(), out int j);
            Assert.AreEqual(j, 6);
            int.TryParse(Interpreter.testOutput[2].ToString(), out int f);
            Assert.AreEqual(f, 6);
        }
    }
}
