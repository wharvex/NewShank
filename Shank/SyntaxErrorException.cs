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
        string RED = "\x1b[91m"; // Red foreground
        string RESET = "\x1b[0m"; // Reset to default colors
        return RED
            + Message
            + " at line "
            + (_cause?.LineNumber ?? 0)
            + ", token: "
            + (_cause?.Type ?? Token.TokenType.EndOfLine)
            + " "
            + (_cause?.Value ?? string.Empty)
            + "\nStack Trace: \n"
            + RESET
            + base.ToString();
    }
}
