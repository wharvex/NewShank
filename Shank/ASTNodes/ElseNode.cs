using LLVMSharp.Interop;

namespace Shank;

public class ElseNode : IfNode
{
    // code for generating llvm ir is done currently within IfNode
    public ElseNode(List<StatementNode> children)
        : base(children) { }

    public override string ToString()
    {
        return $" Else: {StatementListToString(Children)} {((NextIfNode == null) ? string.Empty : NextIfNode)}";
    }
}
