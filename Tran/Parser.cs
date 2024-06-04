using Shank.ASTNodes;

namespace Shank.Tran;

public class Parser
{
    private TokenHandler handler;
    private ProgramNode program;
    private ModuleNode thisClass;

    public Parser(LinkedList<Token> tokens)
    {
        handler = new TokenHandler(tokens);
        program = new ProgramNode();
    }

    private bool AcceptSeparators()
    {
        bool retVal = false;
        while (handler.MatchAndRemove(TokenType.SEPARATOR) != null)
        {
            retVal = true;
        }
        return retVal;
    }

    public ProgramNode Parse()
    {
        while (handler.MoreTokens())
        {
            if (!ParseClass() && !ParseInterface())
            {
                throw new Exception("No class declaration found in file");
            }
            AcceptSeparators();

            if (ParseFields() || ParseFunction())
            {
                AcceptSeparators();
                continue;
            }

            throw new Exception("Statement is not a function or field");
        }

        return program;
    }

    //TODO: implement this such that fields (e.g. variables belonging to the class, not a function) are parsed appropriately
    private bool ParseFields()
    {
        throw new NotImplementedException();
    }

    private bool ParseInterface()
    {
        if (handler.MatchAndRemove(TokenType.INTERFACE) != null)
        {
            Token? name;
            if ((name = handler.MatchAndRemove(TokenType.WORD)) != null)
            {
                thisClass = new ModuleNode(name.GetValue());
                return true;
            }

            throw new Exception("No name provided for interface");
        }

        return false;
    }

    private bool ParseClass()
    {
        if (handler.MatchAndRemove(TokenType.CLASS) != null)
        {
            Token? name;
            if ((name = handler.MatchAndRemove(TokenType.WORD)) != null)
            {
                thisClass = new ModuleNode(name.GetValue());
                if (handler.MatchAndRemove(TokenType.IMPLEMENTS) != null)
                {
                    Token? otherName;
                    if ((otherName = handler.MatchAndRemove(TokenType.WORD)) != null)
                    {
                        ModuleNode? otherClass = program.GetFromModules(otherName.GetValue());
                        if (otherClass != null)
                        {
                            foreach (var function in otherClass.Functions)
                            {
                                thisClass.addFunction(function.Value);
                            }
                            //TODO: Figure out if this is correct/how to get variables from an implemented class
                            foreach (var variable in otherClass.GlobalVariables)
                            {
                                var temp = new List<VariableNode>();
                                temp.Add(variable.Value);
                                thisClass.AddToGlobalVariables(temp);
                            }
                        }

                        throw new Exception(
                            "Could not find class of name: " + otherName.GetValue()
                        );
                    }

                    throw new Exception("No name provided for implemented class");
                }

                return true;
            }

            throw new Exception("No name provided for class");
        }

        return false;
    }

