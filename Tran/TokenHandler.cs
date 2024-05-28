using System;
using System.Collections.Generic;

// Helper to allow for easier handling of the token list passed by the lexer
public class TokenHandler
{
    private LinkedList<Token> tokens;

    public TokenHandler(LinkedList<Token> tokens)
    {
        this.tokens = tokens;
    }

    // Peeks at the next token if it is within the bounds of the list
    public Token? Peek(int j)
    {
        if (j < tokens.Count)
            return tokens.ElementAt(j);
        return null;
    }

    // Returns true if there are more tokens in the list
    public bool MoreTokens()
    {
        return tokens.Count > 0;
    }

    // Removes the token if the type matches with the first one in the list,
    // else returns an empty optional
    public Token? MatchAndRemove(TokenType t)
    {
        if (tokens.Count <= 0)
            return null;
        if (tokens.First.Value.GetType().Equals(t))
        {
            return tokens.First.Value;
        }
        return null;
    }
}
