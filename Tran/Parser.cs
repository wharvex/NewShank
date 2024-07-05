using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Shank.ASTNodes;

namespace Shank.Tran;

public class Parser
{
    private TokenHandler handler;
    private ProgramNode program;
    public ModuleNode thisClass;
    private LinkedList<string> sharedNames;
    public List<VariableDeclarationNode> members;
    private int blockLevel;
    private FunctionNode currentFunction;
    private bool lastLineVariableDec;

    public Parser(LinkedList<Token> tokens)
    {
        handler = new TokenHandler(tokens);
        program = new ProgramNode();
        sharedNames = new LinkedList<string>();
        members = [];
        blockLevel = 0;
        currentFunction = new FunctionNode("default");
        thisClass = new ModuleNode("default");
        lastLineVariableDec = false;
    }

    public bool AcceptSeparators()
    {
        bool retVal = false;
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
        AcceptSeparators();
        if (!ParseClass() && !ParseInterface())
        {
            throw new Exception("No class declaration found in file");
        }
        AcceptSeparators();

        while (handler.MoreTokens())
        {
            if (ParseField() || ParseFunction())
            {
                AcceptSeparators();
                continue;
            }
            throw new Exception("Statement is not a function or field");
        }
        thisClass.ExportTargetNames = sharedNames;
        thisClass.UpdateExports();

        //TODO: how do we pass the record into every function if we can't parse every field before parsing functions (e.g. how do we pass in the record if it is incomplete?)
        //Loop through functions and add this to the parameters
        //Test object-oriented stuff
        //Interfaces after were done
        //Semantic Analysis: check scope of variables (local, member, shared)
        //Built-in functions should only be in interpreter, probably remove all the parser stuff
        //Function call to built-in function node
        //Append strings should be built in
        RecordNode? record = new RecordNode(thisClass.Name, thisClass.Name, members, null);
        thisClass.AddRecord(record);
        program.AddToModules(thisClass);

        foreach (var function in thisClass.Functions)
        {
            foreach (var member in record.Members)
            {
                ((FunctionNode)function.Value).LocalVariables.Add(
                    new VariableDeclarationNode(
                        false,
                        member.Type,
                        member.Name,
                        thisClass.Name,
                        false
                    )
                );
            }
        }
        return program;
    }

