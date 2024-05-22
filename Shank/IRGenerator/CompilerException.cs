namespace Shank;

public class CompilerException: Exception
{
    
    private readonly int _lineNum;
    public CompilerException(string message, int lineNum)
        : base(message)
    {
        _lineNum = lineNum;
    }

    public override string ToString()
    {
        return Message + "\n at line " + _lineNum;
    }
    
}