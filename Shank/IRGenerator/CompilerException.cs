namespace Shank;

public class CompilerException(string message, int lineNum) : Exception(message)
{
    private readonly int _lineNum = lineNum;

    public override string ToString()
    {
        string RED = "\x1b[91m"; // Red foreground
        string RESET = "\x1b[0m"; // Reset to default colors
        return RED
            + Message
            + " at line "
            + _lineNum
            + "\nStack Trace: \n"
            + RESET
            + base.ToString();
    }
}
