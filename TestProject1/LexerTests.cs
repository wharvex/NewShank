using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shank.Tran;

[TestClass]
public class LexerTests
{
    [TestMethod]
    public void Words()
    {
        string file = "Haneen Qasem";
        Lexer lexer = new Lexer(file);
        LinkedList<Token> tokens = lexer.Lex();
        Assert.AreEqual("Haneen Qasem", tokens.ToString());
    }

    [TestMethod]
    public void Digits()
    {
        string file = "2.22 444 888";
        Lexer lexer = new Lexer(file);
        LinkedList<Token> tokens = lexer.Lex();
        Assert.AreEqual("2.22 444 888", tokens.ToString());
    }

    [TestMethod]
    public void OneSymbols()
    {
        string file = "{ } ( ) = > < + ^ + - : * / % , ! \"";
        Lexer lexer = new Lexer(file);
        LinkedList<Token> tokens = lexer.Lex();
        Assert.AreEqual(
            "OPENANGLEBRACKET CLOSEDANGLEBRACKET OPENPARENTHESIS CLOSEDPARENTHESIS EQUALS GREATERTHAN LESSTHAN PLUS EXPONENT MINUS COLON MULTIPLY DIVIDE MODULUS COMMAPERIOD NOT QUOTE",
            tokens.ToString()
        );
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
