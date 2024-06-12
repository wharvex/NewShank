using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.ExprVisitors;
using Shank.IRGenerator;
using Shank.IRGenerator.CompilerPractice.AstNodeVisitors;

namespace Shank.ASTNodes;

public class EnumNode : ExpressionNode
{
    public string Type { get; set; }
    public EnumType NewType;

    public string ParentModuleName { get; set; }

    // public LinkedList<String> EnumElements;
    public bool IsPublic { get; set; }

    public EnumNode(string type, string parentModuleName, List<string> enumElements)
    {
        Type = type;
        ParentModuleName = parentModuleName;
        NewType = new EnumType(type, (enumElements));
        IsPublic = false;
    }

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

    public override T Accept<T>(IAstNodeVisitor<T> visitor) => visitor.Visit(this);
}
