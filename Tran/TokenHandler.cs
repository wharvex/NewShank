using System;
using System.Collections.Generic;

namespace Shank.Tran;

// Helper to allow for easier handling of the token list passed by the lexer
public class TokenHandler
{
    private List<Token> tokens;

    public TokenHandler(List<Token> tokens)
    {
        this.tokens = tokens;
    }

    // Peeks at the next token if it is within the bounds of the list
    public Token? Peek(int j)
    {
        return j < tokens.Count ? tokens.ElementAt(j) : null;
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
        if (tokens.Count > 0 && tokens[0].GetTokenType() == t)
        {
            var matchedToken = tokens[0];
            tokens.RemoveAt(0);
            //  PrintToken(matchedToken);
            return matchedToken;
        }
        return null;
    }

    public void PrintRemainingTokens()
    {
        int count = 0;
        int total = tokens.Count;
        while (count < tokens.Count)
        {
            Console.Write(tokens.ElementAt(count));
            count++;
        }
    }

    //to debug
    public void PrintToken(Token token)
    {
        Console.WriteLine($"Token: {token.GetType()}, Value={token.GetValue()}");
    }
}
