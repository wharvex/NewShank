namespace Shank;

public class FloatOrIntOrString
{
    private float _floatValue;
    private int _intValue;
    private string? _stringValue;

    public float FloatValue
    {
        get => _floatValueIsSet ? _floatValue : throw new InvalidOperationException();
        private set => _floatValue = value;
    }
    public int IntValue
    {
        get => _intValueIsSet ? _intValue : throw new InvalidOperationException();
        private set => _intValue = value;
    }
    public string StringValue
    {
        get => _stringValue ?? throw new InvalidOperationException();
        private set => _stringValue = value;
    }

    private readonly bool _floatValueIsSet;
    private readonly bool _intValueIsSet;

    public ContentsType ActiveContentsType;

    public enum ContentsType
    {
        Float,
        Int,
        String
    }

    public FloatOrIntOrString(string contents)
    {
        StringValue = contents;
        ActiveContentsType = ContentsType.String;
    }

    public FloatOrIntOrString(float contents)
    {
        FloatValue = contents;
        _floatValueIsSet = true;
        ActiveContentsType = ContentsType.Float;
    }

    public FloatOrIntOrString(int contents)
    {
        IntValue = contents;
        _intValueIsSet = true;
        ActiveContentsType = ContentsType.Float;
    }

    public static readonly FloatOrIntOrString Default = new FloatOrIntOrString("empty");
}
