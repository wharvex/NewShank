using System;
using System.Collections.Generic;
using System.IO;

namespace Shank.Tran;

public class Lexer
{
    private StringHandler stringHandler;
    private int lineNumber = 1;
    private int characterPosition = 0;
    private LinkedList<Token> tokens = new LinkedList<Token>();
    private Dictionary<string, TokenType> keywordHash = new Dictionary<string, TokenType>();
    private Dictionary<string, TokenType> twoCharacterHash = new Dictionary<string, TokenType>();
    private Dictionary<string, TokenType> oneCharacterHash = new Dictionary<string, TokenType>();

    public Lexer(string inputs)
    {
        stringHandler = new StringHandler(inputs);
        KeyWord();
        TwoCharacterHashmap();
        OneCharacterHashmap();
    }

    private void KeyWord()
    {
        keywordHash["if"] = TokenType.IF;
        keywordHash["print"] = TokenType.PRINT;
        keywordHash["getline"] = TokenType.GETLINE;
        keywordHash["nextfile"] = TokenType.NEXTFILE;

        //TODO: !!! no such keyword, remove this and add lexing for function based on parentheses
        keywordHash["function"] = TokenType.FUNCTION;

        keywordHash["interface"] = TokenType.INTERFACE;
        keywordHash["class"] = TokenType.CLASS;
        keywordHash["string"] = TokenType.STRING;
        keywordHash["implements"] = TokenType.IMPLEMENTS;
        keywordHash["accessor"] = TokenType.ACCESSOR;
        keywordHash["value"] = TokenType.VALUE;
        keywordHash["loop"] = TokenType.LOOP;
        keywordHash["mutator"] = TokenType.MUTATOR;
        keywordHash["console"] = TokenType.CONSOLE;
        keywordHash["datetime"] = TokenType.DATETIME;
        keywordHash["construct"] = TokenType.CONSTRUCT;
        keywordHash["boolean"] = TokenType.BOOLEAN;
        keywordHash["true"] = TokenType.TRUE;
        keywordHash["false"] = TokenType.FALSE;
        keywordHash["shared"] = TokenType.SHARED;
        keywordHash["\t"] = TokenType.SEPERATOR;
    }

    private void TwoCharacterHashmap()
    {
        twoCharacterHash[">="] = TokenType.GREATEREQUAL;
        twoCharacterHash["++"] = TokenType.DOUBLEPLUS;
        twoCharacterHash["--"] = TokenType.DOUBLEMINUS;
        twoCharacterHash["<="] = TokenType.LESSEQUAL;
        twoCharacterHash["=="] = TokenType.EQUALS;
        twoCharacterHash["!="] = TokenType.NOTEQUAL;
        twoCharacterHash["^="] = TokenType.EXPONENTEQUAL;
        twoCharacterHash["%="] = TokenType.PERCENTEQUALS;
        twoCharacterHash["*="] = TokenType.MULTIPLYEQUALS;
        twoCharacterHash["/="] = TokenType.DIVIDEEQUALS;
        twoCharacterHash["+="] = TokenType.PLUSEQUALS;
        twoCharacterHash["-="] = TokenType.MINUSEQUALS;
    }

    private void OneCharacterHashmap()
    {
        oneCharacterHash["{"] = TokenType.OPENANGLEBRACKET;
        oneCharacterHash["}"] = TokenType.CLOSEDANGLEBRACKET;
        oneCharacterHash["("] = TokenType.OPENPARENTHESIS;
        oneCharacterHash[")"] = TokenType.CLOSEDPARENTHESIS;
        oneCharacterHash["="] = TokenType.EQUALS;
        oneCharacterHash[">"] = TokenType.GREATERTHAN;
        oneCharacterHash["<"] = TokenType.LESSTHAN;
        oneCharacterHash["+"] = TokenType.PLUS;
        oneCharacterHash["^"] = TokenType.EXPONENT;
        oneCharacterHash["-"] = TokenType.MINUS;
        oneCharacterHash[":"] = TokenType.COLON;
        oneCharacterHash["*"] = TokenType.MULTIPLY;
        oneCharacterHash["/"] = TokenType.DIVIDE;
        oneCharacterHash["%"] = TokenType.MODULUS;
        oneCharacterHash[","] = TokenType.COMMA;
        oneCharacterHash["."] = TokenType.PERIOD;
        oneCharacterHash["!"] = TokenType.NOT;
        oneCharacterHash["\""] = TokenType.QUOTE;
    }

