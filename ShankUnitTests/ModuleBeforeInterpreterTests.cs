using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShankUnitTests
{
    [TestClass]
    public class ModuleBeforeInterpreterTests
    {
        public Dictionary<string, ModuleNode> getModulesFromParser(LinkedList<string[]> list)
        {
            Dictionary<string, ModuleNode> Modules = new Dictionary<string, ModuleNode>();
            Lexer l = new Lexer();
            foreach (string[] file in list)
            {
                Parser p = new Parser(l.Lex(file));
                ModuleNode m = p.Module();
                Modules.Add(m.getName(), m);
            }
            return Modules;
        }

        public void initializeInterpreter(LinkedList<string[]> files)
        {
            Interpreter.reset();
            Dictionary<string, ModuleNode> modules = getModulesFromParser(files);
            Interpreter.setModules(modules);
            Interpreter.setStartModule();
            Interpreter.handleExports();
        }

        [TestMethod]
        public void simpleHandleExports()
        {
            string[] file2 =
            {
                "module test2\n",
                "export add\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> list = new LinkedList<string[]>();
            list.AddFirst(file2);
            initializeInterpreter(list);
            Assert.AreEqual(Interpreter.getModules()["test2"].getExportedFunctions().Count, 1);
            Assert.IsTrue(Interpreter.getModules()["test2"].getExportedFunctions().ContainsKey("add"));
        }

        [TestMethod]
        public void multipleHandleExports()
        {
            string[] file1 =
            {
                "module test1\n",
                "export sub\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n",
                "define sub(a,b : integer; var c : integer)\n",
                "\tc := a - b\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "export add\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> list = new LinkedList<string[]>();
            list.AddFirst(file1);
            list.AddLast(file2);
            initializeInterpreter(list);
            Assert.AreEqual(Interpreter.getModules()["test1"].getExportedFunctions().Count, 1);
            Assert.AreEqual(Interpreter.getModules()["test2"].getExportedFunctions().Count, 1);

            Assert.IsTrue(Interpreter.getModules()["test1"].getExportedFunctions().ContainsKey("sub"));
            Assert.IsTrue(Interpreter.getModules()["test2"].getExportedFunctions().ContainsKey("add"));
        }

        [TestMethod]
        public void exportinMultipleFunctions()
        {
            string[] file2 =
            {
                "module test2\n",
                "export add, sub\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "define sub(a,b : integer; var c : integer)\n",
                "\tc := a - b\n"
            };
            LinkedList<string[]> list = new LinkedList<string[]>();
            list.AddLast(file2);
            initializeInterpreter(list);
            Assert.AreEqual(Interpreter.getModules()["test2"].getExportedFunctions().Count, 2);

            Assert.IsTrue(Interpreter.getModules()["test2"].getExportedFunctions().ContainsKey("sub"));
            Assert.IsTrue(Interpreter.getModules()["test2"].getExportedFunctions().ContainsKey("add"));
        }

        [TestMethod]
        public void simpleHandleImport()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "export add\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> list = new LinkedList<string[]>();
            list.AddFirst(file1);
            list.AddLast(file2);
            initializeInterpreter(list);
            Interpreter.handleImports();

            Assert.AreEqual(Interpreter.getModules()["test1"].getImportNames().Count, 1);
            Assert.AreEqual(Interpreter.getModules()["test1"].getImportNames()["test2"].Count, 1);
            Assert.IsTrue(Interpreter.getModules()["test1"].getImportNames()["test2"].Contains("add"));

        }

        [TestMethod]
        public void multipleHandleImport()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "export add, sub\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "define sub(a,b : integer; var c : integer)\n",
                "\tc := a - b\n"
            };

            LinkedList<string[]> list = new LinkedList<string[]>();
            list.AddFirst(file1);
            list.AddLast(file2);
            initializeInterpreter(list);
            Interpreter.handleImports();

            Assert.AreEqual(Interpreter.getModules()["test1"].getImportNames().Count, 1);
            Assert.AreEqual(Interpreter.getModules()["test1"].getImportNames()["test2"].Count, 2);

            Assert.IsTrue(Interpreter.getModules()["test1"].getImportNames()["test2"].Contains("add"));
            Assert.IsTrue(Interpreter.getModules()["test1"].getImportNames()["test2"].Contains("sub"));
        }

        [TestMethod]
        public void importSelectFunctions()
        {
            string[] file1 = {"module test1\n",
                             "import test2 [add]\n",
                             "define start()\n",
                             "variables p : integer\n",
                                "\tp:=3\n",
                                "\twrite p\n"};
            string[] file2 = { "module test2\n",
                              "export add, sub\n",
                              "define add(a, b : integer; var c : integer)\n",
                                 "\tc := a + b\n",
                              "define sub(a,b : integer; var c : integer)\n",
                                 "\tc := a - b\n" };

            LinkedList<string[]> list = new LinkedList<string[]>();
            list.AddFirst(file1);
            list.AddLast(file2);
            initializeInterpreter(list);
            Interpreter.handleImports();

            //the list of available functions from test2 should only be 1
            Assert.AreEqual(Interpreter.getModules()["test1"].getImportNames()["test2"].Count, 1);

            //module test1 importNames should have add, and should not have sub
            Assert.IsTrue(Interpreter.getModules()["test1"].getImportNames()["test2"].Contains("add"));
            Assert.IsFalse(Interpreter.getModules()["test1"].getImportNames()["test2"].Contains("sub"));

            //both add and sub should be present in the importedFunctions
            Assert.IsTrue(Interpreter.getModules()["test1"].getImportedFunctions().ContainsKey("add"));
            Assert.IsTrue(Interpreter.getModules()["test1"].getImportedFunctions().ContainsKey("sub"));
        }

        [TestMethod]
        public void chainHandleImports()
        {
            string[] file1 =
            {
                "module test1\n",
                "import test2\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n"
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

            LinkedList<string[]> list = new LinkedList<string[]>();
            list.AddFirst(file1);
            list.AddLast(file2);
            list.AddLast(file3);
            initializeInterpreter(list);
            Interpreter.handleImports();

            //module test1 should have both add and addFunc, but should only be able to call add
            Assert.AreEqual(Interpreter.getModules()["test1"].getImportNames().Count, 1);
            Assert.AreEqual(Interpreter.getModules()["test1"].getImportedFunctions().Count, 2);

            //test1 should only have add in its available functions to run from test2
            Assert.AreEqual(Interpreter.getModules()["test1"].getImportNames()["test2"].Count, 1);
            Assert.IsTrue(Interpreter.getModules()["test1"].getImportNames()["test2"].Contains("add"));
            Assert.IsFalse(Interpreter.getModules()["test1"].getImportNames()["test2"].Contains("addFunc"));

            //test1 should have access to all functions in test2, but shouldn't be able to call any functions not located
            //in its import dictionary
            Assert.IsTrue(Interpreter.getModules()["test1"].getImportedFunctions().ContainsKey("add"));
            Assert.IsTrue(Interpreter.getModules()["test1"].getImportedFunctions().ContainsKey("addFunc"));
        }
    }
}
