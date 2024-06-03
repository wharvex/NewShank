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
                "WORD(Haneen)\n" + " WORD(Qasem)\n" + " SEPERATOR\n",
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
                "NUMBER(2.22)\n" + " NUMBER(444)\n" + " NUMBER(888)\n" + " SEPERATOR\n";

            string actualTokensString = string.Join("\n", tokens) + "\n";
            Assert.AreEqual(expectedTokensString, actualTokensString);
        }

        [TestMethod]
        public void OneSymbols()
        {
            //"{
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
                + " SEPERATOR\n";

            Assert.AreEqual(expectedTokensString, actualTokensString);
        }

        [TestMethod]
        public void TwoCharacterHashmap()
        {
            string file = ">= ++ -- <= == != ^= %= *= /= += -=";
            Lexer lexer = new Lexer(file);
            LinkedList<Token> tokens = lexer.Lex();
            Assert.AreEqual(
                "GREATEREQUAL DOUBLEPLUS DOUBLEMINUS LESSEQUAL EQUALS NOTEQUAL EXPONENTEQUAL PERCENTEQUALS MULTIPLYEQUALS DIVIDEEQUALS PLUSEQUALS MINUSEQUALS",
                tokens.ToString()
            );
        }

        [TestMethod]
        public void KeyWord()
        {
            string file =
                "if print getline nextfile function interface class string implements accessor value"
                + "loop mutator console datetime construct boolean true false shared \t \n ";
            Lexer lexer = new Lexer(file);
            LinkedList<Token> tokens = lexer.Lex();
            Assert.AreEqual(
                "IF PRINT GETLINE NEXTFILE FUNCTION INTERFACE CLASS STRING IMPLEMENTS ACCESSOR VALUE LOOP MUTATOR CONSOLE DATETIME CONSTRUCT BOOLEAN TRUE FALSE SHARED SEPERATOR SEPERATOR",
                tokens.ToString()
            );
        }

        //fix this
        [TestMethod]
        public void Wrong()
        {
            string file = "#";
            Lexer lexer = new Lexer(file);
            LinkedList<Token> tokens = lexer.Lex();
            Assert.AreEqual("#", tokens.ToString());
        }
    }
}