    public LinkedList<Token> Lex()
    {
        while (!stringHandler.IsDone())
        {
            char currentCharacter = stringHandler.Peek(0);
            if (currentCharacter == ' ')
            {
                stringHandler.GetChar();
                characterPosition++;
            }
            else if (currentCharacter == '\n' || currentCharacter == '\t')
            {
                stringHandler.GetChar();
                lineNumber++;
                characterPosition++;
                tokens.AddFirst((new Token(TokenType.SEPERATOR, lineNumber, characterPosition)));
            }
            else if (currentCharacter == '\r')
            {
                stringHandler.GetChar();
                characterPosition++;
            }
            else if (char.IsLetter(currentCharacter))
            {
                if (IsSingleCharacterWord(currentCharacter))
                {
                    Token singleCharWordToken = ProcessSingleCharacterWord(currentCharacter);
                    tokens.AddFirst(
                        (
                            new Token(
                                TokenType.CHARACTER,
                                currentCharacter.ToString(),
                                lineNumber,
                                characterPosition
                            )
                        )
                    );
                }
                else
                {
                    Token wordProcessor = ProcessWord();
                    tokens.AddFirst(wordProcessor);
                }
            }
            //Research this one, done for now but can be fixed.
            else if (char.IsDigit(currentCharacter))
            {
                Token numberProcessor = ProcessNumber();
                tokens.AddFirst(numberProcessor);
            }
            else if (currentCharacter == '{')
            {
                while (stringHandler.Peek(0) != '\n')
                {
                    stringHandler.GetChar();
                    characterPosition++;
                }
            }
            else
            {
                Token OneTwoSymbols = ProcessSymbols();
                if (OneTwoSymbols != null)
                {
                    tokens.AddFirst(OneTwoSymbols);
                }
                else
                {
                    throw new ArgumentException("UNRECOGNIZED CHARACTER: " + currentCharacter);
                }
            }
        }
        tokens.AddFirst(new Token(TokenType.SEPERATOR, lineNumber, characterPosition));
        return tokens;
    }

    private Token ProcessWord()
    {
        int startPosition = characterPosition;
        TokenType tokenType;
        while (char.IsLetterOrDigit(stringHandler.Peek(0)) || stringHandler.Peek(0) == '_')
        {
            stringHandler.GetChar();
            characterPosition++;
        }
        int length = characterPosition - startPosition;
        string value = stringHandler.GetSubstring(startPosition, length);
        if (keywordHash.ContainsKey(value))
        {
            tokenType = keywordHash[value];
            return new Token(tokenType, lineNumber, startPosition);
        }
        return new Token(TokenType.WORD, value, lineNumber, startPosition);
    }

    private Token ProcessNumber()
    {
        int startPosition = characterPosition;
        bool decimalFound = false;
        while (
            char.IsDigit(stringHandler.Peek(0)) || (!decimalFound && stringHandler.Peek(0) == '.')
        )
        {
            char currentCharacter = stringHandler.GetChar();
            characterPosition++;
            if (currentCharacter == '.')
            {
                decimalFound = true;
            }
        }
        int length = characterPosition - startPosition;
        string value = stringHandler.GetSubstring(startPosition, length);
        return new Token(TokenType.NUMBER, value, lineNumber, startPosition);
    }

    private Token ProcessSymbols()
    {
        string twoCharacterSymbols = stringHandler.PeekString(2);
        string oneCharacterSymbols = stringHandler.PeekString(1);
        if (twoCharacterHash.ContainsKey(twoCharacterSymbols))
        {
            stringHandler.GetChar();
            stringHandler.GetChar();
            characterPosition += 2;
            return new Token(twoCharacterHash[twoCharacterSymbols], lineNumber, characterPosition);
        }
        if (oneCharacterHash.ContainsKey(oneCharacterSymbols))
        {
            stringHandler.GetChar();
            characterPosition++;
            return new Token(oneCharacterHash[oneCharacterSymbols], lineNumber, characterPosition);
        }
        return null;
    }

    private bool IsSingleCharacterWord(char character)
    {
        return char.IsLetter(character);
    }

    private Token ProcessSingleCharacterWord(char character)
    {
        int startPosition = characterPosition;
        stringHandler.GetChar();
        characterPosition++;
        return new Token(TokenType.WORD, character.ToString(), lineNumber, startPosition);
    }
}
