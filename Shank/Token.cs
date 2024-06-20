namespace Shank;

public class Token
{
    private readonly TokenType[] _nonNullValuedTokenTypes =
    [
        TokenType.Identifier,
        TokenType.Number,
        TokenType.CharContents,
        TokenType.StringContents,
    ];

    public Token(TokenType type, int lineNumber)
    {
        Type = type;
        Value = null;
        LineNumber = lineNumber;
    }

    public Token(TokenType type, int lineNumber, string value)
    {
        Type = type;
        Value = value;
        LineNumber = lineNumber;
    }

    public enum TokenType
    {
        Number,
        Plus,
        Minus,
        Times,
        Divide,
        Mod,
        Identifier,
        EndOfLine,
        LeftParen,
        RightParen,
        Integer,
        Real,
        Indent,
        Dedent,
        Semicolon,
        Colon,
        Comma,
        Dot,
        Variables,
        Constants,
        Define,
        Record,
        Generic,
        Assignment,
        If,
        Then,
        Else,
        Elsif,
        For,
        From,
        To,
        While,
        Repeat,
        Until,
        Greater,
        LessThan,
        GreaterEqual,
        LessEqual,
        Equal,
        NotEqual,
        Var,
        True,
        False,
        Boolean,
        Character,
        String,
        Array,
        Of,
        Module,
        Export,
        Import,
        LeftBracket,
        RightBracket,
        CharContents,
        StringContents,
        Test,
        Enum,
        RefersTo
    }

    public TokenType Type { get; init; }
    public string? Value { get; init; }
    public int LineNumber { get; init; }

    /// <summary>
    ///     Method <c>GetValueSafe</c> returns a token value only if it is an identifier, number, character content, or string content
    ///     and if a value is present
    /// </summary>
    /// <returns>Token value as a String</returns>
    /// <exception cref="InvalidOperationException">
    /// <list type="bullet">
    ///     <item>
    ///         <description>The tokens type does not meet the criteria above</description>
    ///     </item>
    ///     <item>
    ///         <description>A value is not present (is null)</description>
    ///     </item>
    /// </list>
    /// </exception>

    public string GetValueSafe()
    {
        if (!_nonNullValuedTokenTypes.Contains(Type))
        {
            throw new InvalidOperationException(
                "It is invalid to call this method on a Token whose Type is not"
                    + " Identifier, Number, CharContents, or StringContents."
            );
        }

        return Value
            ?? throw new InvalidOperationException(
                "Something went wrong internally. A Token of type "
                    + Type
                    + " should not have a null Value."
            );
    }

    public override string ToString()
    {
        return $"[{Type}]" + (Value is null ? "" : " : " + Value);
    }
}
