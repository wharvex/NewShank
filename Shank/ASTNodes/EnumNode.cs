using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

public class EnumNode : ASTNode
{
    public string Type { get; set; }
    public string ParentModuleName { get; set; }
    public LinkedList<String> EnumElements;
    public bool IsPublic { get; set; }

    public EnumNode(string type, string parentModuleName, LinkedList<String> enumElements)
    {
        Type = type;
        ParentModuleName = parentModuleName;
        EnumElements = new LinkedList<string>(enumElements);
        IsPublic = false;
    }

    public override LLVMValueRef Visit(
        LLVMVisitor visitor,
        Context context,
        LLVMBuilderRef builder,
        LLVMModuleRef module
    )
    {
        throw new NotImplementedException();
    }
}
