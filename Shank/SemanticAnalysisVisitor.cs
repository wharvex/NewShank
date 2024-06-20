using Shank.ASTNodes;
using Shank.ExprVisitors;

namespace Shank;

/// <summary>
/// something the semantic analysis team should do tbh
/// </summary>
/// <param name="name"></param>
/// <param name="type"></param>
// struct Function(string name, FunctionType type)
// {
//     private string name;
//     private FunctionType type;
//
//     public void CheckType() { }
// }

public class SemanticAnalysisVisitor : Visitor
{
    private ProgramNode program;
    public ModuleNode StartModule { get; set; }
    public static Dictionary<string, ModuleNode>? Modules { get; set; }

    public override void Visit(IntNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(FloatNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(VariableUsagePlainNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(CharNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(BoolNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(StringNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(MathOpNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(BooleanExpressionNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(RecordNode node)
    {
        throw new NotImplementedException();
    }

    // public override void Visit(ParameterNode node)
    // {
    //     throw new NotImplementedException();
    // }

    public override void Visit(FunctionCallNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(FunctionNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(WhileNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(AssignmentNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(EnumNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(ModuleNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(IfNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(RepeatNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(VariableDeclarationNode node)
    {
        throw new NotImplementedException();
    }

    public override void Visit(ProgramNode node)
    {
        StartModule = node.GetStartModuleSafe();
        node.Modules.Values.ToList()
            .ForEach(n =>
            {
                n.Accept(this);
            });
    }

    public override void Visit(ForNode node)
    {
        throw new NotImplementedException();
    }
}
