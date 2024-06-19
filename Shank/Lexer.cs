using System.Text;

namespace Shank;

public class Lexer
{
    private enum ModeType
    {
        Start,
        Number,
        Name,
        Comment,
        SingleQuote,
        DoubleQuote
    }

    private ModeType Mode { get; set; } = ModeType.Start;
    private Dictionary<string, Token.TokenType> ReservedWords { get; } =
        new()
        {
            { "integer", Token.TokenType.Integer },
            { "real", Token.TokenType.Real },
            { "variables", Token.TokenType.Variables },
            { "constants", Token.TokenType.Constants },
            { "define", Token.TokenType.Define },
            { "record", Token.TokenType.Record },
            { "generic", Token.TokenType.Generic },
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
            { "module", Token.TokenType.Module },
            { "test", Token.TokenType.Test },
            { "enum", Token.TokenType.Enum },
            { "refersTo", Token.TokenType.RefersTo }
        };

    /// <summary>
    /// This method prepares for a mode change.
    /// It creates an appropriate Token if needed.
    /// It always clears the current buffer, unless there is an error.
    /// It sets ModeType to `Start' if ModeType is not `Comment', unless there is an error.
    /// It is called at the end of every line, so we don't set ModeType to `Start' if ModeType is
    /// `Comment' because we want to let comments span multiple lines.
    /// </summary>
    /// <param name="retVal">The Token list</param>
    /// <param name="currentBuffer">Stores what will be the Token's Value property if needed</param>
    /// <param name="lineNumber"></param>
    /// <exception cref="TokenizationErrorException">If character literal is too long</exception>
    private void ModeChangePrep(List<Token> retVal, StringBuilder currentBuffer, int lineNumber)
    {
        switch (Mode)
        {
            case ModeType.Number:
                retVal.Add(new Token(Token.TokenType.Number, lineNumber, currentBuffer.ToString()));
                break;
            case ModeType.Name:
                var val = currentBuffer.ToString();
                retVal.Add(
                    ReservedWords.TryGetValue(val, out var reservedWord)
                        ? new Token(reservedWord, lineNumber)
                        : new Token(Token.TokenType.Identifier, lineNumber, val)
                );
                break;
            case ModeType.SingleQuote:
                if (currentBuffer.Length > 1)
                {
                    throw new TokenizationErrorException(
                        "Character literals can only be one character long.",
                        lineNumber,
                        currentBuffer.ToString()
                    );
                }
                retVal.Add(
                    new Token(Token.TokenType.CharContents, lineNumber, currentBuffer.ToString())
                );
                break;
            case ModeType.DoubleQuote:
                retVal.Add(
                    new Token(Token.TokenType.StringContents, lineNumber, currentBuffer.ToString())
                );
                break;
        }

        currentBuffer.Clear();

        if (Mode != ModeType.Comment)
        {
            Mode = ModeType.Start;
        }
    }

    private static void UpdateIndents(
        int thisIndentLevel,
        ref int currentIndentLevel,
        int lineNumber,
        List<Token> retVal
    )
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

    private void SpacesAndIndents(
        ref int index,
        int lineNumber,
        ref int currentIndentLevel,
        string line,
        List<Token> retVal
    )
    {
        // Initialize a flag to know when we're inside a curly brace block
        if (Mode == ModeType.Comment)
            return;

        var firstNonSpaceIdx = Enumerable
            .Range(index, line.Length)
            .Where(i =>
            {
                // If we encounter a '{', we must set the flag to true and skip the character
                if (line[i] == '{')
                {
                    Mode = ModeType.Comment;
                    return false;
                }

                // If we encounter a '}', we must set the flag to false and skip the character
                if (line[i] == '}')
                {
                    Mode = ModeType.Start;
                    return false;
                }

                // If we're inside a block, we skip the character
                if (Mode == ModeType.Comment)
                    return false;

                // Otherwise, we skip spaces and tabs
                return line[i] != ' ' && line[i] != '\t';
            })
            .FirstOrDefault(-1);

        // If still in the comment, then this method is done.
        if (Mode == ModeType.Comment)
            return;

        // If there is no such index, then this method is done.
        if (firstNonSpaceIdx < 0)
            return;

        // Count the initial spaces in the line.
        var initialSpaces = Enumerable.Range(index, firstNonSpaceIdx).Count(i => line[i] == ' ');

        // Calculate the initial tabs in the line.
        var initialTabs = firstNonSpaceIdx - index - initialSpaces;

        // Translate tabs into spaces.
        initialSpaces += initialTabs * 4;

        // Update the line index.
        index = firstNonSpaceIdx;

        // Get the indent level for this line based on the initial spaces count.
        var thisIndentLevel = initialSpaces / 4;

        // Match currentIndentLevel to this line and add Indent/Dedent tokens to retVal accordingly.
        UpdateIndents(thisIndentLevel, ref currentIndentLevel, lineNumber, retVal);
    }

    private static bool NextCharIsBetween(char c1, char c2, string line, int index) =>
        index < line.Length - 1 && line[index + 1] >= c1 && line[index + 1] <= c2;

    private static bool NextCharIs(char c, string line, int index) =>
        index < line.Length - 1 && line[index + 1] == c;

