namespace Shank
{
    public abstract class InterpreterDataType
    {
        public abstract override string ToString();
        public abstract void FromString(string input);
    }

    public class IntDataType : InterpreterDataType
    {
        public IntDataType(int value)
        {
            Value = value;
        }

        public int Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override void FromString(string input)
        {
            Value = int.Parse(input);
        }
    }

    public class FloatDataType : InterpreterDataType
    {
        public FloatDataType(float value)
        {
            Value = value;
        }

        public float Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override void FromString(string input)
        {
            Value = float.Parse(input);
        }
    }

    public class BooleanDataType : InterpreterDataType
    {
        public BooleanDataType(bool value)
        {
            Value = value;
        }

        public bool Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override void FromString(string input)
        {
            Value = bool.Parse(input);
        }
    }

    public class StringDataType : InterpreterDataType
    {
        public StringDataType(string value)
        {
            Value = value;
        }

        public string Value { get; set; }

        public override string ToString()
        {
            return Value;
        }

        public override void FromString(string input)
        {
            Value = input;
        }
    }

    public class CharDataType : InterpreterDataType
    {
        public CharDataType(char value)
        {
            Value = value;
        }

        public char Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override void FromString(string input)
        {
            Value = input[0];
        }
    }
}
