namespace Shank;

public class SyntaxErrorException : Exception
{
    private readonly Token? _cause;

    public SyntaxErrorException(string message, Token? causeToken)
        : base(message)
    {
        _cause = causeToken;
    }

    public override string ToString()
    {
        return Message
            + "\n at line "
            + (_cause?.LineNumber ?? 0)
            + ", token: "
            + (_cause?.Type ?? Token.TokenType.EndOfLine)
            + " "
            + (_cause?.Value ?? string.Empty);
    }
}
