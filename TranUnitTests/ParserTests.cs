using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shank.Tran;
using Tran;

namespace TranUnitTests
{
    [TestClass]
    public class ParserTests
    {
        private Parser parser;
        private TokenHandler handler;
        private LinkedList<Token> tokens;

        [TestInitialize]
        public void Setup()
        {
            tokens = new LinkedList<Token>();
            handler = new TokenHandler(tokens);
        }

        [TestMethod]
        public void TestParseFunction()
        {
            var testString = "1+2";
            Lexer newLexer = new Lexer(testString);
            LinkedList<Token> tokens = newLexer.Lex();
            Parser newParser = new Parser(tokens);
            var expression = newParser.ParseExpression();
            //Console.Write(expression);
            Assert.AreEqual("1 Plus 2", expression.ToString());
        }
    }
}
