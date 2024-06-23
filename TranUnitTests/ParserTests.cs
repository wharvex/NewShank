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
    }
}
