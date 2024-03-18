using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace ShankUnitTests
{
    [TestClass]
    public class UnitTestTests
    {
        public static string[]? testFile;

        public ModuleNode getModuleFromLexer(string[] file)
        {
            return ModuleParserTests.getModuleFromLexer(file);
        }

        public void initializeInterpreter(LinkedList<string[]> files)
        {
            ModuleBeforeInterpreterTests.initializeInterpreter(files);
        }

        [TestMethod]
        public void simpleUnitTest()
        {
            string[] file1 =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp := 3\n",
                "\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "\n",
                "test simpleTest for add(a, b : integer; var c : integer)\n",
                "variables j : integer\n",
                "\tadd 1, 2, var j\n",
                "\tassertIsEqual 3, j\n"
            };
            ModuleNode m = getModuleFromLexer(file1);
            Assert.AreEqual(m.getTests().Count, 1);
            Assert.IsTrue(m.getTests().ContainsKey("simpleTest"));
            Assert.IsTrue(m.getTests()["simpleTest"].targetFunctionName == "add");
            Assert.IsTrue(m.getTests()["simpleTest"].testingFunctionParameters.Count == 3);
        }

        [TestMethod]
        public void simpleHandleTests()
        {
            string[] file1 =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp := 3\n",
                "\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "\n",
                "test simpleTest for add(a, b : integer; var c : integer)\n",
                "variables j : integer\n",
                "\tadd 1, 2, var j\n",
                "\tassertIsEqual 3, j\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            initializeInterpreter(files);
            Interpreter.handleTests();
            Assert.AreEqual(
                Interpreter.getModules()["default"].getFunctions().Count,
                2 + BuiltInFunctions.numberOfBuiltInFunctions
            );
            Assert.IsTrue(
                (
                    (FunctionNode)Interpreter.getModules()["default"].getFunctions()["add"]
                ).Tests.ContainsKey("simpleTest")
            );
            Assert.AreEqual(
                ((FunctionNode)Interpreter.getModules()["default"].getFunctions()["add"])
                    .Tests["simpleTest"]
                    .testingFunctionParameters
                    .Count,
                3
            );
        }

        [TestMethod]
        public void handleTestsOutOfOrder()
        {
            string[] file1 =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp := 3\n",
                "\n",
                "test simpleTest for add(a, b : integer; var c : integer)\n",
                "variables j : integer\n",
                "\tadd 1, 2, var j\n",
                "\tassertIsEqual 3, j\n",
                "\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            initializeInterpreter(files);
            Interpreter.handleTests();
            Assert.AreEqual(
                Interpreter.getModules()["default"].getFunctions().Count,
                2 + BuiltInFunctions.numberOfBuiltInFunctions
            );
            Assert.IsTrue(
                (
                    (FunctionNode)Interpreter.getModules()["default"].getFunctions()["add"]
                ).Tests.ContainsKey("simpleTest")
            );
            Assert.AreEqual(
                ((FunctionNode)Interpreter.getModules()["default"].getFunctions()["add"])
                    .Tests["simpleTest"]
                    .testingFunctionParameters
                    .Count,
                3
            );
        }

        [TestMethod]
        public void multipleHandleTest()
        {
            string[] file1 =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp := 3\n",
                "\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "define sub(a, b : integer; var c : integer)\n",
                "\tc := a - b\n",
                "test simpleTest for add(a, b : integer; var c : integer)\n",
                "variables j : integer\n",
                "\tadd 1, 2, var j\n",
                "\tassertIsEqual 3, j\n",
                "test subTest for sub(a, b : integer; var c : integer)\n",
                "variables f : integer\n",
                "\tsub 7, 4, var f\n",
                "\tassertIsEqual 3, f\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            initializeInterpreter(files);
            Interpreter.handleTests();
            Assert.AreEqual(
                Interpreter.getModules()["default"].getFunctions().Count,
                3 + BuiltInFunctions.numberOfBuiltInFunctions
            );
            Assert.AreEqual(
                ((FunctionNode)Interpreter.getModules()["default"].getFunctions()["add"])
                    .Tests
                    .Count,
                1
            );
            Assert.AreEqual(
                ((FunctionNode)Interpreter.getModules()["default"].getFunctions()["add"])
                    .Tests
                    .Count,
                1
            );

            Assert.IsTrue(
                (
                    (FunctionNode)Interpreter.getModules()["default"].getFunctions()["add"]
                ).Tests.ContainsKey("simpleTest")
            );
            Assert.AreEqual(
                ((FunctionNode)Interpreter.getModules()["default"].getFunctions()["add"])
                    .Tests["simpleTest"]
                    .testingFunctionParameters
                    .Count,
                3
            );

            Assert.IsTrue(
                (
                    (FunctionNode)Interpreter.getModules()["default"].getFunctions()["sub"]
                ).Tests.ContainsKey("subTest")
            );
            Assert.AreEqual(
                ((FunctionNode)Interpreter.getModules()["default"].getFunctions()["add"])
                    .Tests["simpleTest"]
                    .testingFunctionParameters
                    .Count,
                3
            );
        }

        [TestMethod]
        public void simpleInterpreterMode()
        {
            string[] args = { "", "-ut" };
            string[] file =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp := 3\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "define sub(a, b : integer; var c : integer)\n",
                "\tc := a - b\n",
                "test simpleTest for add(a, b : integer; var c : integer)\n",
                "variables j : integer\n",
                "\tadd 1, 2, var j\n",
                "\tassertIsEqual 3, j\n",
                "test subTest for sub(a, b : integer; var c : integer)\n",
                "variables f : integer\n",
                "\tsub 7, 4, var f\n",
                "\tassertIsEqual 3, f\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            Program.Main(args);
            Assert.AreEqual(2, Program.unitTestResults.Count);
            Assert.AreEqual(
                Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).parentTestName,
                "simpleTest"
            );
            Assert.AreEqual(true, Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).passed);

            Assert.AreEqual(
                Program.unitTestResults.ElementAt(1).Asserts.ElementAt(0).parentTestName,
                "subTest"
            );
            Assert.AreEqual(true, Program.unitTestResults.ElementAt(1).Asserts.ElementAt(0).passed);
        }

        [TestMethod]
        public void testWithTwoAsserts()
        {
            string[] args = { "", "-ut" };
            string[] file =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp := 3\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "test simpleTest for add(a, b : integer; var c : integer)\n",
                "variables j : integer\n",
                "\tadd 1, 2, var j\n",
                "\tassertIsEqual 3, j\n",
                "test simpleTest2 for add(a, b : integer; var c : integer)\n",
                "variables f : integer\n",
                "\tadd -7, 4, var f\n",
                "\tassertIsEqual -3, f\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            Program.Main(args);

            Assert.AreEqual(1, Program.unitTestResults.Count);
            Assert.AreEqual(
                Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).parentTestName,
                "simpleTest"
            );
            Assert.AreEqual(true, Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).passed);

            Assert.AreEqual(
                Program.unitTestResults.ElementAt(0).Asserts.ElementAt(1).parentTestName,
                "simpleTest2"
            );
            Assert.AreEqual(true, Program.unitTestResults.ElementAt(0).Asserts.ElementAt(1).passed);
        }

        [TestMethod]
        public void builtInAssertIsEqualWithBool()
        {
            string[] args = { "", "-ut" };
            string[] file =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp := 3\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "test simpleTest for add(a, b : integer; var c : integer)\n",
                "variables j : integer\n",
                "variables b : boolean\n",
                "\tadd 1, 2, var j\n",
                "\tassertIsEqual 3, j\n",
                "\tb := 3 = j\n",
                "\tassertIsEqual true, b\n",
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            Program.Main(args);

            Assert.AreEqual(1, Program.unitTestResults.Count);
            Assert.AreEqual(
                Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).parentTestName,
                "simpleTest"
            );
            Assert.AreEqual(true, Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).passed);
        }

        [TestMethod]
        public void multipleTestsAndMultipleFunctions()
        {
            string[] args = { "", "-ut" };
            string[] file =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp := 3\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "define sub(a, b : integer; var c : integer)\n",
                "\tc := a - b\n",
                "test addTest for add(a, b : integer; var c : integer)\n",
                "variables j : integer\n",
                "variables b : boolean\n",
                "\tadd 1, 2, var j\n",
                "\tassertIsEqual 3, j\n",
                "\tb := 3 = j\n",
                "\tassertIsEqual true, b\n",
                "test addTest2 for add(a, b : integer; var c : integer)\n",
                "variables f : integer\n",
                "\tadd -7, 4, var f\n",
                "\tassertIsEqual -3, f\n",
                "test subTest for sub(a, b : integer; var c : integer)\n",
                "variables f : integer\n",
                "\tsub 7, 4, var f\n",
                "\tassertIsEqual 3, f\n",
                "test subTest2 for sub(a, b : integer; var c : integer)\n",
                "variables f : integer\n",
                "\tsub -7, 4, var f\n",
                "\tassertIsEqual -11, f\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            Program.Main(args);
            Assert.AreEqual(2, Program.unitTestResults.Count);
            Assert.AreEqual(3, Program.unitTestResults.ElementAt(0).Asserts.Count);
            Assert.AreEqual(2, Program.unitTestResults.ElementAt(1).Asserts.Count);

            Assert.AreEqual(
                "addTest",
                Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).parentTestName
            );
            Assert.AreEqual(true, Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).passed);

            Assert.AreEqual(
                "addTest",
                Program.unitTestResults.ElementAt(0).Asserts.ElementAt(1).parentTestName
            );
            Assert.AreEqual(true, Program.unitTestResults.ElementAt(0).Asserts.ElementAt(1).passed);

            Assert.AreEqual(
                "addTest2",
                Program.unitTestResults.ElementAt(0).Asserts.ElementAt(2).parentTestName
            );
            Assert.AreEqual(true, Program.unitTestResults.ElementAt(0).Asserts.ElementAt(2).passed);

            Assert.AreEqual(
                "subTest",
                Program.unitTestResults.ElementAt(1).Asserts.ElementAt(0).parentTestName
            );
            Assert.AreEqual(true, Program.unitTestResults.ElementAt(1).Asserts.ElementAt(0).passed);

            Assert.AreEqual(
                "subTest2",
                Program.unitTestResults.ElementAt(1).Asserts.ElementAt(1).parentTestName
            );
            Assert.AreEqual(true, Program.unitTestResults.ElementAt(1).Asserts.ElementAt(1).passed);
        }

        [TestMethod]
        public void unitTestsInTwoDifferentModules()
        {
            string[] args = { "", "-ut" };
            string[] file1 =
            {
                "module test1\n",
                "define start()\n",
                "variables p : integer\n",
                "\tp := 3\n",
                "\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "\n",
                "test addTest for add(a, b : integer; var c : integer)\n",
                "variables j : integer\n",
                "\tadd 1, 2, var j\n",
                "\tassertIsEqual 3, j\n"
            };
            string[] file2 =
            {
                "module test2\n",
                "define sub(a, b : integer; var c : integer)\n",
                "\tc := a - b\n",
                "test subTest for sub(a, b : integer; var c : integer)\n",
                "variables f : integer\n",
                "\tsub 7, 4, var f\n",
                "\tassertIsEqual 3, f\n",
            };

            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file1);
            files.AddLast(file2);
            initializeInterpreter(files);
            Program.Main(args);

            Assert.AreEqual(2, Program.unitTestResults.Count);
            Assert.AreEqual(
                Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).parentTestName,
                "addTest"
            );
            Assert.IsTrue(Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).passed);

            Assert.AreEqual(
                Program.unitTestResults.ElementAt(1).Asserts.ElementAt(0).parentTestName,
                "subTest"
            );
            Assert.IsTrue(Program.unitTestResults.ElementAt(1).Asserts.ElementAt(0).passed);
        }

        [TestMethod]
        public void builtInAssertWithReal()
        {
            string[] args = { "", "-ut" };
            string[] file =
            {
                "define start()\n",
                "variables p : real\n",
                "\tp := 3.2\n",
                "define add(a, b : real; var c : real)\n",
                "\tc := a + b\n",
                "test simpleTest for add(a, b : real; var c : real)\n",
                "variables f : real\n",
                "\tadd 2.25, 2.25, var f\n",
                "\tassertIsEqual 4.5, f\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            Program.Main(args);

            Assert.AreEqual(1, Program.unitTestResults.Count);
            Assert.AreEqual(1, Program.unitTestResults.ElementAt(0).Asserts.Count);

            Assert.AreEqual(
                Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).parentTestName,
                "simpleTest"
            );
            Assert.IsTrue(Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).passed);
        }

        [TestMethod]
        public void builtInAssertIsEqualWithString()
        {
            string[] args = { "", "-ut" };
            string[] file =
            {
                "define start()\n",
                "variables p : real\n",
                "\tp := 3.2\n",
                "define doSomething(var s : string)\n",
                "\ts := \"hello\"\n",
                "test simpleTest for doSomething(var s : string)\n",
                "variables f : string\n",
                "\tdoSomething var f\n",
                "\tassertIsEqual \"hello\", f\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            Program.Main(args);

            Assert.AreEqual(1, Program.unitTestResults.Count);
            Assert.AreEqual(1, Program.unitTestResults.ElementAt(0).Asserts.Count);

            Assert.AreEqual(
                Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).parentTestName,
                "simpleTest"
            );
            Assert.IsTrue(Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).passed);
        }

        [TestMethod]
        public void builtInAssertIsEqualWithChar()
        {
            string[] args = { "", "-ut" };
            string[] file =
            {
                "define start()\n",
                "variables p : real\n",
                "\tp := 3.2\n",
                "define doSomething(var s : character)\n",
                "\ts := 'c'\n",
                "test simpleTest for doSomething(var s : character)\n",
                "variables f : character\n",
                "\tdoSomething var f\n",
                "\tassertIsEqual 'c', f\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            Program.Main(args);

            Assert.AreEqual(1, Program.unitTestResults.Count);
            Assert.AreEqual(1, Program.unitTestResults.ElementAt(0).Asserts.Count);

            Assert.AreEqual(
                Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).parentTestName,
                "simpleTest"
            );
            Assert.IsTrue(Program.unitTestResults.ElementAt(0).Asserts.ElementAt(0).passed);
        }
    }
}
