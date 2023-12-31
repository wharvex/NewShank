using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shank
{
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
                + $"\n at line {cause?.LineNumber ?? 0}, token: {cause?.Type ?? Token.TokenType.EndOfLine} {cause?.Value ?? string.Empty}";
        }
    }
}
