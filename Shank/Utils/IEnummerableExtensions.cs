namespace Shank.Utils;

public static class EnumerableExtensions
{
    // Tries to run the function on each element short-circuiting if the returns left for an element.
    public static Either<IEnumerable<TLeft>, TRight> TryAll<T, TLeft, TRight>(
        this IEnumerable<T> source,
        Func<T, Either<TLeft, TRight>> func
    ) =>
        source.Aggregate(
            Either<IEnumerable<TLeft>, TRight>.Left([]),
            (accumulated, current) =>
                accumulated.FlatMap(goodAccumulated => func(current).Map(goodAccumulated.Append))
        );

    // Runs the function on all the elements and splits the results in the Left and Right Lists.
    public static (IEnumerable<TLeft> Left, IEnumerable<TRight> Right) AggregateEither<
        T,
        TLeft,
        TRight
    >(this IEnumerable<T> source, Func<T, Either<TLeft, TRight>> func) =>
        source.Aggregate(
            (Left: (IEnumerable<TLeft>)[], Right: (IEnumerable<TRight>)[]),
            (results, current) =>
                func(current) switch
                {
                    Left<TLeft, TRight>(var left) => results with { Left = [..results.Left, left] },
                    Right<TLeft, TRight>(var right)
                        => results with
                        {
                            Right = [..results.Right, right]
                        },
                }
        );

    // Format a list to a string seperated by the delimiter which defaults to `, `, default to using `[`, `]` to surround the list.
    // Converts each element to a string using the toString function you provide.
    public static string ToString<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, string> toString,
        string delimiter = ", ",
        string open = "[",
        string close = "]"
    ) => $"{open}{string.Join(delimiter, source.Select(toString))}{close}";

    // Format a list to a string seperated by the delimiter which defaults to `, `, default to using `[`, `]` to surround the list.
    // Note: uses ToString to convert each element to a string
    public static string ToString<TSource>(
        this IEnumerable<TSource> source,
        string delimiter = ", ",
        string open = "[",
        string close = "]"
    ) => source.ToString(item => item.ToString(), delimiter, open, close);
}
