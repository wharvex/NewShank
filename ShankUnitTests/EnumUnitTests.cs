using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shank.ASTNodes;

namespace ShankUnitTests
{
    [TestClass]
    public class EnumUnitTests
    {
        public ModuleNode getModuleFromParser(string[] file)
        {
            return ModuleParserTests.getModuleFromParser(file);
        }

        public static void initializeInterpreter(LinkedList<string[]> files)
        {
            ModuleInterpreterTests.initializeInterpreter(files);
        }

        public static void runInterpreter()
        {
            ModuleInterpreterTests.runInterpreter();
        }

        [TestMethod]
        public void simpleEnumParse()
        {
            string[] file =
            {
                "enum colors = [red]\n",
                "define start()\n",
                "variables i : integer\n",
                "\ti := 3\n"
            };
            ModuleNode module = getModuleFromParser(file);
            Assert.IsNotNull(module);
            Assert.AreEqual(1, module.getEnums().Count());
            Assert.IsTrue(module.getEnums().ContainsKey("colors"));
            Assert.AreEqual(1, module.getEnums()["colors"].NewType.Variants.Count);
        }

        [TestMethod]
        public void simpleEnumParseMultipleElements()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables i : integer\n",
                "\ti := 3\n"
            };
            ModuleNode module = getModuleFromParser(file);
            Assert.IsNotNull(module);
            Assert.AreEqual(1, module.getEnums().Count());
            Assert.IsTrue(module.getEnums().ContainsKey("colors"));
            Assert.AreEqual(3, module.getEnums()["colors"].NewType.Variants.Count);
        }

        [TestMethod]
        public void parseEnumAfterFunction()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer\n",
                "\ti := 3\n",
                "enum colors = [red, green, blue]\n"
            };
            ModuleNode module = getModuleFromParser(file);
            Assert.IsNotNull(module);
            Assert.AreEqual(1, module.getEnums().Count());
            Assert.IsTrue(module.getEnums().ContainsKey("colors"));
            Assert.AreEqual(3, module.getEnums()["colors"].NewType.Variants.Count);
        }

        //checking if the line "e := red" would be parsed without throwing an error
        [TestMethod]
        public void parseEnumVariable()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables i : integer\n",
                "variables e : colors\n",
                "\ti := 3\n",
                "\te := red\n"
            };
            ModuleNode module = getModuleFromParser(file);
            Assert.IsNotNull(module);
        }

        [TestMethod]
        public void enumDeclarationSemanticAnalysis()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables i : integer\n",
                "\ti := 3\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void enumVariableCreationSemanticAnalysis()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables i : integer\n",
                "variables e : colors\n",
                "\ti := 3\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void enumVariableAssignmentSemanticAnalysis()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables i : integer\n",
                "variables e : colors\n",
                "\ti := 3\n",
                "\te := red\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void enumVariableCompareSemanticAnalysis()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables i : integer\n",
                "variables e : colors\n",
                "\ti := 3\n",
                "\te := red\n",
                "\tif e = red then\n",
                "\t\twrite \"red\"\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void simpleEnumInterpreter()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables i : integer\n",
                "variables e : colors\n",
                "\ti := 3\n",
                "\te := red\n",
                "\twriteToTest e\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
            runInterpreter();
            Console.Write(Interpreter.testOutput.ToString());
            Assert.AreEqual("red ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void EnumAtEndInterpreter()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer\n",
                "variables e : colors\n",
                "\ti := 3\n",
                "\te := red\n",
                "\twriteToTest e\n",
                "enum colors = [red, green, blue]\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
            runInterpreter();
            Console.Write(Interpreter.testOutput.ToString());
            Assert.AreEqual("red ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void CompareEnum()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables e : colors\n",
                "\te := red\n",
                "\tif e = red then\n",
                "\t\twriteToTest \"true\"\n",
                "\telse\n",
                "\t\twriteToTest \"false\""
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
            runInterpreter();
            Console.Write(Interpreter.testOutput.ToString());
            Assert.AreEqual("true ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void CompareEnumVariables()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables e, s : colors\n",
                "\te := red\n",
                "\ts := blue\n",
                "\tif e = s then\n",
                "\t\twriteToTest \"true\"\n",
                "\telse\n",
                "\t\twriteToTest \"false\""
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);

            SemanticAnalysis.CheckModules();
            runInterpreter();
            Console.Write(Interpreter.testOutput.ToString());
            Assert.AreEqual("false ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(SemanticErrorException), "Cannot compare two enum elements")]
        public void CompareEnumValues()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables e : colors\n",
                "\te := red\n",
                "\tif red = red then\n",
                "\t\twriteToTest \"true\"\n",
                "\telse\n",
                "\t\twriteToTest \"false\""
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
        }

        [TestMethod]
        public void CompareEnumVariablesGreaterThanFalse()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables e, s : colors\n",
                "\te := red\n",
                "\ts := blue\n",
                "\tif e > s then\n",
                "\t\twriteToTest \"true\"\n",
                "\telse\n",
                "\t\twriteToTest \"false\""
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
            runInterpreter();
            Console.Write(Interpreter.testOutput.ToString());
            Assert.AreEqual("false ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void CompareEnumVariablesGreaterThanTrue()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables e, s : colors\n",
                "\te := red\n",
                "\ts := blue\n",
                "\tif s > e then\n",
                "\t\twriteToTest \"true\"\n",
                "\telse\n",
                "\t\twriteToTest \"false\""
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
            runInterpreter();
            Console.Write(Interpreter.testOutput.ToString());
            Assert.AreEqual("true ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void CompareEnumVariablesGreaterOrEqualFalse()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables e, s : colors\n",
                "\te := red\n",
                "\ts := blue\n",
                "\tif e >= s then\n",
                "\t\twriteToTest \"true\"\n",
                "\telse\n",
                "\t\twriteToTest \"false\""
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
            runInterpreter();
            Console.Write(Interpreter.testOutput.ToString());
            Assert.AreEqual("false ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void CompareEnumVariablesGreaterOrEqualDifferentValuesTrue()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables e, s : colors\n",
                "\te := red\n",
                "\ts := blue\n",
                "\tif s >= e then\n",
                "\t\twriteToTest \"true\"\n",
                "\telse\n",
                "\t\twriteToTest \"false\""
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
            runInterpreter();
            Console.Write(Interpreter.testOutput.ToString());
            Assert.AreEqual("true ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void CompareEnumVariablesGreaterOrEqualSameValuesTrue()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables e, s : colors\n",
                "\te := red\n",
                "\ts := red\n",
                "\tif s >= e then\n",
                "\t\twriteToTest \"true\"\n",
                "\telse\n",
                "\t\twriteToTest \"false\""
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
            runInterpreter();
            Console.Write(Interpreter.testOutput.ToString());
            Assert.AreEqual("true ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void CompareEnumValueWithVariableLessOrEqual()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables e, s : colors\n",
                "\te := green\n",
                "\tif red <= e then\n",
                "\t\twriteToTest \"true\"\n",
                "\telse\n",
                "\t\twriteToTest \"false\""
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
            runInterpreter();
            Console.Write(Interpreter.testOutput.ToString());
            Assert.AreEqual("false ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void CompareEnumVariableWithValueGreaterOrEqual()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables e : colors\n",
                "\te := red\n",
                "\tif e >= green then\n",
                "\t\twriteToTest \"true\"\n",
                "\telse\n",
                "\t\twriteToTest \"false\""
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            SemanticAnalysis.CheckModules();
            runInterpreter();
            Console.Write(Interpreter.testOutput.ToString());
            Assert.AreEqual("false ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void constantEnum()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "constants p = red\n",
                "variables r : colors\n",
                "\tr := p\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            runInterpreter();
        }

        [TestMethod]
        public void EnumInRecord()
        {
            string[] file =
            {
                "record rec\n",
                "\te : colors\n",
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables r : rec\n",
                "\tr.e := red\n",
                "\twriteToTest r.e\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            runInterpreter();
            Assert.AreEqual("red ", Interpreter.testOutput.ToString());
        }
    }
}
