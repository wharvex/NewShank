using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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

    public bool AcceptSeparators()
    {
        bool retVal = false;
        //while (handler.MatchAndRemove(TokenType.SEPARATOR) != null)
        while (
            (
                handler.MatchAndRemove(TokenType.SEPARATOR)
                ?? handler.MatchAndRemove(TokenType.NEWLINE)
                ?? handler.MatchAndRemove(TokenType.TAB)
            ) != null
        )
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

            List<VariableNode> members = new List<VariableNode>();
            members.AddRange(thisClass.GlobalVariables.Values.ToList());
            //TODO: how to add functions to the members list for the record?
            //members.AddRange(thisClass.Functions.Values.ToList());
            RecordNode? record = new RecordNode(thisClass.Name, thisClass.Name, members, null);
            thisClass.AddRecord(record);
            throw new Exception("Statement is not a function or field");
        }

        return program;
    }

    public bool ParseField()
    {
        var variable = ParseVariableDeclaration();
        bool hasField = false;
        if (variable != null)
        {
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

    public FunctionNode? ParseProperty(TokenType propertyType, String? variableName)
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

    public bool ParseInterface()
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

    //When checking VarRef check if its within local scope, then check the record if it exists, then check global if shared
    //Class should have a reference to the record of itself containing only variables, use NewType
    //Interfaces should use an enum inside the interface to determine which subtype to use, each implemented subclass should have enum
    //Should be a post-processing step, save until the end of parser

    //Variable Reference: ensure the correct scope is used and uses record if applicable
    //Class: Should add fields to a record node that is passed to every function to check if variable is in it
    //Interfaces: contain an enum inside the interface for subtype of class, each class has a type - do later
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
                            RecordNode? record = otherClass.Records[otherName.GetValue()];
                            if (record != null)
                            {
                                //TODO: Check with Phipps for correctness
                                foreach (var entry in record.NewType.Fields)
                                {
                                    var temp = new List<VariableNode>();
                                    temp.Add(
                                        new VariableNode { Type = entry.Value, Name = entry.Key }
                                    );
                                    thisClass.AddToGlobalVariables(temp);
                                }
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
    public bool ParseFunction()
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
                functionNode.LocalVariables = ParseArguments(false);
                if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) == null)
                    throw new Exception("Function declaration missing end parenthesis");
            }
            else
            {
                throw new Exception("Function declaration missing open parenthesis");
            }
            if (handler.MatchAndRemove(TokenType.COLON) != null)
            {
                functionNode.ParameterVariables.AddRange(ParseArguments(true));
            }
            functionNode.Statements = ParseBlock();
            return true;
        }

        return false;
    }

    public List<VariableNode> ParseArguments(bool isConstant)
    {
        var parameters = new List<VariableNode>();
        var variable = new VariableNode();
        do
        {
            AcceptSeparators();
            if ((variable = ParseVariableDeclaration()) != null)
            {
                variable.IsConstant = isConstant;
                parameters.Add(variable);
                continue;
            }
            throw new Exception("No name provided for variable in parameters");
        } while (handler.MatchAndRemove(TokenType.COMMA) != null);
        return parameters;
    }

    public List<ParameterNode> ParseParameters()
    {
        var parameters = new List<ParameterNode>();
        VariableUsagePlainNode variable;
        do
        {
            AcceptSeparators();
            if ((variable = ParseVariableReference()) != null)
            {
                parameters.Add(new ParameterNode(variable, false));
                continue;
            }
            throw new Exception("No name provided for variable in parameters");
        } while (handler.MatchAndRemove(TokenType.COMMA) != null);
        return parameters;
    }

    //TODO: finish implementing ParseVariableReference()
    public VariableUsagePlainNode ParseVariableReference()
    {
        throw new NotImplementedException();
    }

    //TODO: check for other statement types
    public ASTNode? ParseStatement()
    {
        var statement = ParseIf() ?? ParseLoop() ?? ParseReturn() ?? ParseExpression();
        return statement;
    }

    //TODO: finish implementing ParseReturn()
    public ASTNode? ParseReturn()
    {
        throw new NotImplementedException();
    }

    public ASTNode? ParseLoop()
    {
        if (handler.MatchAndRemove(TokenType.LOOP) != null)
        {
            var conditionLoop = ParseExpression() as BooleanExpressionNode;

            if (conditionLoop == null)
            {
                throw new Exception("In ParseLoop method, the condition is null");
            }
            var LoopBody = ParseBlock();

            if (LoopBody == null)
            {
                throw new Exception("In ParseLoop method, the LoopBody is null");
            }
            return new WhileNode(conditionLoop, LoopBody);
        }
        return null;
    }

    public ASTNode? ParseIf()
    {
        if (handler.MatchAndRemove(TokenType.IF) != null)
        {
            var condition = ParseExpression() as BooleanExpressionNode;
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
                var elseBlock = ParseBlock();
                // return new IfNode(condition, block, new IfNode(elseBlock));
                return new IfNode(
                    condition
                        ?? throw new InvalidOperationException("In ParseIf, condition is null"),
                    block,
                    new IfNode(null, elseBlock, null)
                );
            }
            // Return just the 'IF' branch without an 'ELSE'
            return new IfNode(
                condition ?? throw new InvalidOperationException("In ParseIf, condition is null"),
                block,
                null
            );
        }
        return null;
    }

    public LinkedList<ASTNode> ParseBuiltInFunctionNode()
    {
        LinkedList<ASTNode> expressions = new LinkedList<ASTNode>();

        if (handler.MatchAndRemove(TokenType.PERIOD) != null)
        {
            if (handler.MatchAndRemove(TokenType.TIMES) != null)
            {
                if (handler.MatchAndRemove(TokenType.OPENPARENTHESIS) != null)
                {
                    if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) == null)
                    {
                        throw new Exception(
                            "In ParseBuiltInFunctionNode method, Expected a closing parenthesis after .Times( "
                        );
                    }
                }
            }
        }
        if (handler.MatchAndRemove(TokenType.CONSOLE) != null)
        {
            if (handler.MatchAndRemove(TokenType.PERIOD) != null)
            {
                if (handler.MatchAndRemove(TokenType.PRINT) != null)
                {
                    if (handler.MatchAndRemove(TokenType.OPENPARENTHESIS) != null)
                    {
                        do
                        {
                            expressions.AddLast(ParseExpression());
                        } while (handler.MatchAndRemove(TokenType.COMMA) != null);

                        if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) == null)
                        {
                            throw new Exception(
                                "In ParseBuiltInFunctionNode method, Expected a closing parenthesis"
                            );
                        }
                    }
                    else
                    {
                        throw new Exception(
                            "In ParseBuiltInFunctionNode method, Expected an opening parenthesis"
                        );
                    }
                }
                else
                {
                    throw new Exception(
                        "In ParseBuiltInFunctionNode method, Expected 'print' after 'console.'"
                    );
                }
            }
            else
            {
                throw new Exception(
                    "In ParseBuiltInFunctionNode method, Expected a period after 'console'"
                );
            }
        }
        return expressions;
    }

    public List<StatementNode> ParseBlock()
    {
        List<StatementNode> blocks = new List<StatementNode>();
        AcceptSeparators();
        while (handler.MatchAndRemove(TokenType.FUNCTIONBLOCKIDENTIFIER) != null)
        {
            if (handler.MatchAndRemove(TokenType.TAB) != null)
            {
                throw new Exception("Invalid indentation from ParseBlock");
            }
            AcceptNewline();
            ASTNode? statement = ParseStatement();
            AcceptNewline();
            if (statement != null)
            {
                blocks.Add((StatementNode)statement);
            }
        }
        return blocks;
    }

    public void AcceptNewline()
    {
        while (handler.Peek(0) != null && handler.Peek(0).GetTokenType() == TokenType.NEWLINE)
        {
            handler.MatchAndRemove(TokenType.NEWLINE);
        }
    }

    public ASTNode? ParseFunctionCall()
    {
        AcceptSeparators();
        var functionToken = handler.MatchAndRemove(TokenType.FUNCTION);

        if (functionToken == null)
        {
            return null;
        }

        var functionName = functionToken.GetValue();
        var functionLineNumber = functionToken.GetTokenLineNumber();

        if (handler.MatchAndRemove(TokenType.OPENPARENTHESIS) == null)
        {
            throw new InvalidOperationException(
                "Missing open parenthesis in function<" + functionName + ">"
            );
        }

        List<ParameterNode> parameters = ParseParameters();

        if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) == null)
        {
            throw new InvalidOperationException(
                "Missing closing parenthesis in function<" + functionName + ">"
            );
        }

        FunctionCallNode functionCallNode = new FunctionCallNode(functionName);
        functionCallNode.Parameters.AddRange(parameters);
        functionCallNode.LineNum = functionLineNumber;
        return functionCallNode;
    }

    public ExpressionNode? ParseExpression()
    {
        var lt = ParseTerm();
        if (lt == null)
            return null;
        return ParseExpressionRhs(lt);
    }

    public ExpressionNode? ParseExpressionRhs(ExpressionNode lt)
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

    public ExpressionNode? ParseTerm()
    {
        var lt = ParseFactor();
        if (lt == null)
            return null;
        return ParseTermRhs(lt);
    }

    public ExpressionNode? ParseTermRhs(ExpressionNode lt)
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

    public ExpressionNode? ParseFactor()
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

    public static Type GetDataTypeFromConstantNodeType(ASTNode constantNode) =>
        constantNode switch
        {
            StringNode => new StringType(),
            CharNode => new CharacterType(),
            FloatNode => new RealType(),
            BooleanExpressionNode or BoolNode => new BooleanType(),
            _
                => throw new InvalidOperationException(
                    "Bad constant node type for converting to data type."
                )
        };

    public static Type GetDataTypeFromTokenType(TokenType tt) =>
        tt switch
        {
            TokenType.NUMBER => new RealType(),
            TokenType.BOOLEAN => new BooleanType(),
            TokenType.CHARACTER => new CharacterType(),
            TokenType.STRING => new StringType(),
            _ => throw new InvalidOperationException("Bad TokenType for conversion into DataType"),
        };

    public Type GetTypeUsageFromToken(Token t) =>
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

    public Type Type(VariableNode.DeclarationContext declarationContext)
    {
        Token? tokenType =
            handler.MatchAndRemove(TokenType.NUMBER)
            ?? handler.MatchAndRemove(TokenType.STRING)
            ?? handler.MatchAndRemove(TokenType.CHARACTER)
            ?? handler.MatchAndRemove(TokenType.BOOLEAN);
        if (tokenType == null)
        {
            throw new Exception("Expected start of a type");
        }

        return GetTypeUsageFromToken(tokenType);
    }

    public VariableNode? ParseVariableDeclaration()
    {
        Type variableType = ParseType();
        Token? nameToken = handler.MatchAndRemove(TokenType.WORD);
        if (nameToken == null)
        {
            throw new Exception("Variable declaration missing a name");
        }
        VariableNode variableNode = new VariableNode
        {
            Type = variableType,
            Name = nameToken.GetValue()
        };
        return variableNode;
    }

    private Type? ParseType()
    {
        Token? tokenType =
            handler.MatchAndRemove(TokenType.NUMBER)
            ?? handler.MatchAndRemove(TokenType.STRING)
            ?? handler.MatchAndRemove(TokenType.CHARACTER)
            ?? handler.MatchAndRemove(TokenType.BOOLEAN);
        if (tokenType == null)
        {
            return null;
        }

        return GetTypeUsageFromToken(tokenType);
    }
}
