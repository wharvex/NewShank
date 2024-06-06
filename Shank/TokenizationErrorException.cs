namespace Shank;

public class TokenizationErrorException : Exception
{
    private readonly string? _cause;
    private readonly int _lineNum;

    public TokenizationErrorException(string message, int lineNum, string? causeInput)
        : base(message)
    {
        _cause = causeInput;
        _lineNum = lineNum;
    }

    public override string ToString()
    {
        string RED = "\x1b[91m"; // Red foreground
        string RESET = "\x1b[0m"; // Reset to default colors
        return RED
            + Message
            + " at line "
            + _lineNum
            + ", cause: "
            + _cause
            + "\nStack Trace: \n"
            + RESET
            + base.ToString();
    }
}