    private static bool NextCharIsEither(char c1, char c2, string line, int index) =>
        index < line.Length - 1 && (line[index + 1] == c1 || line[index + 1] == c2);

    private void MainSwitch(
        char character,
        StringBuilder currentBuffer,
        List<Token> retVal,
        int lineNumber,
        ref int index,
        string line
    )
    {
        switch (character)
        {
            case >= 'A'
            and <= 'Z'
            or (>= 'a' and <= 'z') when Mode != ModeType.Name:
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                Mode = ModeType.Name;
                currentBuffer.Append(character);
                break;
            case >= 'A'
            and <= 'Z'
            or (>= 'a' and <= 'z')
            or (>= '0' and <= '9') when Mode == ModeType.Name:
                currentBuffer.Append(character);
                break;
            case (>= '0' and <= '9') when (Mode is ModeType.Start or ModeType.Number):
                Mode = ModeType.Number;
                currentBuffer.Append(character);
                break;
            case ('.') when (Mode is ModeType.Number):
                currentBuffer.Append(character);
                break;
            case '-'
            or '+' when Mode is ModeType.Number or ModeType.Name:
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                retVal.Add(
                    new Token(
                        character == '+' ? Token.TokenType.Plus : Token.TokenType.Minus,
                        lineNumber
                    )
                );
                break;
            case '.':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                retVal.Add(new Token(Token.TokenType.Dot, lineNumber));
                break;
            case '-'
            or '+' when Mode is ModeType.Start:
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                if (NextCharIsBetween('0', '9', line, index))
                {
                    Mode = ModeType.Number;
                    currentBuffer.Append(character);
                    break;
                }
                retVal.Add(
                    new Token(
                        character == '+' ? Token.TokenType.Plus : Token.TokenType.Minus,
                        lineNumber
                    )
                );
                break;
            case ':':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                if (NextCharIs('=', line, index))
                {
                    index++;
                    retVal.Add(new Token(Token.TokenType.Assignment, lineNumber));
                    break;
                }
                retVal.Add(new Token(Token.TokenType.Colon, lineNumber));
                break;
            case '>':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                if (NextCharIs('=', line, index))
                {
                    index++;
                    retVal.Add(new Token(Token.TokenType.GreaterEqual, lineNumber));
                    break;
                }
                retVal.Add(new Token(Token.TokenType.Greater, lineNumber));
                break;
            case '<':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                if (NextCharIsEither('=', '>', line, index))
                {
                    index++;
                    retVal.Add(
                        new Token(
                            line[index] == '='
                                ? Token.TokenType.LessEqual
                                : Token.TokenType.NotEqual,
                            lineNumber
                        )
                    );
                    break;
                }
                retVal.Add(new Token(Token.TokenType.LessThan, lineNumber));
                break;
            case ';':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                retVal.Add(new Token(Token.TokenType.Semicolon, lineNumber));
                break;
            case ',':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                retVal.Add(new Token(Token.TokenType.Comma, lineNumber));
                break;
            case '=':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                retVal.Add(new Token(Token.TokenType.Equal, lineNumber));
                break;
            case '[':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                retVal.Add(new Token(Token.TokenType.LeftBracket, lineNumber));
                break;
            case ']':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                retVal.Add(new Token(Token.TokenType.RightBracket, lineNumber));
                break;
            case ('*' or '/'):
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                retVal.Add(
                    new Token(
                        character == '*' ? Token.TokenType.Times : Token.TokenType.Divide,
                        lineNumber
                    )
                );
                break;
            case '{':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                Mode = ModeType.Comment;
                break;
            case ')':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                retVal.Add(new Token(Token.TokenType.RightParen, lineNumber));
                break;
            case '(':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                retVal.Add(new Token(Token.TokenType.LeftParen, lineNumber));
                break;
            case '\'':
                Mode = ModeType.SingleQuote;
                break;
            case '"':
                Mode = ModeType.DoubleQuote;
                break;
            case ' ':
                ModeChangePrep(retVal, currentBuffer, lineNumber);
                break;
        }
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
            var index = 0;

            SpacesAndIndents(ref index, lineNumber, ref currentIndentLevel, line, retVal);

            // Main For-Loop
            for (; index < line.Length; index++)
            {
                var character = line[index];
                switch (Mode)
                {
                    case ModeType.Comment when character != '}':
                        break;
                    case ModeType.Comment:
                        Mode = ModeType.Start;
                        break;
                    case ModeType.SingleQuote when character == '\'':
                        ModeChangePrep(retVal, currentBuffer, lineNumber);
                        break;
                    case ModeType.SingleQuote:
                        currentBuffer.Append(character);
                        break;
                    case ModeType.DoubleQuote when character == '\"':
                        ModeChangePrep(retVal, currentBuffer, lineNumber);
                        break;
                    case ModeType.DoubleQuote:
                        currentBuffer.Append(character);
                        break;
                    default:
                        MainSwitch(character, currentBuffer, retVal, lineNumber, ref index, line);
                        break;
                }
            }

            if (Mode is ModeType.SingleQuote or ModeType.DoubleQuote)
            {
                throw new TokenizationErrorException(
                    "Unclosed string/character literal is invalid to produce a token.",
                    lineNumber,
                    currentBuffer.ToString()
                );
            }

            // Line end mode change.
            ModeChangePrep(retVal, currentBuffer, lineNumber);
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
