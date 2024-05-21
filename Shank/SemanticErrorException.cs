using Shank.ASTNodes;

namespace Shank;

public class SemanticErrorException : Exception
{
    private readonly ASTNode? _cause;

    public SemanticErrorException(string message, ASTNode? causeNode)
        : base(message)
    {
        _cause = causeNode;
    }

    public override string ToString()
    {
        return Message
            + "\n at line "
            + (_cause?.Line ?? 0)
            + ", node: "
            + (_cause?.NodeName ?? "??");
    }
}