    //TODO: double-check the work here
    private bool ParseFunction()
    {
        FunctionNode functionNode;
        Token? function;
        //TODO: make sure private token is correct/added to lexer
        bool isPublic = handler.MatchAndRemove(TokenType.PRIVATE) == null;
        if (isPublic && handler.MatchAndRemove(TokenType.SHARED) != null)
        {
            //TODO:Yeah I have no idea where to start with this since Shank has no static types or functions (that I could find)
        }
        if ((function = handler.MatchAndRemove(TokenType.FUNCTION)) != null)
        {
            functionNode = new FunctionNode(function.GetValue(), thisClass.Name, isPublic);
            thisClass.addFunction(functionNode);
            if (handler.MatchAndRemove(TokenType.OPENPARENTHESIS) != null)
            {
                functionNode.LocalVariables = ParseParameters();
                if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) == null)
                    throw new Exception("Function declaration missing end parenthesis");
            }
            else
            {
                throw new Exception("Function declaration missing open parenthesis");
            }
            if (handler.MatchAndRemove(TokenType.COLON) != null)
            {
                functionNode.LocalVariables.AddRange(ParseParameters());
            }
            functionNode.Statements = ParseBlock();
            return true;
        }

        return false;
    }

    private List<VariableNode> ParseParameters()
    {
        var parameters = new List<VariableNode>();
        var variable = new VariableNode();
        Token? name;

        do
        {
            AcceptSeparators();
            if (handler.MatchAndRemove(TokenType.STRING) != null)
            {
                variable.Type = VariableNode.DataType.String;
            }
            else if (handler.MatchAndRemove(TokenType.NUMBER) != null)
            {
                variable.Type = VariableNode.DataType.Real;
            }
            if ((name = handler.MatchAndRemove(TokenType.WORD)) != null)
            {
                variable.Name = name.GetValue();
                parameters.Add(variable);
                continue;
            }
            throw new Exception("No name provided for variable in parameters");
        } while (handler.MatchAndRemove(TokenType.COMMA) != null);
        return parameters;
    }

    //TODO: check for other statement types
    private ASTNode? ParseStatement()
    {
        var statement = ParseIf();
        if (statement != null)
        {
            return statement;
        }

        statement = ParseLoop();
        if (statement != null)
        {
            return statement;
        }
        statement = ParseReturn();
        if (statement != null)
        {
            return statement;
        }

        statement = ParseExpression();
        return statement;
    }

    private ASTNode? ParseReturn()
    {
        throw new NotImplementedException();
    }

    private ASTNode? ParseLoop()
    {
        throw new NotImplementedException();
    }

    private ASTNode? ParseIf()
    {
        throw new NotImplementedException();
    }

    //TODO: finish implementing ParseBlock()
    private List<StatementNode> ParseBlock()
    {
        throw new NotImplementedException();
    }

    private ASTNode? ParseExpression()
    {
        var lt = ParseTerm();
        if (lt == null)
            return null;
        return ParseExpressionRhs(lt);
    }

    private ASTNode? ParseExpressionRhs(ASTNode lt)
    {
        if (handler.MatchAndRemove(TokenType.PLUS) != null)
        {
            var rt = ParseTerm();
            if (rt == null)
            {
                throw new Exception("Term expected after.");
            }

            lt = new MathOpNode(lt, MathOpNode.MathOpType.plus, rt);
            return ParseExpressionRhs(lt);
        }
        else if (handler.MatchAndRemove(TokenType.MINUS) != null)
        {
            var rt = ParseTerm();
            if (rt == null)
            {
                throw new Exception("Term expected after.");
            }

            lt = new MathOpNode(lt, MathOpNode.MathOpType.minus, rt);
            return ParseExpressionRhs(lt);
        }
        else if (handler.MatchAndRemove(TokenType.LESSEQUAL) != null)
        {
            var rt = ParseTerm();
            if (rt == null)
            {
                throw new Exception("Term expected after.");
            }
            return new BooleanExpressionNode(
                lt,
                BooleanExpressionNode.BooleanExpressionOpType.le,
                rt
            );
        }
        else if (handler.MatchAndRemove(TokenType.LESSTHAN) != null)
        {
            var rt = ParseTerm();
            if (rt == null)
            {
                throw new Exception("Term expected after.");
            }
            return new BooleanExpressionNode(
                lt,
                BooleanExpressionNode.BooleanExpressionOpType.lt,
                rt
            );
        }
        else if (handler.MatchAndRemove(TokenType.GREATEREQUAL) != null)
        {
            var rt = ParseTerm();
            if (rt == null)
            {
                throw new Exception("Term expected after.");
            }
            return new BooleanExpressionNode(
                lt,
                BooleanExpressionNode.BooleanExpressionOpType.ge,
                rt
            );
        }
        else if (handler.MatchAndRemove(TokenType.GREATERTHAN) != null)
        {
            var rt = ParseTerm();
            if (rt == null)
            {
                throw new Exception("Term expected after.");
            }
            return new BooleanExpressionNode(
                lt,
                BooleanExpressionNode.BooleanExpressionOpType.gt,
                rt
            );
        }
        else if (handler.MatchAndRemove(TokenType.EQUALS) != null)
        {
            var rt = ParseTerm();
            if (rt == null)
            {
                throw new Exception("Term expected after.");
            }
            return new BooleanExpressionNode(
                lt,
                BooleanExpressionNode.BooleanExpressionOpType.eq,
                rt
            );
        }
        else if (handler.MatchAndRemove(TokenType.NOTEQUAL) != null)
        {
            var rt = ParseTerm();
            if (rt == null)
            {
                throw new Exception("Term expected after.");
            }
            return new BooleanExpressionNode(
                lt,
                BooleanExpressionNode.BooleanExpressionOpType.ne,
                rt
            );
        }
        else
        {
            return lt;
        }
    }

    private ASTNode? ParseTerm()
    {
        var lt = ParseFactor();
        if (lt == null)
            return null;
        return ParseTermRhs(lt);
    }

    private ASTNode? ParseTermRhs(ASTNode lt)
    {
        if (handler.MatchAndRemove(TokenType.MULTIPLY) != null)
        {
            var rt = ParseFactor();
            if (rt == null)
            {
                throw new Exception("Factor expected after.");
            }

            lt = new MathOpNode(lt, MathOpNode.MathOpType.times, rt);
            return ParseTermRhs(lt);
        }
        else if (handler.MatchAndRemove(TokenType.DIVIDE) != null)
        {
            var rt = ParseFactor();
            if (rt == null)
            {
                throw new Exception("Factor expected after.");
            }

            lt = new MathOpNode(lt, MathOpNode.MathOpType.divide, rt);
            return ParseTermRhs(lt);
        }
        else if (handler.MatchAndRemove(TokenType.MODULUS) != null)
        {
            var rt = ParseFactor();
            if (rt == null)
            {
                throw new Exception("Factor expected after.");
            }
            lt = new MathOpNode(lt, MathOpNode.MathOpType.modulo, rt);
            return ParseTermRhs(lt);
        }
        else
        {
            return lt;
        }
    }

    private ASTNode? ParseFactor()
    {
        if (handler.MatchAndRemove(TokenType.OPENPARENTHESIS) != null)
        {
            var exp = ParseExpression();
            if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) == null)
                throw new Exception("Closing parenthesis expected after.");
            return exp;
        }
        if (handler.MatchAndRemove(TokenType.TRUE) is { })
            return new BoolNode(true);
        if (handler.MatchAndRemove(TokenType.FALSE) is { })
            return new BoolNode(false);

        var token = handler.MatchAndRemove(TokenType.NUMBER);
        if (token == null)
            return null;
        if (token.GetValue().Contains('.'))
        {
            return new FloatNode(float.Parse(token.GetValue()));
        }
        return new IntNode(int.Parse(token.GetValue()));
    }

    public static VariableNode.DataType GetDataTypeFromConstantNodeType(ASTNode constantNode) =>
        constantNode switch
        {
            IntNode => VariableNode.DataType.Integer,
            StringNode => VariableNode.DataType.String,
            FloatNode => VariableNode.DataType.Real,
            BooleanExpressionNode or BoolNode => VariableNode.DataType.Boolean,
            _
                => throw new InvalidOperationException(
                    "Bad constant node type for converting to data type."
                )
        };

    private static VariableNode.DataType GetDataTypeFromTokenType(TokenType tt) =>
        tt switch
        {
            TokenType.NUMBER => VariableNode.DataType.Real,
            TokenType.BOOLEAN => VariableNode.DataType.Boolean,
            TokenType.STRING => VariableNode.DataType.String,
            _ => throw new InvalidOperationException("Bad TokenType for conversion into DataType"),
        };

    private TypeUsage GetTypeUsageFromToken(Token t) =>
        t.GetTokenType() switch
        {
            TokenType.NUMBER => new TypeUsage(VariableNode.DataType.Real),
            TokenType.BOOLEAN => new TypeUsage(VariableNode.DataType.Boolean),
            TokenType.STRING => new TypeUsage(VariableNode.DataType.String),
            _
                => throw new NotImplementedException(
                    "Bad TokenType for generating a TypeUsage: " + t.GetTokenType()
                )
        };

    private VariableNode? ParseVariableTypes()
    {
        var variableName = new VariableNode();
        Token? tokenType =
            handler.MatchAndRemove(TokenType.NUMBER)
            ?? handler.MatchAndRemove(TokenType.STRING)
            ?? handler.MatchAndRemove(TokenType.BOOLEAN);
        if (tokenType == null)
        {
            return null;
        }
        var variableType = GetTypeUsageFromToken(tokenType);
        Token? nameToken = handler.MatchAndRemove(TokenType.WORD);
        if (nameToken == null)
        {
            throw new Exception("Nothing followed after variable declaration");
        }
        variableName.Name = nameToken.GetValue();
        return variableName;
    }
}
