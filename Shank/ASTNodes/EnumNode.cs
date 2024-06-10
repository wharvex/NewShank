using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.IRGenerator;

namespace Shank.ASTNodes;

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
        visitor.Visit(this);
        throw new Exception("not implemented uet");
        // return b
    }

    public override T Visit<T>(ExpressionVisitor<T> visit)
    {
        throw new NotImplementedException();
    }
}
