using System.Text;

namespace Shank
{
    public class Lexer
    {
        private enum modeType
        {
            Start,
            Number,
            Name,
            Comment,
            SingleQuote,
            DoubleQuote
        }

        private modeType mode = modeType.Start;
        private Dictionary<string, Token.TokenType> ReservedWords =
            new()
            {
                { "integer", Token.TokenType.Integer },
                { "real", Token.TokenType.Real },
                { "variables", Token.TokenType.Variables },
                { "constants", Token.TokenType.Constants },
                { "define", Token.TokenType.Define },
                { "if", Token.TokenType.If },
                { "then", Token.TokenType.Then },
                { "else", Token.TokenType.Else },
                { "elsif", Token.TokenType.Elsif },
                { "for", Token.TokenType.For },
                { "from", Token.TokenType.From },
                { "to", Token.TokenType.To },
                { "while", Token.TokenType.While },
                { "repeat", Token.TokenType.Repeat },
                { "until", Token.TokenType.Until },
                { "mod", Token.TokenType.Mod },
                { "var", Token.TokenType.Var },
                { "true", Token.TokenType.True },
                { "false", Token.TokenType.False },
                { "character", Token.TokenType.Character },
                { "boolean", Token.TokenType.Boolean },
                { "string", Token.TokenType.String },
                { "array", Token.TokenType.Array },
                { "of", Token.TokenType.Of },
                { "export", Token.TokenType.Export },
                { "import", Token.TokenType.Import },
                { "module", Token.TokenType.Module }
            };

        private void ModeChange(List<Token> retVal, StringBuilder currentBuffer, int lineNumber)
        {
            switch (mode)
            {
                case modeType.Number:
                    retVal.Add(
                        new Token(Token.TokenType.Number, lineNumber, currentBuffer.ToString())
                    );
                    currentBuffer.Clear();
                    break;
                case modeType.Name:
                    var val = currentBuffer.ToString();
                    retVal.Add(
                        ReservedWords.ContainsKey(val)
                            ? new Token(ReservedWords[val], lineNumber)
                            : new Token(Token.TokenType.Identifier, lineNumber, val)
                    );
                    currentBuffer.Clear();
                    break;
                case modeType.SingleQuote:
                    retVal.Add(
                        new Token(
                            Token.TokenType.CharContents,
                            lineNumber,
                            currentBuffer.ToString()
                        )
                    );
                    if (currentBuffer.Length > 1)
                        throw new Exception(
                            $"Character literals can only be one character long. Found: {currentBuffer}"
                        );
                    currentBuffer.Clear();
                    break;
                case modeType.DoubleQuote:
                    retVal.Add(
                        new Token(
                            Token.TokenType.StringContents,
                            lineNumber,
                            currentBuffer.ToString()
                        )
                    );
                    currentBuffer.Clear();
                    break;
            }
            if (mode != modeType.Comment)
                mode = modeType.Start;
        }

