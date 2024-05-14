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
        return Message + "\n at line " + _lineNum + ", cause: " + _cause;
    }
}
