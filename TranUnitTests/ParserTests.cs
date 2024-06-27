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
        public void ParseExpressionTestPlus()
        {
            CreateParser("1 + 2");
            Assert.AreEqual("1 Plus 2", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestTimes()
        {
            CreateParser("x * y");
            Assert.AreEqual("x Times y", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestGreaterEq()
        {
            CreateParser("300 >= 46.56");
            Assert.AreEqual("300 ge 46.56", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestLessThan()
        {
            CreateParser("46.56 < 75");
            Assert.AreEqual("46.56 lt 75", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestNotEqual()
        {
            CreateParser("1!=2");
            Assert.AreEqual("1 ne 2", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestEquals()
        {
            CreateParser("8==8");
            Assert.AreEqual("8 eq 8", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestMinus()
        {
            CreateParser("3 - 2");
            Assert.AreEqual("3 Minus 2", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestDivide()
        {
            CreateParser("x/y");
            Assert.AreEqual("x Divide y", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestLessEquals()
        {
            CreateParser("557 <= 4656");
            Assert.AreEqual("557 le 4656", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestModulo()
        {
            CreateParser("4656%40");
            Assert.AreEqual("4656 Modulo 40", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestTrue()
        {
            CreateParser("true");
            Assert.AreEqual("True", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestFalse()
        {
            CreateParser("false");
            Assert.AreEqual("False", parser.ParseExpression().ToString());
        }

        [TestMethod]
        public void ParseExpressionTestGreaterThan()
        {
            CreateParser("46.56>2.29");
            Assert.AreEqual("46.56 gt 2.29", parser.ParseExpression().ToString());
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

        [TestMethod]
        public void GeneralParserTest()
        {
            CreateParser(
                @"
class Tran
    loopsAndIfs()
        if n > 100
            keepGoing = false
            x=5%100
            temp = loop x.times()
                console.print (temp)
        console.print(n)".Replace("    ", "\t")
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
        public void FibonacciProgramTest()
        {
            CreateParser(
                @"
class FibonacciProgram
    FibonacciMath()
        number x = 0
        number y = 1
        number z = 0
        number totalCount = 8
        console.print(x + "" "" + y)
        loop totalCount.times()
            z = x+y
            console.print("" "" + z)
             x = y
             y = z".Replace("    ", "\t")
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
    }
}
