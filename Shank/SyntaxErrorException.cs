namespace Shank
{
    // TODO: This is the only internal class in the project. Why is it internal?
    // Based on briefly reading about this keyword, it seems like it would only be useful if
    // our project had more than one "assembly." Does our project have more than one assembly?
    internal class SyntaxErrorException : Exception
    {
        private Token? cause;

        public SyntaxErrorException(string message, Token? causeToken)
            : base(message)
        {
            cause = causeToken;
        }

        public override string ToString()
        {
            return Message
                + "\n at line "
                + (cause?.LineNumber ?? 0)
                + ", token: "
                + (cause?.Type ?? Token.TokenType.EndOfLine)
                + " "
                + (cause?.Value ?? string.Empty);
        }
    }
}