    public bool ParseField()
    {
        var variable = ParseVariableDeclaration();
        if (variable != null || lastLineVariableDec)
        {
            var property = ParseProperty(TokenType.ACCESSOR, members.Last().Name);
            if (property != null)
            {
                thisClass.addFunction(property);
            }

            property = ParseProperty(TokenType.MUTATOR, members.Last().Name);
            if (property != null)
            {
                thisClass.addFunction(property);
            }

            lastLineVariableDec = false;
            return true;
        }

        return false;
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
            currentFunction = property;
            property.Statements = ParseBlock();
            var retVal = new VariableDeclarationNode { IsConstant = true, Name = "value" };
            property.ParameterVariables.Add(retVal);
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
    public bool ParseClass()
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
                                thisClass.AddRecord(record);
                            }
                        }
                        throw new Exception(
                            "Could not find class of name: " + otherName.GetValue()
                        );
                    }

                    throw new Exception("No name provided for implemented class");
                }

                blockLevel++;
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
                var parameters = ParseParameters(true);
                functionNode.ParameterVariables.AddRange(parameters);
                if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) == null)
                    throw new Exception("Function declaration missing end parenthesis");
            }
            else
            {
                throw new Exception("Function declaration missing open parenthesis");
            }

            if (handler.MatchAndRemove(TokenType.COLON) != null)
            {
                functionNode.ParameterVariables.AddRange(ParseParameters(false));
            }

            currentFunction = functionNode;
            functionNode.Statements = ParseBlock();
            return true;
        }

        return false;
    }

    public List<VariableDeclarationNode> ParseParameters(bool isConstant)
    {
        var parameters = new List<VariableDeclarationNode>();
        var variable = ParseVariableDeclaration();
        if (variable == null)
        {
            return [];
        }
        variable.IsConstant = isConstant;
        parameters.Add(variable);
        while (handler.MatchAndRemove(TokenType.COMMA) != null)
        {
            AcceptSeparators();
            if ((variable = ParseVariableDeclaration()) != null)
            {
                variable.IsConstant = isConstant;
                parameters.Add(variable);
                continue;
            }
            throw new Exception("No name provided for variable in parameters");
        }
        return parameters;
    }

    public List<ExpressionNode> ParseArguments()
    {
        var arguments = new List<ExpressionNode>();
        ExpressionNode? expression;
        do
        {
            AcceptSeparators();
            if ((expression = ParseExpression()) != null)
            {
                arguments.Add(expression);
                continue;
            }
            throw new Exception("No name provided for variable in parameters");
        } while (handler.MatchAndRemove(TokenType.COMMA) != null);
        return arguments;
    }

    //TODO: finish implementing ParseVariableReference()
    public VariableUsagePlainNode? ParseVariableReference()
    {
        var wordToken = handler.MatchAndRemove(TokenType.WORD);
        if (wordToken != null)
        {
            //TODO: wat?
            //if (handler.MatchAndRemove(TokenType.PERIOD) != null)
            //{
            //    VariableUsagePlainNode? variableReference = ParseVariableReference();

            //    if (variableReference != null)
            //    {
            //        return new VariableUsagePlainNode(
            //            wordToken.GetValue(),
            //            variableReference,
            //            VariableUsagePlainNode.VrnExtType.RecordMember
            //        );
            //    }
            //}
            return new VariableUsagePlainNode(wordToken.GetValue(), thisClass.Name);
        }
        return null;
    }

    //TODO: check for other statement types
    public ASTNode? ParseStatement()
    {
        var statement =
            ParseIf() ?? ParseLoop() ?? ParseReturn() ?? ParseFunctionCall() ?? ParseAssignment();
        return statement;
    }

    private StatementNode? ParseAssignment()
    {
        var variable = ParseVariableReference();
        if (variable != null)
        {
            if (handler.MatchAndRemove(TokenType.EQUAL) != null)
            {
                var expression = ParseExpression();
                if (expression != null)
                {
                    //TODO: check if newTarget is something we need to worry about
                    return new AssignmentNode(variable, expression);
                }
            }
        }

        return null;
    }

    //TODO: finish implementing ParseReturn()
    public StatementNode? ParseReturn()
    {
        return null;
    }

    public StatementNode? ParseLoop()
    {
        if (handler.MatchAndRemove(TokenType.LOOP) != null)
        {
            var conditionLoop = ParseExpression() as BooleanExpressionNode;
            // var condition = ParseBuiltInFunctionNode();
            var LoopBody = ParseBlock();
            if (LoopBody == null)
            {
                throw new Exception("In ParseLoop method, the LoopBody is null");
            }
            if (conditionLoop != null)
            {
                return new WhileNode(conditionLoop, LoopBody);
            }
        }
        return null;
    }

    public StatementNode? ParseIf()
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
                    new ElseNode(elseBlock)
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

    public string ParseBuiltInFunctionNode()
    {
        if (handler.MatchAndRemove(TokenType.PERIOD) != null)
        {
            if (handler.MatchAndRemove(TokenType.TIMES) != null)
            {
                if (handler.MatchAndRemove(TokenType.OPENPARENTHESIS) != null)
                {
                    if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) != null)
                    {
                        return "Times Function";
                    }
                }
            }
        }
        if (handler.MatchAndRemove(TokenType.PERIOD) != null)
        {
            if (handler.MatchAndRemove(TokenType.CLONE) != null)
            {
                if (handler.MatchAndRemove(TokenType.OPENPARENTHESIS) != null)
                {
                    if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) != null)
                    {
                        return "Clone";
                    }
                }
            }
        }
        if (handler.MatchAndRemove(TokenType.PERIOD) != null)
        {
            if (handler.MatchAndRemove(TokenType.GETDATE) != null)
            {
                if (handler.MatchAndRemove(TokenType.OPENPARENTHESIS) != null)
                {
                    if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) != null)
                    {
                        return "GetDate Function";
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
                        if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) != null)
                        {
                            return "String";
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
        return null;
    }

    public List<StatementNode> ParseBlock()
    {
        blockLevel++;
        int currentLevel;
        var statements = new List<StatementNode>();
        while (handler.MatchAndRemove(TokenType.NEWLINE) != null)
        {
            AcceptNewlines();
            currentLevel = 0;
            while (handler.MatchAndRemove(TokenType.TAB) != null)
            {
                currentLevel++;
            }
            var statement = ParseStatement();
            if (currentLevel != blockLevel)
            {
                if (statement != null)
                {
                    throw new Exception("Invalid indentation in block");
                }
                break;
            }

            if (statement != null)
            {
                statements.Add((StatementNode)statement);
            }
            else
            {
                var variableDec = ParseVariableDeclaration();
                if (variableDec != null)
                {
                    currentFunction.LocalVariables.Add(variableDec);
                }
            }
        }

        blockLevel--;
        return statements;
    }

    public void AcceptNewlines()
    {
        while (handler.Peek(0) != null && handler.Peek(0)!.GetTokenType() == TokenType.NEWLINE)
        {
            handler.MatchAndRemove(TokenType.NEWLINE);
        }
    }

    public StatementNode? ParseFunctionCall()
    {
        var functionToken = handler.MatchAndRemove(TokenType.FUNCTION);

        if (functionToken == null)
        {
            return null;
        }

        var functionName = ParseBuiltInFunctionNode() ?? functionToken.GetValue();
        FunctionCallNode functionCallNode = new FunctionCallNode(functionName);
        var functionLineNumber = functionToken.GetTokenLineNumber();

        if (handler.MatchAndRemove(TokenType.OPENPARENTHESIS) == null)
        {
            throw new InvalidOperationException(
                "Missing open parenthesis in function<" + functionName + ">"
            );
        }

        functionCallNode.Arguments.AddRange(ParseArguments());

        if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) == null)
        {
            throw new InvalidOperationException(
                "Missing closing parenthesis in function<" + functionName + ">"
            );
        }

        functionCallNode.LineNum = functionLineNumber;
        functionCallNode.Name = functionName;
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

        if (handler.MatchAndRemove(TokenType.QUOTE) != null)
        {
            string value = "";
            Token? word;
            while ((word = handler.MatchAndRemove(TokenType.WORD)) != null)
            {
                value += word.GetValue();
            }
            if (handler.MatchAndRemove(TokenType.QUOTE) != null)
            {
                return new StringNode(value);
            }
            throw new Exception("String literal missing end quotes");
        }

        VariableUsagePlainNode? variable;
        if ((variable = ParseVariableReference()) != null)
        {
            return variable;
        }
        if (handler.MatchAndRemove(TokenType.TRUE) is { })
            return new BoolNode(true);
        if (handler.MatchAndRemove(TokenType.FALSE) is { })
            return new BoolNode(false);

        var token = handler.MatchAndRemove(TokenType.NUMERAL);
        if (token == null)
            return null;
        return new FloatNode(float.Parse(token.GetValue()));
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

    public Type Type(VariableDeclarationNode.DeclarationContext declarationContext)
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

    public VariableDeclarationNode? ParseVariableDeclaration()
    {
        Type? variableType = ParseType();
        if (variableType == null)
        {
            return null;
        }
        Token? nameToken = handler.MatchAndRemove(TokenType.WORD);
        if (nameToken == null)
        {
            throw new Exception("Variable declaration missing a name");
        }
        VariableDeclarationNode variableNode = new VariableDeclarationNode(
            false,
            variableType,
            nameToken.GetValue(),
            thisClass.Name,
            false
        );
        members.Add(variableNode);
        lastLineVariableDec = true;
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
