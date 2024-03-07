namespace Shank
{
    public class Token
    {
        public Token(TokenType type, int lineNumber, string? value = null)
        {
            Type = type;
            Value = value;
            LineNumber = lineNumber;
        }

        public enum TokenType
        {
            Number,
            Plus,
            Minus,
            Times,
            Divide,
            Mod,
            Identifier,
            EndOfLine,
            LeftParen,
            RightParen,
            Integer,
            Real,
            Indent,
            Dedent,
            Semicolon,
            Colon,
            Comma,
            Variables,
            Constants,
            Define,
            Record,
            Assignment,
            If,
            Then,
            Else,
            Elsif,
            For,
            From,
            To,
            While,
            Repeat,
            Until,
            Greater,
            LessThan,
            GreaterEqual,
            LessEqual,
            Equal,
            NotEqual,
            Var,
            True,
            False,
            Boolean,
            Character,
            String,
            Array,
            Of,
            Module,
            Export,
            Import,
            LeftBracket,
            RightBracket,
            CharContents,
            StringContents,
            Test
        }

        public TokenType Type { get; init; }
        public string? Value { get; init; }
        public int LineNumber { get; init; }

        public override string ToString()
        {
            return $"[{Type}]" + (Value is null ? "" : " : " + Value);
        }
    }
}
