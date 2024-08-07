using Shank.ASTNodes;

namespace Shank.WalkCompliantVisitors;

public static class WalkCompliantVisitorsHelper
{
    public static List<T> WalkList<T>(this List<T> l, WalkCompliantVisitor v)
        where T : ASTNode
    {
        // This could be written as a one-liner (see below), but then we can't step into `n.Walk' in
        // the debugger, AND we can't use the "Step Into Specific" method described in
        // WalkDictionary further below.
        var ret = new List<T>();
        foreach (var n in l)
        {
            ret.Add((T)n.Walk(v));
        }

        return ret;

        // One-liner version (debugger-unfriendly).
        // return [.. l.Select(e => (T)e.Walk(v))];
    }

    public static Dictionary<TK, TV> WalkDictionary<TK, TV>(
        this Dictionary<TK, TV> d,
        WalkCompliantVisitor v
    )
        where TV : ASTNode
        where TK : notnull
    {
        // The one-liner below is the equivalent of this:
        // var ret = new Dictionary<TK, TV>();
        // foreach (var p in d)
        // {
        //     ret[p.Key] = (TV)p.Value.Walk(v);
        // }
        // return ret;

        // One-liner version.
        // If you want to step into kvp.Value.Walk from here in the debugger, do this in VS:
        // Set a breakpoint on this line and Start Debugging (F5);
        // Right-click anywhere in the editor window when the debugger hits this line, then:
        // Step Into Specific -> Shank.ASTNodes.ASTNode.Walk
        return d.Select(kvp => new KeyValuePair<TK, TV>(kvp.Key, (TV)kvp.Value.Walk(v)))
            .ToDictionary();
    }
}
