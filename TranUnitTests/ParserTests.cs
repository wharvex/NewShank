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
        private Parser parser = null!;
        private Lexer lexer = null!;
        private List<List<Token>> tokens = new List<List<Token>>();

        [TestInitialize]
        public void Setup()
        {
            tokens = new List<List<Token>>();
        }

        private void CreateParser(List<string> program)
        {
            lexer = new Lexer(program);
            tokens = lexer.Lex();
            parser = new Parser(tokens);
        }

        [TestMethod]
        public void ParseExpressionTestPlus()
        {
            List<string> programList = new List<string>();
            programList.Add("1 + 2");
            CreateParser(programList);
            Assert.AreEqual("1 Plus 2", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestTimes()
        {
            List<string> programList = new List<string>();
            programList.Add("x * y");
            CreateParser(programList);
            Assert.AreEqual("x Times y", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestGreaterEq()
        {
            List<string> programList = new List<string>();
            programList.Add("300 >= 46.56");
            CreateParser(programList);
            Assert.AreEqual("300 ge 46.56", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestLessThan()
        {
            List<string> programList = new List<string>();
            programList.Add("46.56 < 75");
            CreateParser(programList);
            Assert.AreEqual("46.56 lt 75", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestNotEqual()
        {
            List<string> programList = new List<string>();
            programList.Add("1!=2");
            CreateParser(programList);
            Assert.AreEqual("1 ne 2", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestEquals()
        {
            List<string> programList = new List<string>();
            programList.Add("8==8");
            CreateParser(programList);
            Assert.AreEqual("8 eq 8", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestMinus()
        {
            List<string> programList = new List<string>();
            programList.Add("3 - 2");
            CreateParser(programList);
            Assert.AreEqual("3 Minus 2", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestDivide()
        {
            List<string> programList = new List<string>();
            programList.Add("x/y");
            CreateParser(programList);
            Assert.AreEqual("x Divide y", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestLessEquals()
        {
            List<string> programList = new List<string>();
            programList.Add("557 <= 4656");
            CreateParser(programList);
            Assert.AreEqual("557 le 4656", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestModulo()
        {
            List<string> programList = new List<string>();
            programList.Add("4656%40");
            CreateParser(programList);
            Assert.AreEqual("4656 Modulo 40", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestTrue()
        {
            List<string> programList = new List<string>();
            programList.Add("true");
            CreateParser(programList);
            Assert.AreEqual("True", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestFalse()
        {
            List<string> programList = new List<string>();
            programList.Add("false");
            CreateParser(programList);
            Assert.AreEqual("False", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseExpressionTestGreaterThan()
        {
            List<string> programList = new List<string>();
            programList.Add("46.56>2.29");
            CreateParser(programList);
            Assert.AreEqual("46.56 gt 2.29", parser.ParseExpression()!.ToString());
        }

        [TestMethod]
        public void ParseClassTest()
        {
            List<string> programList = new List<string>();
            programList.Add("class Tran");
            CreateParser(programList);
            parser.ParseClass();
            Assert.AreEqual("Tran", parser.thisClass.Name);
        }

        [TestMethod]
        public void ParseInterfaceTest()
        {
            // CreateParser("interface Tran");
            List<string> programList = new List<string>();
            programList.Add("interface someName\r\n\tupdateClock()\r\n\tsquare() : number s");
            CreateParser(programList);
            parser.ParseInterface();
            Assert.AreEqual("someName", parser.thisClass.Name);
        }

        [TestMethod]
        public void ParseFieldTest()
        {
            List<string> programList = new List<string>();
            programList.Add("number x");
            CreateParser(programList);
            parser.ParseField();
            Assert.AreEqual("x", parser.members.First().Name);
            Assert.AreEqual("real", parser.members.First().Type.ToString());
        }

        [TestMethod]
        public void ParseFunctionTest()
        {
            List<string> programList = new List<string>();
            programList.Add(
                @"
class Tran
    helloWorld()
        x = 1 + 1".Replace("    ", "\t")
            );
            CreateParser(programList);
            /*
            CreateParser(
                @"
class Tran
    helloWorld()
        x = 1 + 1".Replace("    ", "\t")
            );
            */
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
            List<string> programList = new List<string>();
            programList.Add(
                @"
class Tran
    helloWorld() : number retVal1, string retVal2
        x = 1 + 1".Replace("    ", "\t")
            );
            CreateParser(programList);
            /*
            CreateParser(
                @"
class Tran
    helloWorld() : number retVal1, string retVal2
        x = 1 + 1".Replace("    ", "\t")
            );
            */
            parser.Parse();
            Assert.AreEqual(
                "real",
                parser.thisClass.Functions.First().Value.ParameterVariables[0].Type.ToString()
            );
            Assert.AreEqual(
                "string",
                parser.thisClass.Functions.First().Value.ParameterVariables[1].Type.ToString()
            );
            Assert.AreEqual(
                "retVal1",
                parser.thisClass.Functions.First().Value.ParameterVariables[0].Name
            );
            Assert.AreEqual(
                "retVal2",
                parser.thisClass.Functions.First().Value.ParameterVariables[1].Name
            );
        }

        [TestMethod]
        public void ParseMembersTest()
        {
            List<string> programList = new List<string>();
            programList.Add(
                @"
class Tran
    number w
    string x
    boolean y
    character z".Replace("    ", "\t")
            );
            CreateParser(programList);
            /*
            CreateParser(
                @"
class Tran
    number w
    string x
    boolean y
    character z".Replace("    ", "\t")
            );
            */
            parser.Parse();
            Assert.AreEqual("w", parser.thisClass.Records.First().Value.Members[0].Name);
            Assert.AreEqual("x", parser.thisClass.Records.First().Value.Members[1].Name);
            Assert.AreEqual("y", parser.thisClass.Records.First().Value.Members[2].Name);
            Assert.AreEqual("z", parser.thisClass.Records.First().Value.Members[3].Name);
        }

        [TestMethod]
        public void ParseParametersTest()
        {
            List<string> programList = new List<string>();
            programList.Add(
                @"
class Tran
    doStuff(number param1, boolean param2)
        x = 1+1".Replace("    ", "\t")
            );
            CreateParser(programList);
            /*
            CreateParser(
                @"
class Tran
    doStuff(number param1, boolean param2)
        x = 1+1".Replace("    ", "\t")
            );
            */
            parser.Parse();

            Console.WriteLine(parser.thisClass.Functions.First().Value.ParameterVariables[1].Type);

            Assert.AreEqual("doStuff", parser.thisClass.Functions.First().Value.Name);

            Assert.AreEqual(
                "param1",
                parser.thisClass.Functions.First().Value.ParameterVariables[0].Name
            );

            Assert.AreEqual(
                "real",
                parser.thisClass.Functions.First().Value.ParameterVariables[0].Type.ToString()
            );

            Assert.AreEqual(
                "param2",
                parser.thisClass.Functions.First().Value.ParameterVariables[1].Name
            );

            Assert.AreEqual(
                "boolean",
                parser.thisClass.Functions.First().Value.ParameterVariables[1].Type.ToString()
            );
        }

        [TestMethod]
        //ask about this test
        public void ParseLoopTest()
        {
            List<string> programList = new List<string>();
            programList.Add("temp = loop x.times() \r\n" + "\tconsole.print (temp)");
            CreateParser(programList);
            var expression = parser.ParseLoop();
            Console.Write(expression);
        }

        [TestMethod]
        public void ParseIfTest()
        {
            List<string> programList = new List<string>();
            programList.Add("if n > 100\r\n" + "\tkeepGoing = false");
            CreateParser(programList);
            var expression = parser.ParseIf();
            var expected =
                "if, line 0, n gt 100, begin\r\n"
                + "if, line 0, n gt 100, statements begin\r\n"
                + "keepGoing assigned as False\r\n"
                + "if, line 0, n gt 100, statements end\r\n"
                + "if, line 0, n gt 100, next begin\r\n"
                + "if, line 0, n gt 100, next end\r\n"
                + "if, line 0, n gt 100, end";
            // Console.Write(expression);
            Assert.AreEqual(expected, expression!.ToString());
        }

        [TestMethod]
        public void ParseBuiltInFunctionNodeTestTimes()
        {
            List<string> programList = new List<string>();
            programList.Add(".times()");
            Lexer newLexer = new Lexer(programList);
            List<List<Token>> tokens = newLexer.Lex();
            Parser newParser = new Parser(tokens);
            var expression = newParser.ParseBuiltInFunctionNode();
            Console.Write(expression);
        }

        [TestMethod]
        public void ParseBuiltInFunctionNodeTestGetDate()
        {
            List<string> programList = new List<string>();
            programList.Add("clock.getDate()");
            Lexer newLexer = new Lexer(programList);
            List<List<Token>> tokens = newLexer.Lex();
            Parser newParser = new Parser(tokens);
            var expression = newParser.ParseBuiltInFunctionNode();
            Console.Write(expression);
        }

        [TestMethod]
        public void ParseAccessorAndMutatorTest()
        {
            List<string> programList = new List<string>();
            programList.Add(
                @"
class Tran
    helloWorld()
        string y
            accessor: value = y
            mutator: y = value".Replace("    ", "\t")
            );
            CreateParser(programList);
            /*
            CreateParser(
                @"
class Tran
    helloWorld()
        string y
            accessor: value = y
            mutator: y = value".Replace("    ", "\t")
            );
            */
            parser.Parse();
            Assert.AreEqual("J", parser.thisClass.Functions.Skip(1).ToString());
        }

        [TestMethod]
        public void parsedumb()
        {
            List<string> files = new List<string>();
            files.Add(
                @"
interface someName
    square() : number s".Replace("    ", "\t")
            );
            files.Add(
                @"
class test implements someName
    start()
        number x 
        x = 10
        number y
        y = square(x)
        console.print(y)

    square(number x) : number s
        number a
        a = 1".Replace("    ", "\t")
            );
            CreateParser(files);
            parser.Parse();
            TRANsformer.InterfaceWalk(parser.thisClass);
        }
    }
}