        public List<Token> Lex(string[] lines)
        {
            var retVal = new List<Token>();
            var currentBuffer = new StringBuilder();
            var currentIndentLevel = 0;
            var lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                int spaces = 0,
                    index = 0;

                for (index = 0; index < line.Length; index++)
                    if (line[index] == ' ')
                        spaces++;
                    else if (line[index] == '\t')
                        spaces += 4;
                    else
                        break;
                var thisIndentLevel = spaces / 4;
                if (index < line.Length) // there is still more to go - this wasn't an empty line
                {
                    while (thisIndentLevel > currentIndentLevel)
                    {
                        currentIndentLevel++;
                        retVal.Add(new Token(Token.TokenType.Indent, lineNumber));
                    }

                    while (thisIndentLevel < currentIndentLevel)
                    {
                        currentIndentLevel--;
                        retVal.Add(new Token(Token.TokenType.Dedent, lineNumber));
                    }
                }

                for (; index < line.Length; index++) // continue on
                {
                    var character = line[index];
                    if (mode == modeType.Comment)
                    {
                        switch (character)
                        {
                            case '*':
                                if (index < line.Length - 1)
                                {
                                    var next = line[index + 1];
                                    if (next == ')')
                                        mode = modeType.Start;
                                    index++;
                                }

                                break;
                        }
                    }
                    else if (mode == modeType.SingleQuote)
                    {
                        if (character == '\'')
                            ModeChange(retVal, currentBuffer, lineNumber);
                        else
                            currentBuffer.Append(character);
                    }
                    else if (mode == modeType.DoubleQuote)
                    {
                        if (character == '\"')
                            ModeChange(retVal, currentBuffer, lineNumber);
                        else
                            currentBuffer.Append(character);
                    }
                    else
                    {
                        switch (character)
                        {
                            case >= 'A'
                            and <= 'Z'
                            or (>= 'a' and <= 'z') when mode != modeType.Name:
                                if (mode != modeType.Name)
                                    ModeChange(retVal, currentBuffer, lineNumber);
                                mode = modeType.Name;
                                currentBuffer.Append(character);
                                break;
                            case >= 'A'
                            and <= 'Z'
                            or (>= 'a' and <= 'z')
                            or (>= '0' and <= '9' or '.') when mode == modeType.Name:
                                currentBuffer.Append(character);
                                break;
                            case (>= '0' and <= '9' or '.')
                                when (mode is modeType.Start or modeType.Number):
                                mode = modeType.Number;
                                currentBuffer.Append(character);
                                break;
                            case '-'
                            or '+' when mode is modeType.Number or modeType.Name:
                                ModeChange(retVal, currentBuffer, lineNumber);
                                retVal.Add(
                                    new Token(
                                        character == '+'
                                            ? Token.TokenType.Plus
                                            : Token.TokenType.Minus,
                                        lineNumber
                                    )
                                );
                                break;
                            case '-'
                            or '+' when mode is modeType.Start:
                                ModeChange(retVal, currentBuffer, lineNumber);
                                if (index < line.Length - 1)
                                {
                                    var next = line[index + 1];
                                    if (next is >= '0' and <= '9')
                                    {
                                        mode = modeType.Number;
                                        currentBuffer.Append(character);
                                    }
                                    else
                                        retVal.Add(
                                            new Token(
                                                character == '+'
                                                    ? Token.TokenType.Plus
                                                    : Token.TokenType.Minus,
                                                lineNumber
                                            )
                                        );
                                }
                                else
                                {
                                    // Last thing in the line is a minus. This doesn't make sense...
                                    retVal.Add(new Token(Token.TokenType.Minus, lineNumber));
                                }

                                break;
                            case ':':
                                ModeChange(retVal, currentBuffer, lineNumber);
                                if (index < line.Length - 1)
                                {
                                    var next = line[index + 1];
                                    if (next == '=')
                                    {
                                        index++;
                                        retVal.Add(
                                            new Token(Token.TokenType.Assignment, lineNumber)
                                        );
                                        continue;
                                    }
                                }

                                retVal.Add(new Token(Token.TokenType.Colon, lineNumber));
                                break;
                            case '>':
                                ModeChange(retVal, currentBuffer, lineNumber);
                                if (index < line.Length - 1)
                                {
                                    var next = line[index + 1];
                                    if (next == '=')
                                    {
                                        index++;
                                        retVal.Add(
                                            new Token(Token.TokenType.GreaterEqual, lineNumber)
                                        );
                                        continue;
                                    }
                                }

                                retVal.Add(new Token(Token.TokenType.Greater, lineNumber));
                                break;
                            case '<':
                                ModeChange(retVal, currentBuffer, lineNumber);
                                if (index < line.Length - 1)
                                {
                                    var next = line[index + 1];
                                    if (next == '=')
                                    {
                                        index++;
                                        retVal.Add(
                                            new Token(Token.TokenType.LessEqual, lineNumber)
                                        );
                                        continue;
                                    }

                                    if (next == '>')
                                    {
                                        index++;
                                        retVal.Add(new Token(Token.TokenType.NotEqual, lineNumber));
                                        continue;
                                    }
                                }

                                retVal.Add(new Token(Token.TokenType.LessThan, lineNumber));
                                break;
                            case ';':
                                ModeChange(retVal, currentBuffer, lineNumber);
                                retVal.Add(new Token(Token.TokenType.Semicolon, lineNumber));
                                break;
                            case ',':
                                ModeChange(retVal, currentBuffer, lineNumber);
                                retVal.Add(new Token(Token.TokenType.Comma, lineNumber));
                                break;
                            case '=':
                                ModeChange(retVal, currentBuffer, lineNumber);
                                retVal.Add(new Token(Token.TokenType.Equal, lineNumber));
                                break;
                            case '[':
                                ModeChange(retVal, currentBuffer, lineNumber);
                                retVal.Add(new Token(Token.TokenType.LeftBracket, lineNumber));
                                break;
                            case ']':
                                ModeChange(retVal, currentBuffer, lineNumber);
                                retVal.Add(new Token(Token.TokenType.RightBracket, lineNumber));
                                break;
                            case ('*' or '/'):
                                ModeChange(retVal, currentBuffer, lineNumber);
                                retVal.Add(
                                    new Token(
                                        character == '*'
                                            ? Token.TokenType.Times
                                            : Token.TokenType.Divide,
                                        lineNumber
                                    )
                                );
                                break;
                            case ')':
                                ModeChange(retVal, currentBuffer, lineNumber);
                                retVal.Add(new Token(Token.TokenType.RightParen, lineNumber));
                                break;
                            case '(':
                                ModeChange(retVal, currentBuffer, lineNumber);
                                if (index < line.Length - 1)
                                {
                                    var next = line[index + 1];
                                    if (next == '*')
                                        mode = modeType.Comment;
                                }

                                if (mode != modeType.Comment)
                                    retVal.Add(new Token(Token.TokenType.LeftParen, lineNumber));
                                break;
                            case '\'':
                                mode = modeType.SingleQuote;
                                break;
                            case '"':
                                mode = modeType.DoubleQuote;
                                break;
                            case ' ':
                                ModeChange(retVal, currentBuffer, lineNumber);
                                break;
                        }
                    }
                }

                ModeChange(retVal, currentBuffer, lineNumber);
                retVal.Add(new Token(Token.TokenType.EndOfLine, lineNumber));
            }

            while (0 < currentIndentLevel)
            {
                currentIndentLevel--;
                retVal.Add(new Token(Token.TokenType.Dedent, lineNumber));
            }
            return retVal;
        }
    }
}
