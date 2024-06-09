using System.Diagnostics.CodeAnalysis;
using Shank.ASTNodes;

namespace Shank.Tran;

public class Parser
{
    private TokenHandler handler;
    private ProgramNode program;
    private ModuleNode thisClass;
    private LinkedList<string> sharedNames;

    public Parser(LinkedList<Token> tokens)
    {
        handler = new TokenHandler(tokens);
        program = new ProgramNode();
        sharedNames = new LinkedList<string>();
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

            if (ParseField() || ParseFunction())
            {
                AcceptSeparators();
                continue;
            }

            thisClass.ExportTargetNames = sharedNames;
            thisClass.UpdateExports();

            throw new Exception("Statement is not a function or field");
        }

        return program;
    }

    private bool ParseField()
    {
        var variable = ParseVariableDeclaration();
        bool hasField = false;
        if (variable != null)
        {
            //TODO: Can properties be added to the class as regular functions?
            //TODO: What should the name of the function be in this case?
            var property = ParseProperty(TokenType.ACCESSOR, variable.Name);
            if (property != null)
            {
                thisClass.addFunction(property);
                hasField = true;
            }

            property = ParseProperty(TokenType.MUTATOR, variable.Name);
            if (property != null)
            {
                thisClass.addFunction(property);
                hasField = true;
            }
        }
        return hasField;
    }

    private FunctionNode? ParseProperty(TokenType propertyType, String? variableName)
    {
        if (
            handler.MatchAndRemove(propertyType) != null
            && handler.MatchAndRemove(TokenType.COLON) != null
        )
        {
            var property = new FunctionNode(
                variableName + propertyType.ToString().ToLower(),
                thisClass.Name,
                true
            );
            property.Statements = ParseBlock();
            return property;
        }

        return null;
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
        var isPublic = handler.MatchAndRemove(TokenType.PRIVATE) == null;
        var isShared = (isPublic && handler.MatchAndRemove(TokenType.SHARED) != null);
        if ((function = handler.MatchAndRemove(TokenType.FUNCTION)) != null)
        {
            functionNode = new FunctionNode(function.GetValue(), thisClass.Name, isPublic);
            thisClass.addFunction(functionNode);
            if (isShared)
            {
                sharedNames.AddLast(function.GetValue());
            }
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
        do
        {
            AcceptSeparators();
            if ((variable = ParseVariableDeclaration()) != null)
            {
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
        var statement = ParseIf() ?? ParseLoop() ?? ParseReturn() ?? ParseExpression();
        return statement;
    }

    //TODO: finish implementing ParseReturn()
    private ASTNode? ParseReturn()
    {
        throw new NotImplementedException();
    }

    //TODO: finish implementing ParseLoop()
    private ASTNode? ParseLoop()
    {
        throw new NotImplementedException();
    }

    //TODO: finish implementing ParseIf()
    private ASTNode? ParseIf()
    {
        if (handler.MatchAndRemove(TokenType.IF) != null)
        {
            var condition = ParseFactor() as BooleanExpressionNode;
            var block = ParseBlock();
            AcceptSeparators();

            if (handler.MatchAndRemove(TokenType.ELSE) != null)
            {
                var nextIf = ParseIf() as IfNode;
                if (nextIf != null)
                {
                    // Return the 'IF' node with 'ELSE IF' or 'ELSE'
                    return new IfNode(
                        condition
                            ?? throw new InvalidOperationException("In ParseIf, condition is null"),
                        block,
                        nextIf
                    );
                }
                // Return the 'IF' node with 'ELSE'
                return new IfNode(condition, block, ParseBlock());
            }
            // Return just the 'IF' branch without an 'ELSE'
            return new IfNode(
                condition ?? throw new InvalidOperationException("In ParseIf, condition is null"),
                block,
                new List<StatementNode>()
            );
        }
        return null;
    }

    //TODO: finish implementing ParseBlock()
    private List<StatementNode> ParseBlock()
    {
        while (true)
        {
            ParseStatement();
        }
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

            lt = new MathOpNode(lt, MathOpNode.MathOpType.Plus, rt);
            return ParseExpressionRhs(lt);
        }
        else if (handler.MatchAndRemove(TokenType.MINUS) != null)
        {
            var rt = ParseTerm();
            if (rt == null)
            {
                throw new Exception("Term expected after.");
            }

            lt = new MathOpNode(lt, MathOpNode.MathOpType.Minus, rt);
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

            lt = new MathOpNode(lt, MathOpNode.MathOpType.Times, rt);
            return ParseTermRhs(lt);
        }
        else if (handler.MatchAndRemove(TokenType.DIVIDE) != null)
        {
            var rt = ParseFactor();
            if (rt == null)
            {
                throw new Exception("Factor expected after.");
            }

            lt = new MathOpNode(lt, MathOpNode.MathOpType.Divide, rt);
            return ParseTermRhs(lt);
        }
        else if (handler.MatchAndRemove(TokenType.MODULUS) != null)
        {
            var rt = ParseFactor();
            if (rt == null)
            {
                throw new Exception("Factor expected after.");
            }
            lt = new MathOpNode(lt, MathOpNode.MathOpType.Modulo, rt);
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

    public static IType GetDataTypeFromConstantNodeType(ASTNode constantNode) =>
        constantNode switch
        {
            IntNode => new IntegerType(),
            StringNode => new StringType(),
            CharNode => new  CharacterType(),
            FloatNode => new RealType(),
            BooleanExpressionNode or BoolNode => new BooleanType(),
            _
                => throw new InvalidOperationException(
                    "Bad constant node type for converting to data type."
                )
        };

    private static IType GetDataTypeFromTokenType(TokenType tt) =>
        tt switch
        {
            TokenType.NUMBER => new RealType(),
            TokenType.BOOLEAN => new BooleanType(),
            TokenType.CHARACTER => new CharacterType(),
            TokenType.STRING => new StringType(),
            _ => throw new InvalidOperationException("Bad TokenType for conversion into DataType"),
        };

    private IType GetTypeUsageFromToken(Token t) =>
        t.GetTokenType() switch
        {
            TokenType.NUMBER => new RealType(),
            TokenType.BOOLEAN => new BooleanType(),
            TokenType.CHARACTER => new CharacterType(),
            TokenType.STRING => new StringType(),
            _
                => throw new NotImplementedException(
                    "Bad TokenType for generating a TypeUsage: " + t.GetTokenType()
                )
        };

    private VariableNode? ParseVariableDeclaration()
    {
        var variableName = new VariableNode();
        Token? tokenType =
            handler.MatchAndRemove(TokenType.NUMBER)
            ?? handler.MatchAndRemove(TokenType.STRING)
            ?? handler.MatchAndRemove(TokenType.CHARACTER)
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
