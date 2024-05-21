namespace Shank;

public class BuiltInFunctionNode : CallableNode
{
    public BuiltInFunctionNode(string name, BuiltInCall execute)
        : base(name, execute) { }

    public bool IsVariadic = false;
}