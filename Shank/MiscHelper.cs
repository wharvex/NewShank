namespace Shank;

public static class MiscHelper
{
    // https://stackoverflow.com/a/6499344/16458003
    public static void UpdateKey<TKey, TValue>(
        this IDictionary<TKey, TValue> d,
        TKey fromKey,
        TKey toKey
    )
    {
        var temp = d[fromKey];
        d.Remove(fromKey);
        d[toKey] = temp;
    }
}
