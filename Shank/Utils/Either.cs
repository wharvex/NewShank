namespace Shank.Utils;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable once InconsistentNaming
public interface Either<out TLeft, TRight>
#pragma warning restore IDE1006 // Naming Styles
{
    public Either<TLeftNew, TRight> FlatMap<TLeftNew>(Func<TLeft, Either<TLeftNew, TRight>> func);
    public Either<TLeftNew, TRight> Map<TLeftNew>(Func<TLeft, TLeftNew> func);
    public Either<TLeft, TRightNew> MapRight<TRightNew>(Func<TRight, TRightNew> func);
    public Either<TLeft, TRight> Filter(bool filter, Func<TRight> error);
    public Either<TLeft, TRight> Filter(bool filter, TRight error);
    public TLeft OrElseThrow(Func<TRight, Exception> exception);
    public TLeft OrElseThrow(Exception exception);
    public static Either<TLeft, TRight> Left(TLeft left) => new Left<TLeft, TRight>(left);
    public static Either<TLeft, TRight> Right(TRight right) => new Right<TLeft, TRight>(right);
}

public readonly record struct Left<TLeft, TRight>(TLeft LeftValue) : Either<TLeft, TRight>
{
    public Either<TLeft, TRight> Filter(bool filter, Func<TRight> error) =>
        filter ? this : new Right<TLeft, TRight>(error());

    public Either<TLeft, TRight> Filter(bool filter, TRight error) =>
        filter ? this : new Right<TLeft, TRight>(error);

    public TLeft OrElseThrow(Func<TRight, Exception> exception) => LeftValue;

    public TLeft OrElseThrow(Exception exception) => LeftValue;

    public Either<TLeftNew, TRight> FlatMap<TLeftNew>(Func<TLeft, Either<TLeftNew, TRight>> func) =>
        func(LeftValue);

    public Either<TLeftNew, TRight> Map<TLeftNew>(Func<TLeft, TLeftNew> func) =>
        new Left<TLeftNew, TRight>(func(LeftValue));

    public Either<TLeft, TRightNew> MapRight<TRightNew>(Func<TRight, TRightNew> func) =>
        new Left<TLeft, TRightNew>(LeftValue);
}

public readonly record struct Right<TLeft, TRight>(TRight RightValue) : Either<TLeft, TRight>
{
    public Either<TLeftNew, TRight> FlatMap<TLeftNew>(Func<TLeft, Either<TLeftNew, TRight>> func) =>
        new Right<TLeftNew, TRight>(RightValue);

    public Either<TLeftNew, TRight> Map<TLeftNew>(Func<TLeft, TLeftNew> func) =>
        new Right<TLeftNew, TRight>(RightValue);

    public Either<TLeft, TRight> Filter(bool filter, Func<TRight> error) => this;

    public Either<TLeft, TRight> Filter(bool filter, TRight error) => this;

    public Either<TLeft, TRightNew> MapRight<TRightNew>(Func<TRight, TRightNew> func) =>
        new Right<TLeft, TRightNew>(func(RightValue));

    public TLeft OrElseThrow(Func<TRight, Exception> exception) => throw exception(RightValue);

    public TLeft OrElseThrow(Exception exception) => throw exception;
}
