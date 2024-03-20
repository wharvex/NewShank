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
            Dot,
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

        public string GetIdentifierValue()
        {
            // TODO: Convert this to a method that gets the non-null Value of a Token of any type
            // for which something has gone wrong internally if Value is null.
            if (Type != TokenType.Identifier)
            {
                throw new InvalidOperationException("This method is for Identifier Tokens only.");
            }

            if (Value is null)
            {
                throw new InvalidOperationException(
                    "Something went wrong internally. A Token of type Identifier should not have a"
                        + " null Value."
                );
            }

            return Value;
        }

        public override string ToString()
        {
            return $"[{Type}]" + (Value is null ? "" : " : " + Value);
        }
    }
}
