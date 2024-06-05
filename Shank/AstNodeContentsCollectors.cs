using System.Diagnostics;
using Shank.ASTNodes;

namespace Shank;

public static class AstNodeContentsCollectors
{
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
            _ => throw new UnreachableException()
        };
}
