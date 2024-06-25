using Shank.ExprVisitors;

namespace Shank.ASTNodes;

public class EnumNode(string type, string parentModuleName, List<string> enumElements)
    : ExpressionNode
//required because interpter class on line 572 (if you chamge it back it has an error
{
    public string TypeName => Type.Name;
    public EnumType Type = new(type,parentModuleName, enumElements);
    public List<string> EnumElements => Type.Variants;
    public string ParentModuleName { get; set; } = parentModuleName;
    public bool IsPublic { get; set; } = false;

    // public override LLVMValueRef Visit(
    //     LLVMVisitor visitor,
    //     Context context,
    //     LLVMBuilderRef builder,
    //     LLVMModuleRef module
    // )
    // {
    //     visitor.Visit(this);
    //     throw new Exception("not implemented uet");
    //     // return b
    // }


    public override T Accept<T>(ExpressionVisitor<T> visit)
    {
        throw new NotImplementedException();
    }

    public override void Accept<T>(StatementVisitor v)
    {
        throw new NotImplementedException();
    }

    public override void Accept(Visitor v) => v.Visit(this);
}
