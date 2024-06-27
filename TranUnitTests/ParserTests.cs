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
        public void ParseLoopTest()
        {
            CreateParser("temp = loop x.times() \r\n"+
                            "\tconsole.print (temp)");
            var expression = parser.ParseLoop();
            Console.Write(expression);
        }

        [TestMethod]
        public void ParseIfTest()
        {
            CreateParser(
                "if n > 100\r\n"+
                 "\tkeepGoing = false");
            var expression = parser.ParseIf();
            var expected = "if, line 0, n gt 100, begin\r\n"
                           + "if, line 0, n gt 100, statements begin\r\n"
                           +"keepGoing assigned as False\r\n"
                           + "if, line 0, n gt 100, statements end\r\n"
                           + "if, line 0, n gt 100, next begin\r\n"
                           + "if, line 0, n gt 100, next end\r\n"
                           + "if, line 0, n gt 100, end";
           // Console.Write(expression);
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
