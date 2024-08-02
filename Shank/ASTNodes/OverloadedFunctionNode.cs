using Shank.ExprVisitors;
using Shank.WalkCompliantVisitors;

namespace Shank.ASTNodes;

public class OverloadedFunctionNode(
    string name,
    string moduleName,
    Dictionary<TypeIndex, CallableNode> overloads,
    bool isPublicIn = false
) : CallableNode(name, moduleName, isPublicIn: isPublicIn)
{
    public Dictionary<TypeIndex, CallableNode> Overloads { get; set; } = overloads;

    public override void Accept(Visitor v)
    {
        v.Visit(this);
    }

    public override ASTNode? Walk(SAVisitor v)
    {
        var temp = v.Visit(this);
        if (temp != null)
            return temp;
        Overloads = Overloads
            .Select(o => (o.Key, (CallableNode)(o.Value.Walk(v) ?? o.Value)))
            .ToDictionary();

        return v.PostWalk(this);
    }

    public override ASTNode Walk(WalkCompliantVisitor v)
    {
        var ret = v.Visit(this, out var shortCircuit);
        if (shortCircuit)
        {
            return ret;
        }

        Overloads = Overloads.WalkDictionary(v);

        return v.Final(this);
    }
}
