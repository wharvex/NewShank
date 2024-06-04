using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shank.Tran;
using Tran;

namespace TranUnitTests
{
    [TestClass]
    public class LexerTests
    {
        [TestMethod]
        public void Words()
        {
            string file = "Haneen Qasem";
            Lexer lexer = new Lexer(file);
            LinkedList<Token> tokens = lexer.Lex();

            // Collect the values of the tokens to create the expected string
            string actualTokensString = string.Join("\n", tokens) + "\n";
            Assert.AreEqual(
                "WORD(Haneen)\n" + " WORD(Qasem)\n" + " SEPARATOR\n",
                actualTokensString
            );
        }

        [TestMethod]
        public void Digits()
        {
            string file = "2.22 444 888";
            Lexer lexer = new Lexer(file);
            LinkedList<Token> tokens = lexer.Lex();

            string expectedTokensString =
                "NUMBER(2.22)\n" + " NUMBER(444)\n" + " NUMBER(888)\n" + " SEPARATOR\n";
            string actualTokensString = string.Join("\n", tokens) + "\n";

            Assert.AreEqual(expectedTokensString, actualTokensString);
        }

        [TestMethod]
        public void Comments()
        {
            string file = "{ Exercise??, I thought you said extra fries` }";
            Lexer lexer = new Lexer(file);
            LinkedList<Token> tokens = lexer.Lex();
            string actualTokensString = string.Join("\n", tokens) + "\n";
            Assert.AreEqual(" SEPARATOR\n", actualTokensString);
        }

        [TestMethod]
        public void OneSymbols()
        {
            string file = "} ( ) = > < + ^ + - : * / % , ! \"";
            Lexer lexer = new Lexer(file);
            LinkedList<Token> tokens = lexer.Lex();
            string actualTokensString = string.Join("\n", tokens) + "\n";
            string expectedTokensString =
                " CLOSEDANGLEBRACKET\n"
                + " OPENPARENTHESIS\n"
                + " CLOSEDPARENTHESIS\n"
                + " EQUALS\n"
                + " GREATERTHAN\n"
                + " LESSTHAN\n"
                + " PLUS\n"
                + " EXPONENT\n"
                + " PLUS\n"
                + " MINUS\n"
                + " COLON\n"
                + " MULTIPLY\n"
                + " DIVIDE\n"
                + " MODULUS\n"
                + " COMMA\n"
                + " NOT\n"
                + " QUOTE\n"
                + " SEPARATOR\n";

            Assert.AreEqual(expectedTokensString, actualTokensString);
        }

        [TestMethod]
        public void TwoCharacterHashmap()
        {
            string file = ">= ++ -- <= == != ^= %= *= /= += -= && || ";
            Lexer lexer = new Lexer(file);
            LinkedList<Token> tokens = lexer.Lex();
            string actualTokensString = string.Join("\n", tokens) + "\n";
            string expectedTokensString =
                " GREATEREQUAL\n"
                + " DOUBLEPLUS\n"
                + " DOUBLEMINUS\n"
                + " LESSEQUAL\n"
                + " EQUALS\n"
                + " NOTEQUAL\n"
                + " EXPONENTEQUAL\n"
                + " PERCENTEQUALS\n"
                + " MULTIPLYEQUALS\n"
                + " DIVIDEEQUALS\n"
                + " PLUSEQUALS\n"
                + " MINUSEQUALS\n"
                + " AND\n"
                + " OR\n"
                + " SEPARATOR\n";
            Assert.AreEqual(expectedTokensString, actualTokensString);
        }

        [TestMethod]
        public void KeyWord()
        {
            string file =
                "if print getline nextfile function interface class string implements accessor value "
                + "loop mutator console datetime construct boolean true false shared \t \n ";
            Lexer lexer = new Lexer(file);
            LinkedList<Token> tokens = lexer.Lex();
            string actualTokensString = string.Join("\n", tokens) + "\n";

            string expectedTokensString =
                " IF\n"
                + " PRINT\n"
                + " GETLINE\n"
                + " NEXTFILE\n"
                + " FUNCTION\n"
                + " INTERFACE\n"
                + " CLASS\n"
                + " STRING\n"
                + " IMPLEMENTS\n"
                + " ACCESSOR\n"
                + " VALUE\n"
                + " LOOP\n"
                + " MUTATOR\n"
                + " CONSOLE\n"
                + " DATETIME\n"
                + " CONSTRUCT\n"
                + " BOOLEAN\n"
                + " TRUE\n"
                + " FALSE\n"
                + " SHARED\n"
                + " SEPARATOR\n"
                + " SEPARATOR\n"
                + " SEPARATOR\n";
            Assert.AreEqual(expectedTokensString, actualTokensString);
        }

        [TestMethod]
        public void Wrong()
        {
            string file = "#";
            Lexer lexer = new Lexer(file);

            Assert.ThrowsException<ArgumentException>(
                () => lexer.Lex(),
                "UNRECOGNIZED CHARACTER: #"
            );
        }
    }
}
