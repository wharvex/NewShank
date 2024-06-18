using System.Diagnostics;
using System.Linq.Expressions;
using Shank.ASTNodes;

namespace Shank;

public static class ContentsCollectors
{
    public static List<ASTNode?> ChildNodesCollector(this ASTNode node) =>
        node switch
        {
            ModuleNode m
                =>
                [
                    ..m.Functions.Select(kvp => kvp.Value),
                    ..m.Records.Select(kvp => kvp.Value),
                    ..m.Enums.Select(kvp => kvp.Value),
                    ..m.GlobalVariables.Select(kvp => kvp.Value)
                ],
            ProgramNode p => [..p.Modules.Select(kvp => kvp.Value)],
            AssignmentNode a => [a.Target, a.Expression],
            BoolNode => [],
            BooleanExpressionNode be => [be.Left, be.Right],
            BuiltInFunctionNode bf => [],
            CharNode ch => [],
            IfNode i => [i.Expression, ..i.Children, i.NextIfNode],
            FunctionNode f => [..f.Statements, ..f.LocalVariables],
            _ => throw new UnreachableException()
        };

    //public static List<Option<ASTNode>> ChildNodesCollectorOptional(this ASTNode node) =>
    //    node switch
    //    {
    //        ModuleNode m
    //            =>
    //            [
    //                ..m.Functions.Select(kvp => ((ASTNode)kvp.Value).Some() ),
    //                ..m.Records.Select(kvp => ((ASTNode)kvp.Value).Some()),
    //                ..m.Enums.Select(kvp => ((ASTNode)kvp.Value).Some()),
    //                ..m.GlobalVariables.Select(kvp => ((ASTNode)kvp.Value).Some())
    //            ],
    //        ProgramNode p => [..p.Modules.Select(kvp => ((ASTNode)kvp.Value).Some())],
    //        AssignmentNode a => [((ASTNode)a.Target).Some(), a.Expression.Some()],
    //        BoolNode => [],
    //        BooleanExpressionNode be => [be.Left.Some(), be.Right.Some()],
    //        BuiltInFunctionNode bf => [],
    //        CharNode ch => [],
    //        IfNode i
    //            =>
    //            [
    //                i.Expression is null
    //                    ? Option.None<ASTNode>()
    //                    : Option.Some<ASTNode>(i.Expression),
    //                ..i.Children.Select(n => ((ASTNode)n).Some()),
    //                i.NextIfNode is null
    //                    ? Option.None<ASTNode>()
    //                    : Option.Some<ASTNode>(i.NextIfNode),
    //                ..i.ElseBlock?.Select(n => ((ASTNode)n).Some())
    //            ],
    //        FunctionNode f
    //            =>
    //            [
    //                ..f.Statements.Select(n => ((ASTNode)n).Some()),
    //                ..f.LocalVariables.Select(n => ((ASTNode)n).Some())
    //            ],
    //        _ => throw new UnreachableException()
    //    };

    public static string? NamesCollector(this ASTNode node)
    {
        return node switch
        {
            ModuleNode m => m.Name,
            CallableNode c => c.Name,
            VariableDeclarationNode v => v.Name,
            _ => throw new UnreachableException()
        };
    }
}
