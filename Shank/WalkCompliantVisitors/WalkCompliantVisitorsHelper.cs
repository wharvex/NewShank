using Shank.ASTNodes;

namespace Shank.WalkCompliantVisitors;

public static class WalkCompliantVisitorsHelper
{
    public static List<T> WalkList<T>(this List<T> l, WalkCompliantVisitor v)
        where T : ASTNode
    {
        // This method could be a one-liner (see commented-out return statement below), but then for
        // some reason that syntax won't let us step into the "Walk" method in the debugger.
        var ret = new List<T>();
        foreach (var n in l)
        {
            ret.Add((T)n.Walk(v));
        }

        return ret;
        //return [..l.Select(e => (T)e.Walk(this))];
    }

    public static Dictionary<K, V> WalkDictionary<K, V>(
        this Dictionary<K, V> d,
        WalkCompliantVisitor v
    )
        where V : ASTNode
    {
        // This method could be a one-liner (see commented-out return statement below), but then for
        // some reason that syntax won't let us step into the "Walk" method in the debugger.
        var ret = new Dictionary<K, V>();
        foreach (var p in d)
        {
            ret[p.Key] = (V)p.Value.Walk(v);
        }

        return ret;

        //return d.Select(kvp => new KeyValuePair<string, T>(kvp.Key, (T)kvp.Value.Walk(this)))
        //    .ToDictionary();
    }
}
