﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shank.ASTNodes;

namespace ShankUnitTests
{
    [TestClass]
    public class ReferenceTests
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
        public void SimpleReferenceParse()
        {
            string[] file =
            {
                "record rtest\n",
                "\ti : integer\n",
                "\ts : string\n",
                "define start()\n",
                "variables t : refersTo rtest\n",
                "\tallocateMemory var t\n"
            };
            ModuleNode m = getModuleFromParser(file);
        }

        [TestMethod]
        public void SimpleReferenceInterpret()
        {
            string[] file =
            {
                "record rtest\n",
                "\ti : integer\n",
                "\ts : string\n",
                "define start()\n",
                "variables t : refersTo rtest\n",
                "\tallocateMemory var t\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddFirst(file);
            initializeInterpreter(files);
            runInterpreter();
        }

        [TestMethod]
        public void ReferenceInterpretAssignMember()
        {
            string[] file =
            {
                "record rtest\n",
                "\ti : integer\n",
                "\ts : string\n",
                "define start()\n",
                "variables t : refersTo rtest\n",
                "\tallocateMemory var t\n",
                "\tt.i := 4\n",
                "\tt.s := \"hello\"",
                "\twriteToTest t.i\n",
                "\twriteToTest t.s\n",
                "\tfreeMemory var t\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddFirst(file);
            initializeInterpreter(files);
            runInterpreter();
            //change this when Tim implements sending record members to functions
            Assert.AreEqual("4 hello ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void builtInSizeFunctionSimple()
        {
            string[] file =
            {
                "record rtest\n",
                "\ti : integer\n",
                "\ts : string\n",
                "define start()\n",
                "variables t : refersTo rtest\n",
                "variables p : integer\n",
                "\tallocateMemory var t\n",
                "\tt.i := 4\n",
                "\tt.s := \"hello\"",
                "\tsize t, var p\n",
                "\twriteToTest p\n",
                "\tfreeMemory var t\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddFirst(file);
            initializeInterpreter(files);
            runInterpreter();
            Assert.AreEqual("14 ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void builtInIsSet()
        {
            string[] file =
            {
                "record rtest\n",
                "\ti : integer\n",
                "\ts : string\n",
                "define start()\n",
                "variables t : refersTo rtest\n",
                "variables p : boolean\n",
                "\tallocateMemory var t\n",
                "\tt.i := 4\n",
                "\tt.s := \"hello\"\n",
                "\tisSet t, var p\n",
                "\twriteToTest p\n",
                "\tfreeMemory var t\n",
                "\tisSet t, var p\n",
                "\twriteToTest p\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddFirst(file);
            initializeInterpreter(files);
            runInterpreter();
            Assert.AreEqual("True False ", Interpreter.testOutput.ToString());
        }

        [TestMethod]
        public void nestedRefersToInRecord()
        {
            string[] file =
            {
                "record node\n",
                "\tdata : integer\n",
                "\tnext : refersTo node\n",
                "define start()\n",
                "variables head, n : refersTo node\n",
                "\tallocateMemory var head\n",
                "\tallocateMemory var n\n",
                "\thead.data := 5\n",
                "\thead.next := n\n",
                "\thead.next.data := 3\n",
                "\twriteToTest head.data, head.next.data\n",
                "\tfreeMemory var n\n",
                "\tfreeMemory var head\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddFirst(file);
            initializeInterpreter(files);
            runInterpreter();
            Assert.AreEqual("5 3 ", Interpreter.testOutput.ToString());
        }
    }
}
