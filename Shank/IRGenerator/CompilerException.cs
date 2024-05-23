namespace Shank;

public class CompilerException(string message, int lineNum) : Exception(message)
{
    
    private readonly int _lineNum = lineNum;

    public override string ToString()
    {
        return Message + "\n at line " + _lineNum;
    }
}
