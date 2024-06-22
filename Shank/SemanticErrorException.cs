using Shank.ASTNodes;

namespace Shank;

public class SemanticErrorException : Exception
{
    private readonly ASTNode? _cause;

    public SemanticErrorException(string message, ASTNode? causeNode = null)
        : base(message)
    {
        _cause = causeNode;
    }

    public override string ToString()
    {
        string RED = "\x1b[91m"; // Red foreground
        string RESET = "\x1b[0m"; // Reset to default colors
        return RED
            + Message
            + " at line "
            + (_cause?.Line ?? 0)
            + ", node: "
            + (_cause?.GetType().Name ?? "??")
            + "\nStack Trace: \n"
            + RESET
            + base.ToString();
    }
}
