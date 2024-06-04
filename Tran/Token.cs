using System;

namespace Shank.Tran;

public enum TokenType
{
    INTERFACE,
    NUMBER,
    CLASS,
    STRING,
    IMPLEMENTS,
    ACCESSOR,
    VALUE,
    LOOP,
    WORD,
    MUTATOR,
    CONSOLE,
    PRINT,
    DATETIME,
    CONSTRUCT,
    CHARACTER,
    BOOLEAN,
    TRUE,
    FALSE,
    IF,
    SHARED,
    GETLINE,
    NEXTFILE,
    FUNCTION,
    SEPARATOR,
    GREATEREQUAL,
    DOUBLEPLUS,
    DOUBLEMINUS,
    LESSEQUAL,
    COLON,
    EQUALS,
    GREATERTHAN,
    LESSTHAN,
    NOT,
    PLUS,
    MINUS,
    EXPONENT,
    MULTIPLY,
    DIVIDE,
    MODULUS,
    NOTEQUAL,
    OPENPARENTHESIS,
    CLOSEDPARENTHESIS,
    OPENANGLEBRACKET,
    CLOSEDANGLEBRACKET,
    PERIOD,
    QUOTE,
    COMMA,
    EXPONENTEQUAL,
    PERCENTEQUALS,
    MULTIPLYEQUALS,
    DIVIDEEQUALS,
    PLUSEQUALS,
    MINUSEQUALS,
    PRIVATE,
    AND,
    OR,
    RETURN
}

public class Token
{
    private TokenType type;
    private string value;
    private int lineNumber;
    private int characterPosition;

    public Token(TokenType type, int lineNumber, int characterPosition)
    {
        this.type = type;
        this.lineNumber = lineNumber;
        this.characterPosition = characterPosition;
    }

    public Token(TokenType type, string value, int lineNumber, int characterPosition)
    {
        this.type = type;
        this.value = value;
        this.lineNumber = lineNumber;
        this.characterPosition = characterPosition;
    }

    public string GetValue()
    {
        return value;
    }

    public TokenType GetTokenType()
    {
        return type;
    }

    public override string ToString()
    {
        if (value != null)
        {
            if (characterPosition <= 0)
            {
                return type + "(" + value + ")";
            }
            else
            {
                return " " + type + "(" + value + ")";
            }
        }
        else
        {
            if (lineNumber < 2)
            {
                return " " + type.ToString();
            }
            else
            {
                return " " + type.ToString();
            }
        }
    }
}
