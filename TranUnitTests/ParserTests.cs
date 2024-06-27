using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shank.ASTNodes;
using Shank.Tran;
using Tran;

namespace TranUnitTests
{
    [TestClass]
    public class ParserTests
    {
        private Parser parser;
        private Lexer lexer;
        private TokenHandler handler;
        private LinkedList<Token> tokens;

        [TestInitialize]
        public void Setup()
        {
            tokens = new LinkedList<Token>();
            handler = new TokenHandler(tokens);
        }

        private void CreateParser(string program)
        {
            lexer = new Lexer(program);
            tokens = lexer.Lex();
            parser = new Parser(tokens);
        }

        [TestMethod]
        public void ParseExpressionTest()
        {
            CreateParser("1 + 2");
            Assert.AreEqual("1 Plus 2", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseClassTest()
        {
            CreateParser("class Tran");
            parser.ParseClass();
            Assert.AreEqual("Tran", parser.thisClass.Name);
        }

        [TestMethod]
        public void ParseInterfaceTest()
        {
            CreateParser("interface Tran");
            parser.ParseInterface();
            Assert.AreEqual("Tran", parser.thisClass.Name);
        }

        [TestMethod]
        public void ParseFieldTest()
        {
            CreateParser("number x");
            parser.ParseField();
            Assert.AreEqual("x", parser.members.First().Name);
            Assert.AreEqual("real", parser.members.First().Type.ToString());
        }

        [TestMethod]
        public void ParseFunctionTest()
        {
            CreateParser(
                @"
class Tran
    helloWorld()
        x = 1 + 1".Replace("    ", "\t")
            );
            parser.Parse();
            Assert.AreEqual("helloWorld", parser.thisClass.Functions.First().Key);
            Assert.AreEqual(
                "x assigned as 1 Plus 1",
                ((FunctionNode)parser.thisClass.Functions.First().Value)
                    .Statements.First()
                    .ToString()
            );
        }

        [TestMethod]
        public void ParseReturnTest()
        {
            CreateParser(
                @"
class Tran
    helloWorld() : number retVal1, string retVal2
        x = 1 + 1".Replace("    ", "\t")
            );
            parser.Parse();
            Assert.AreEqual("real", parser.thisClass.Functions.First().Value.ParameterVariables[0].Type.ToString());
            Assert.AreEqual("string", parser.thisClass.Functions.First().Value.ParameterVariables[1].Type.ToString());
            Assert.AreEqual("retVal1", parser.thisClass.Functions.First().Value.ParameterVariables[0].Name);
            Assert.AreEqual("retVal2", parser.thisClass.Functions.First().Value.ParameterVariables[1].Name);
        }

        [TestMethod]
        public void ParseMembersTest()
        {
            CreateParser(
                @"
class Tran
    number w
    string x
    boolean y
    character z".Replace("    ", "\t")
            );
            parser.Parse();
            Assert.AreEqual("w", parser.thisClass.Records.First().Value.Members[0].Name);
            Assert.AreEqual("x", parser.thisClass.Records.First().Value.Members[1].Name);
            Assert.AreEqual("y", parser.thisClass.Records.First().Value.Members[2].Name);
            Assert.AreEqual("z", parser.thisClass.Records.First().Value.Members[3].Name);
        }

        [TestMethod]
        public void ParseParametersTest()
        {
            CreateParser(
                @"
class Tran
    doStuff(number param1, boolean param2)
        x = 1+1".Replace("    ", "\t")
            );
            parser.Parse();
            Assert.AreEqual("param1", ((FunctionNode)parser.thisClass.Functions.First().Value).LocalVariables[0].Name);
            Assert.AreEqual("real", ((FunctionNode)parser.thisClass.Functions.First().Value).LocalVariables[0].Type.ToString());
            Assert.AreEqual("param2", ((FunctionNode)parser.thisClass.Functions.First().Value).LocalVariables[1].Name);
            Assert.AreEqual("boolean", ((FunctionNode)parser.thisClass.Functions.First().Value).LocalVariables[1].Type.ToString());
        }

        [TestMethod]
        //ask about this test
        public void ParseLoopTest()
        {
            var testString = "temp = loop x.times() console.print (temp)";
            Lexer newLexer = new Lexer(testString);
            LinkedList<Token> tokens = newLexer.Lex();
            Parser newParser = new Parser(tokens);
            var expression = newParser.ParseLoop();
            Console.Write(expression);
            //  Assert.AreEqual("temp = loop x.times() console.print (temp)", expression.ToString());
        }

        [TestMethod]
        public void ParseIfTest()
        {
            CreateParser(
                @"if n > 100
    keepGoing = false".Replace("    ", "\t")
            );
            var expression = parser.ParseIf();
            var expected =
                "if, line 0, n gt 100, begin\r\n"
                + "if, line 0, n gt 100, statements begin\r\n"
                + "if, line 0, n gt 100, statements end\r\n"
                + "if, line 0, n gt 100, next begin\r\n"
                + "if, line 0, n gt 100, next end\r\n"
                + "if, line 0, n gt 100, end";

            Assert.AreEqual(expected, expression.ToString());
        }

        [TestMethod]
        public void ParseBuiltInFunctionNodeTestTimes()
        {
            var testString = ".times()";
            Lexer newLexer = new Lexer(testString);
            LinkedList<Token> tokens = newLexer.Lex();
            Parser newParser = new Parser(tokens);
            var expression = newParser.ParseBuiltInFunctionNode();
            Console.Write(expression);
        }

        [TestMethod]
        public void ParseBuiltInFunctionNodeTestPrint()
        {
            var testString = "console.print (temp)";
            Lexer newLexer = new Lexer(testString);
            LinkedList<Token> tokens = newLexer.Lex();
            Parser newParser = new Parser(tokens);
            var expression = newParser.ParseBuiltInFunctionNode();
            Console.Write(expression);
        }

        [TestMethod]
        public void ParseBuiltInFunctionNodeTestgetDate()
        {
            var testString = "clock.getDate()";
            Lexer newLexer = new Lexer(testString);
            LinkedList<Token> tokens = newLexer.Lex();
            Parser newParser = new Parser(tokens);
            var expression = newParser.ParseBuiltInFunctionNode();
            Console.Write(expression);
        }
    }
}
