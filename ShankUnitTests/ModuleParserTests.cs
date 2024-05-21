using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shank;

namespace ShankUnitTests
{
    [TestClass]
    public class ModuleParserTests
    {
        public static ModuleNode getModuleFromParser(string[] code)
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
            string[] code =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n"
            };
            ModuleNode m = getModuleFromParser(code);
            Assert.AreNotEqual(m, null);
            Assert.AreEqual(m.getFunctions().Count, 1);
        }

        [TestMethod]
        public void singleIsModule()
        {
            string[] code =
            {
                "module test1\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n"
            };
            ModuleNode m = getModuleFromParser(code);
            Assert.AreEqual(m.getName(), "test1");
        }

        [TestMethod]
        public void simpleImport()
        {
            string[] code =
            {
                "module test1\n",
                "import test2\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n"
            };
            ModuleNode m = getModuleFromParser(code);
            //if imports don't have functions listed, they are added to the linked list in the dictonary with the key of their module name
            //between the parser and interpreter
            Assert.IsTrue(m.getImportNames().ContainsKey("test2"));
            Assert.AreEqual(m.getImportNames()["test2"].Count, 0);
        }

        [TestMethod]
        public void simpleImportAtEnd()
        {
            string[] code =
            {
                "module test1\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n",
                "import test2\n"
            };
            ModuleNode m = getModuleFromParser(code);
            //if imports don't have functions listed, they are added to the linked list in the dictonary with the key of their module name
            //between the parser and interpreter
            Assert.IsTrue(m.getImportNames().ContainsKey("test2"));
            Assert.AreEqual(m.getImportNames()["test2"].Count, 0);
        }

        //TODO: make sure import and export are followed by a check for an end of line
        [TestMethod]
        public void simpleImportFunctions()
        {
            string[] code =
            {
                "module test1\n",
                "import test2[add]\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n"
            };
            ModuleNode m = getModuleFromParser(code);
            Assert.AreEqual(m.getImportNames().Count, 1);
            Assert.IsTrue(m.getImportNames().ContainsKey("test2"));
            Assert.AreEqual(m.getImportNames()["test2"].First.Value, "add");
        }

        [TestMethod]
        public void simpleImportFunctionsAtEnd()
        {
            string[] code =
            {
                "module test1\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n",
                "import test2[add]\n"
            };
            ModuleNode m = getModuleFromParser(code);
            Assert.AreEqual(m.getImportNames().Count, 1);
            Assert.IsTrue(m.getImportNames().ContainsKey("test2"));
            Assert.AreEqual(m.getImportNames()["test2"].First.Value, "add");
        }

        [TestMethod]
        public void simpleImportMultipleFunctions()
        {
            string[] code =
            {
                "module test1\n",
                "import test2[add, addFunc]\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n"
            };
            ModuleNode m = getModuleFromParser(code);
            Assert.AreEqual(m.getImportNames().Count, 1);
            Assert.IsTrue(m.getImportNames().ContainsKey("test2"));
            Assert.AreEqual(m.getImportNames()["test2"].Count, 2);
            Assert.AreEqual(m.getImportNames()["test2"].First.Value, "add");
            Assert.AreEqual(m.getImportNames()["test2"].Last.Value, "addFunc");
        }

        [TestMethod]
        public void simpleImportMultipleFunctionsAtEnd()
        {
            string[] code =
            {
                "module test1\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n",
                "import test2[add, addFunc]\n"
            };
            ModuleNode m = getModuleFromParser(code);
            Assert.AreEqual(m.getImportNames().Count, 1);
            Assert.IsTrue(m.getImportNames().ContainsKey("test2"));
            Assert.AreEqual(m.getImportNames()["test2"].Count, 2);
            Assert.AreEqual(m.getImportNames()["test2"].First.Value, "add");
            Assert.AreEqual(m.getImportNames()["test2"].Last.Value, "addFunc");
        }

        [TestMethod]
        public void simpleExport()
        {
            string[] code =
            {
                "module test2\n",
                "export add\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            ModuleNode m = getModuleFromParser(code);
            Assert.AreEqual(m.getExportNames().Count, 1);
            Assert.IsTrue(m.getExportNames().Contains("add"));
        }

        [TestMethod]
        public void simpleExportAtEnd()
        {
            string[] code =
            {
                "module test2\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "export add\n"
            };
            ModuleNode m = getModuleFromParser(code);
            Assert.AreEqual(m.getExportNames().Count, 1);
            Assert.IsTrue(m.getExportNames().Contains("add"));
        }

        [TestMethod]
        public void simpleMultipleExport()
        {
            string[] code =
            {
                "module test2\n",
                "export add, addThree\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "\n",
                "define addThree(a : integer; var c : integer)\n",
                "\t c := a + 3\n"
            };
            ModuleNode m = getModuleFromParser(code);
            Assert.AreEqual(m.getExportNames().Count, 2);
            Assert.IsTrue(m.getExportNames().Contains("add"));
            Assert.IsTrue(m.getExportNames().Contains("addThree"));
        }

        [TestMethod]
        public void simplMultipleeExportAtEnd()
        {
            string[] code =
            {
                "module test2\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "\n",
                "define addThree(a : integer; var c : integer)\n",
                "\t c := a + 3\n",
                "export add, addThree\n"
            };
            ModuleNode m = getModuleFromParser(code);
            Assert.AreEqual(m.getExportNames().Count, 2);
            Assert.IsTrue(m.getExportNames().Contains("add"));
            Assert.IsTrue(m.getExportNames().Contains("addThree"));
        }

        [TestMethod]
        public void mergeUnnamedModule()
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

            ModuleNode module1 = getModuleFromParser(file1);
            ModuleNode module2 = getModuleFromParser(file2);
            module1.mergeModule(module2);

            Assert.AreEqual(module1.getFunctions().Count, 3);
            Assert.IsTrue(module1.getFunctions().ContainsKey("start"));
            Assert.IsTrue(module1.getFunctions().ContainsKey("add"));
            Assert.IsTrue(module1.getFunctions().ContainsKey("addThree"));
        }

        [TestMethod]
        public void ProgramMergeModuleLogic()
        {
            Interpreter.Reset();
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
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            files.AddLast(file2);
            foreach (var lines in files)
            {
                var tokens = new List<Token>();
                var l = new Lexer();
                tokens.AddRange(l.Lex(lines));

                var p = new Parser(tokens);

                var brokeOutOfWhile = false;
                while (tokens.Any())
                {
                    //FunctionNode? fn = null;
                    ModuleNode module = null;
                    var parserErrorOccurred = false;

                    try
                    {
                        module = p.Module();
                        //if the file never declares itself as module, give it a unique digit name
                        //the digit
                        if (module.getName() == null)
                        {
                            if (Interpreter.getModules().ContainsKey("default"))
                                Interpreter.getModules()["default"].mergeModule(module);
                            else
                            {
                                module.setName("default");
                                Interpreter.Modules.Add("default", module);
                            }
                        }
                    }
                    catch (SyntaxErrorException e)
                    {
                        Console.WriteLine(
                            $"\nParsing error encountered in mergeModule unit test:\n{e}\nskipping..."
                        );
                        parserErrorOccurred = true;
                    }

                    if (parserErrorOccurred)
                    {
                        brokeOutOfWhile = true;
                        break;
                    }
                    if (module.getName() != null && module.getName() != "default")
                        Interpreter.Modules.Add(module.getName(), module);
                }

                // Parser error occurred -- skip to next file.
                if (brokeOutOfWhile)
                {
                    continue;
                }
            }
            Assert.AreEqual(Interpreter.getModules().Count, 1);

            Assert.IsTrue(Interpreter.getModules().ContainsKey("default"));
            Assert.AreEqual(Interpreter.getModules()["default"].getFunctions().Count, 3);

            Assert.IsTrue(Interpreter.getModules()["default"].getFunctions().ContainsKey("start"));
            Assert.IsTrue(Interpreter.getModules()["default"].getFunctions().ContainsKey("add"));
            Assert.IsTrue(
                Interpreter.getModules()["default"].getFunctions().ContainsKey("addThree")
            );
        }

        [TestMethod]
        public void SimpleEnumExport()
        {
            string[] code =
            {
                "module test2\n",
                "enum colors = [red, green, blue]\n",
                "export colors\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            ModuleNode m = getModuleFromParser(code);
            Assert.AreEqual(m.getExportNames().Count, 1);
            Assert.IsTrue(m.getExportNames().Contains("colors"));
        }

        [TestMethod]
        public void SimpleEnumImport()
        {
            string[] code =
            {
                "module test1\n",
                "import test2 [colors]\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp:=3\n",
                "\twrite p\n"
            };
            ModuleNode m = getModuleFromParser(code);
            //if imports don't have functions listed, they are added to the linked list in the dictonary with the key of their module name
            //between the parser and interpreter
            Assert.IsTrue(m.getImportNames().ContainsKey("test2"));
            Assert.AreEqual(m.getImportNames()["test2"].Count, 1);
        }
    }
}
