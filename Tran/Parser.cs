//MAKE A VARIABLE CALLED LINE SOMEWHERE HERE FOR ASTNODE and this: GetDataTypeFromConstantNodeType, and Module()

using LLVMSharp;
using Shank;
using Shank.ASTNodes;

namespace Shank.Tran;

public class Parser
{
    private TokenHandler handler;
    private ProgramNode program;

    public Parser(LinkedList<Token> tokens)
    {
        handler = new TokenHandler(tokens);
        program = new ProgramNode();
    }

    private bool AcceptSeperators()
    {
        bool retVal = false;
        while (handler.MatchAndRemove(TokenType.SEPERATOR) != null)
        {
            retVal = true;
        }
        return retVal;
    }

    public ProgramNode Parse()
    {
        while (handler.MoreTokens())
        {
            AcceptSeperators();
            if (ParseClass())
            {
                AcceptSeperators();
            }

            if (ParseFunction())
            {
                AcceptSeperators();
                continue;
            }

            throw new Exception("Not a function or action");
        }

        return program;
    }

    private bool ParseClass()
    {
        ModuleNode module;
        if (handler.MatchAndRemove(TokenType.CLASS) != null)
        {
            Token? name;
            if ((name = handler.MatchAndRemove(TokenType.WORD)) != null)
            {
                program.AddToModules(new ModuleNode(name.GetValue()));
                return true;
            }

            throw new Exception("No name provided for class");
        }

        return false;
    }

    private bool ParseFunction()
    {
        FunctionNode functionNode;
        Token? function;
        if ((function = handler.MatchAndRemove(TokenType.FUNCTION)) != null)
        {
            functionNode = new FunctionNode(function.GetValue(), "FunctionNode", true);
        }

        return false;
    }
}
