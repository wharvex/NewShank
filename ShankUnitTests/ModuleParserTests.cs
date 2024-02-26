using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shank;
using System.Data;

namespace ModuleUnitTests
{
    [TestClass]
    public class ModuleParserTests
    {
        public ModuleNode getModuleFromLexer(string[] code)
        {
            Lexer lexer = new Lexer();
            ModuleNode m;
            try
            {
                Parser p = new Parser(lexer.Lex(code));
                m = p.Module();
            }
            catch (SyntaxErrorException e)
            {
                Console.Write("Error in module test 1:\n");
                throw e;
            }
            return m;
        }
        [TestMethod]
        public void singleNonModule()
        {
            string[] code = { "define start()\n",
                              "variables p : integer\n", 
                                    "\tp:=3\n", 
                                    "\twrite p\n" };
            ModuleNode m = getModuleFromLexer(code);
            Assert.AreNotEqual(m, null);
            Assert.AreEqual(m.getFunctions().Count, 1);
        }

        [TestMethod]
        public void singleIsModule()
        {
            string[] code = {"module test1\n",
                            "define start()\n",
                            "variables p : integer\n",
                                "\tp:=3\n",
                                "\twrite p\n" };
            ModuleNode m = getModuleFromLexer(code);
            Assert.AreEqual(m.getName(), "test1");
        }
        [TestMethod]
        public void simpleImport()
        {
            string[] code = { "module test1\n",
                              "import test2\n",
                              "define start()\n",
                              "variables p : integer\n",
                                    "\tp:=3\n", "\twrite p\n" };
            ModuleNode m = getModuleFromLexer(code);
            //if imports don't have functions listed, they are added to the linked list in the dictonary with the key of their module name
            //between the parser and interpreter
            Assert.IsTrue(m.getImportDict().ContainsKey("test2"));
            Assert.AreEqual(m.getImportDict()["test2"].Count, 0);
        }
        [TestMethod]
        public void simpleImportAtEnd()
        {
            string[] code = { "module test1\n",
                              "define start()\n",
                              "variables p : integer\n",
                                    "\tp:=3\n", 
                                    "\twrite p\n",
                              "import test2\n" };
            ModuleNode m = getModuleFromLexer(code);
            //if imports don't have functions listed, they are added to the linked list in the dictonary with the key of their module name
            //between the parser and interpreter
            Assert.IsTrue(m.getImportDict().ContainsKey("test2"));
            Assert.AreEqual(m.getImportDict()["test2"].Count, 0);
        }
        //TODO: make sure import and export are followed by a check for an end of line
        [TestMethod]
        public void simpleImportFunctions()
        {
            string[] code = { "module test1\n",
                              "import test2[add]\n",
                              "define start()\n",
                              "variables p : integer\n",
                                    "\tp:=3\n",
                                    "\twrite p\n" };
            ModuleNode m = getModuleFromLexer(code);
            Assert.AreEqual(m.getImportDict().Count, 1);
            Assert.IsTrue(m.getImportDict().ContainsKey("test2"));
            Assert.AreEqual(m.getImportDict()["test2"].First.Value, "add");
        }

        [TestMethod]
        public void simpleImportFunctionsAtEnd()
        {
            string[] code = { "module test1\n",
                              "define start()\n",
                              "variables p : integer\n",
                                    "\tp:=3\n",
                                    "\twrite p\n",
                              "import test2[add]\n" };
            ModuleNode m = getModuleFromLexer(code);
            Assert.AreEqual(m.getImportDict().Count, 1);
            Assert.IsTrue(m.getImportDict().ContainsKey("test2"));
            Assert.AreEqual(m.getImportDict()["test2"].First.Value, "add");
        }
        [TestMethod]
        public void simpleImportMultipleFunctions()
        {
            string[] code = { "module test1\n",
                              "import test2[add, addFunc]\n",
                              "define start()\n",
                              "variables p : integer\n",
                                    "\tp:=3\n",
                                    "\twrite p\n" };
            ModuleNode m = getModuleFromLexer(code);
            Assert.AreEqual(m.getImportDict().Count, 1);
            Assert.IsTrue(m.getImportDict().ContainsKey("test2"));
            Assert.AreEqual(m.getImportDict()["test2"].Count, 2);
            Assert.AreEqual(m.getImportDict()["test2"].First.Value, "add");
            Assert.AreEqual(m.getImportDict()["test2"].Last.Value, "addFunc");
        }

        [TestMethod]
        public void simpleImportMultipleFunctionsAtEnd()
        {
            string[] code = { "module test1\n",
                              "define start()\n",
                              "variables p : integer\n",
                                    "\tp:=3\n",
                                    "\twrite p\n",
                              "import test2[add, addFunc]\n" };
            ModuleNode m = getModuleFromLexer(code);
            Assert.AreEqual(m.getImportDict().Count, 1);
            Assert.IsTrue(m.getImportDict().ContainsKey("test2"));
            Assert.AreEqual(m.getImportDict()["test2"].Count, 2);
            Assert.AreEqual(m.getImportDict()["test2"].First.Value, "add");
            Assert.AreEqual(m.getImportDict()["test2"].Last.Value, "addFunc");

        }
        [TestMethod]
        public void simpleExport()
        {
            string[] code = { "module test2\n",
                              "export add\n",
                              "define add(a, b : integer; var c : integer)\n",
                                    "\tc := a + b\n"};
            ModuleNode m = getModuleFromLexer(code);
            Assert.AreEqual(m.getExportList().Count, 1);
            Assert.IsTrue(m.getExportList().Contains("add"));
        }

        [TestMethod]
        public void simpleExportAtEnd()
        {
            string[] code = { "module test2\n",
                              "define add(a, b : integer; var c : integer)\n",
                                    "\tc := a + b\n",
                              "export add\n" };
            ModuleNode m = getModuleFromLexer(code);
            Assert.AreEqual(m.getExportList().Count, 1);
            Assert.IsTrue(m.getExportList().Contains("add"));
        }
        [TestMethod]
        public void simpleMultipleExport()
        {
            string[] code = { "module test2\n",
                              "export add, addThree\n",
                              "define add(a, b : integer; var c : integer)\n",
                                    "\tc := a + b\n","\n",
                              "define addThree(a : integer; var c : integer)\n",
                                    "\t c := a + 3\n" };
            ModuleNode m = getModuleFromLexer(code);
            Assert.AreEqual(m.getExportList().Count, 2);
            Assert.IsTrue(m.getExportList().Contains("add"));
            Assert.IsTrue(m.getExportList().Contains("addThree"));

        }

        [TestMethod]
        public void simplMultipleeExportAtEnd()
        {
            string[] code = { "module test2\n",
                              "define add(a, b : integer; var c : integer)\n",
                                    "\tc := a + b\n",
                              "\n",
                              "define addThree(a : integer; var c : integer)\n",
                                    "\t c := a + 3\n",
                              "export add, addThree\n" };
            ModuleNode m = getModuleFromLexer(code);
            Assert.AreEqual(m.getExportList().Count, 2);
            Assert.IsTrue(m.getExportList().Contains("add"));
            Assert.IsTrue(m.getExportList().Contains("addThree"));
        }

    }
}