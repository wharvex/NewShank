namespace Shank.MathOppable;

public class MathOppableString : IMathOppable
{
    public string Contents { get; set; }

    public MathOppableString(string contents)
    {
        Contents = contents;
    }
}
