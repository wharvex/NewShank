using System.Diagnostics;
using System.Linq.Expressions;
using Shank.ASTNodes;

namespace Shank;

public static class AstNodeContentsCollectors
{
    // Make a generic version of this method that lets you filter by the type of node you pass in.
    public static List<ASTNode> ContentsCollector(this ASTNode node) =>
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
            IfNode i => [i.Expression, ..i.Children, i.NextIfNode, ..i.ElseBlock],
            FunctionNode f => [..f.Statements, ..f.LocalVariables],
            _ => throw new UnreachableException()
        };
}
