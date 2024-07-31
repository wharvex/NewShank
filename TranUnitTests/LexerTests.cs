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
            string file = "Test String";
            List<string> fileList = new List<string>();
            fileList.Add(file);
            Lexer lexer = new Lexer(fileList);
            List<List<Token>> tokens = lexer.Lex();

            // Collect the values of the tokens to create the expected string

            string actualTokensString = "";
            foreach (List<Token> projectFile in tokens)
            {
                actualTokensString += string.Join("\n", projectFile) + "\n";
            }

            Assert.AreEqual(
                "WORD(Test)\n" + " WORD(String)\n" + " SEPARATOR\n",
                actualTokensString
            );

            file = "word.reference";
            fileList = new List<string>();
            fileList.Add(file);
            lexer = new Lexer(fileList);
            tokens = lexer.Lex();

            actualTokensString = "";
            foreach (List<Token> projectFile in tokens)
            {
                actualTokensString += string.Join("\n", projectFile) + "\n";
            }

            Assert.AreEqual(
                "WORD(word)\n" + " PERIOD\n" + " WORD(reference)\n" + " SEPARATOR\n",
                actualTokensString
            );

            file = "Test String";
            string file2 = "word.reference";
            fileList = new List<string>();
            fileList.Add(file);
            fileList.Add(file2);
            lexer = new Lexer(fileList);
            tokens = lexer.Lex();

            actualTokensString = "";
            foreach (List<Token> projectFile in tokens)
            {
                actualTokensString += string.Join("\n", projectFile) + "\n";
            }

            Assert.AreEqual(
                "WORD(Test)\n"
                    + " WORD(String)\n"
                    + " SEPARATOR\n"
                    + "WORD(word)\n"
                    + " PERIOD\n"
                    + " WORD(reference)\n"
                    + " SEPARATOR\n",
                actualTokensString
            );
        }

        [TestMethod]
        public void Digits()
        {
            string file = "2.22 444 888";
            List<string> fileList = new List<string>();
            fileList.Add(file);
            Lexer lexer = new Lexer(fileList);
            List<List<Token>> tokens = lexer.Lex();

            string actualTokensString = "";
            foreach (List<Token> projectFile in tokens)
            {
                actualTokensString += string.Join("\n", projectFile) + "\n";
            }

            string expectedTokensString =
                "NUMERAL(2.22)\n" + " NUMERAL(444)\n" + " NUMERAL(888)\n" + " SEPARATOR\n";

            Assert.AreEqual(expectedTokensString, actualTokensString);
        }

        [TestMethod]
        public void Comments()
        {
            string file = "{ Exercise??, I thought you said extra fries` }";
            List<string> fileList = new List<string>();
            fileList.Add(file);
            Lexer lexer = new Lexer(fileList);
            List<List<Token>> tokens = lexer.Lex();

            string actualTokensString = "";
            foreach (List<Token> projectFile in tokens)
            {
                actualTokensString += string.Join("\n", projectFile) + "\n";
            }

            Assert.AreEqual(" SEPARATOR\n", actualTokensString);
        }

        [TestMethod]
        public void Functions()
        {
            string file = "tyler(";
            List<string> fileList = new List<string>();
            fileList.Add(file);
            Lexer lexer = new Lexer(fileList);
            List<List<Token>> tokens = lexer.Lex();
            string expectedTokensString =
                "FUNCTION(tyler)\n" + " OPENPARENTHESIS\n" + " SEPARATOR\n";

            string actualTokensString = "";
            foreach (List<Token> projectFile in tokens)
            {
                actualTokensString += string.Join("\n", projectFile) + "\n";
            }

            Assert.AreEqual(expectedTokensString, actualTokensString);
        }

        [TestMethod]
        public void OneSymbols()
        {
            string file = "} ( ) = > < + ^ + - : * / % , ! \" \n \t";
            List<string> fileList = new List<string>();
            fileList.Add(file);
            Lexer lexer = new Lexer(fileList);
            List<List<Token>> tokens = lexer.Lex();

            string actualTokensString = "";
            foreach (List<Token> projectFile in tokens)
            {
                actualTokensString += string.Join("\n", projectFile) + "\n";
            }

            string expectedTokensString =
                " CLOSEDANGLEBRACKET\n"
                + " OPENPARENTHESIS\n"
                + " CLOSEDPARENTHESIS\n"
                + " EQUAL\n"
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
                + " NEWLINE\n"
                + " TAB\n"
                + " SEPARATOR\n";

            Assert.AreEqual(expectedTokensString, actualTokensString);
        }

        [TestMethod]
        public void TwoCharacterHashmap()
        {
            string file = ">= ++ -- <= == != ^= %= *= /= += -= && || \n\t";
            List<string> fileList = new List<string>();
            fileList.Add(file);
            Lexer lexer = new Lexer(fileList);
            List<List<Token>> tokens = lexer.Lex();

            string actualTokensString = "";
            foreach (List<Token> projectFile in tokens)
            {
                actualTokensString += string.Join("\n", projectFile) + "\n";
            }

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
                + " NEWLINE\n"
                + " TAB\n"
                + " SEPARATOR\n";
            Assert.AreEqual(expectedTokensString, actualTokensString);
        }

        [TestMethod]
        public void KeyWord()
        {
            string file =
                "if print getline nextfile function interface class string implements accessor "
                + "loop mutator console datetime construct boolean true false shared \t \n return";
            List<string> fileList = new List<string>();
            fileList.Add(file);
            Lexer lexer = new Lexer(fileList);
            List<List<Token>> tokens = lexer.Lex();

            string actualTokensString = "";
            foreach (List<Token> projectFile in tokens)
            {
                actualTokensString += string.Join("\n", projectFile) + "\n";
            }

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
                + " LOOP\n"
                + " MUTATOR\n"
                + " CONSOLE\n"
                + " DATETIME\n"
                + " CONSTRUCT\n"
                + " BOOLEAN\n"
                + " TRUE\n"
                + " FALSE\n"
                + " SHARED\n"
                + " TAB\n"
                + " NEWLINE\n"
                + " RETURN\n"
                + " SEPARATOR\n";
            Assert.AreEqual(expectedTokensString, actualTokensString);
        }

        [TestMethod]
        public void Wrong()
        {
            string file = "#";
            List<string> fileList = new List<string>();
            fileList.Add(file);
            Lexer lexer = new Lexer(fileList);

            Assert.ThrowsException<ArgumentException>(
                () => lexer.Lex(),
                "UNRECOGNIZED CHARACTER: #"
            );
        }
    }
}
