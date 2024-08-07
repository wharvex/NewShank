using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Shank.ASTNodes;

namespace Shank.Tran;

public class Parser
{
    private List<List<Token>> files = new List<List<Token>>();
    private TokenHandler handler;
    private static ProgramNode program;
    public ModuleNode thisClass;
    private LinkedList<string> sharedNames;
    public List<VariableDeclarationNode> members;
    private int blockLevel;
    private FunctionNode currentFunction;
    private int tempVarNum;
    private List<StatementNode> statements;

    public Parser(List<Token> tokens)
    {
        handler = new TokenHandler(tokens);
        program = new ProgramNode();
        sharedNames = new LinkedList<string>();
        members = [];
        blockLevel = 0;
        currentFunction = new FunctionNode("default", "default", false);
        thisClass = new ModuleNode("default");
        tempVarNum = 0;
        statements = new List<StatementNode>();
    }

    public Parser(List<List<Token>> tokens)
    {
        files = tokens;
        program = new ProgramNode();
        sharedNames = new LinkedList<string>();
        members = [];
        blockLevel = 0;
        currentFunction = new FunctionNode("default", "default", false);
        thisClass = new ModuleNode("default");
        tempVarNum = 0;
        statements = new List<StatementNode>();
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
        for (int i = 0; i < files.Count; i++)
        {
            handler = new TokenHandler(files[i]);
            AcceptSeparators();
            if (!ParseInterface() && !ParseClass())
            {
                throw new Exception("No class declaration found in file");
            }

            AcceptSeparators();
            while (handler.MoreTokens())
            {
                AcceptSeparators();

                if (ParseField() || ParseFunction())
                {
                    AcceptSeparators();
                    continue;
                }
                throw new Exception("Statement is not a function or field");
            }
            thisClass.ExportTargetNames = sharedNames;
            thisClass.UpdateExports();

            RecordNode record = new RecordNode("this", thisClass.Name, members, null);
            thisClass.AddRecord(record);
            program.AddToModules(thisClass);

            var recordParam = new VariableDeclarationNode(
                false,
                new UnknownType("this"),
                record.Name,
                thisClass.Name,
                false
            );

            //foreach (FunctionNode function in thisClass.Functions.Values)
            //{
            //    function.ParameterVariables.Add(recordParam);
            //    //function.VariablesInScope.Add(recordParam.Name, recordParam);
            //}
            blockLevel--;
        }
        return program;
    }

    public bool ParseField()
    {
        var variable = ParseVariableDeclaration();
        if (variable != null)
        {
            members.Add(variable);
            AcceptSeparators();
            var property = ParseProperty(TokenType.ACCESSOR, variable);
            if (property != null)
            {
                thisClass.addFunction(property);
            }

            AcceptSeparators();
            property = ParseProperty(TokenType.MUTATOR, variable);
            if (property != null)
            {
                thisClass.addFunction(property);
            }

            return true;
        }

        return false;
    }

    public FunctionNode? ParseProperty(TokenType propertyType, VariableDeclarationNode variable)
    {
        if (
            handler.MatchAndRemove(propertyType) != null
            && handler.MatchAndRemove(TokenType.COLON) != null
        )
        {
            var property = new FunctionNode(
                "_" + variable.Name + "_" + propertyType.ToString().ToLower(),
                thisClass.Name,
                true
            );

            currentFunction = property;

            blockLevel++;
            property.Statements = ParseBlock();
            blockLevel--;

            var value = new VariableDeclarationNode(
                false,
                variable.Type,
                "value",
                thisClass.Name,
                false
            );
            if (propertyType == TokenType.ACCESSOR)
            {
                property.LocalVariables.Add(value);
                property.VariablesInScope.Add(value.Name!, value);
                var retVal = new VariableDeclarationNode(
                    false,
                    variable.Type,
                    "retVal",
                    thisClass.Name,
                    false
                );
                property.ParameterVariables.Add(retVal);
            }
            else
            {
                value.IsConstant = true;
                property.ParameterVariables.Add(value);
                //property.VariablesInScope.Add(value.Name!, value);
            }

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
                program.AddToModules(thisClass);
                //RecordNode? record = new RecordNode("interface"+thisClass.Name, thisClass.Name, members, null);
                RecordNode? record = new RecordNode(
                    thisClass.Name,
                    "interface" + thisClass.Name,
                    members,
                    null
                );
                //RecordNode? record = new RecordNode(thisClass.Name, thisClass.Name, members, null);
                thisClass.AddRecord(record);
                if (ParseInterfaceFunctions() == false)
                {
                    throw new Exception("Nothing enclosed within the interface");
                }

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
        var isPublic = handler.MatchAndRemove(TokenType.PRIVATE) == null;
        var isShared = (isPublic && handler.MatchAndRemove(TokenType.SHARED) != null);

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
                            //program.AddToModules(otherClass);
                            RecordNode? record = otherClass.Records[otherName.GetValue()];
                            if (record != null)
                            {
                                thisClass.AddRecord(record);
                                return true;
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
        Token? function;
        //Console.WriteLine("here");
        List<VariableDeclarationNode> parameters;
        var isPublic = handler.MatchAndRemove(TokenType.PRIVATE) == null;
        var isShared = (isPublic && handler.MatchAndRemove(TokenType.SHARED) != null);

        if ((function = handler.MatchAndRemove(TokenType.FUNCTION)) != null)
        {
            currentFunction = new FunctionNode(function.GetValue(), thisClass.Name, isPublic);

            thisClass.addFunction(currentFunction);
            if (isShared)
            {
                sharedNames.AddLast(function.GetValue());
            }

            if (handler.MatchAndRemove(TokenType.OPENPARENTHESIS) != null)
            {
                parameters = ParseParameters(true);
                if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) == null)
                    throw new Exception("Function declaration missing end parenthesis");
            }
            else
            {
                throw new Exception("Function declaration missing open parenthesis");
            }

            if (handler.MatchAndRemove(TokenType.COLON) != null)
            {
                parameters.AddRange(ParseParameters(false));
            }

            currentFunction.ParameterVariables = parameters;
            foreach (var parameter in currentFunction.ParameterVariables)
            {
                currentFunction.VariablesInScope.Add(parameter.Name, parameter);
            }

            currentFunction.Statements.AddRange(ParseBlock());
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
        ExpressionNode? expression = ParseExpression();
        if (expression == null)
            return arguments;
        arguments.Add(expression);
        while (handler.MatchAndRemove(TokenType.COMMA) != null)
        {
            AcceptSeparators();
            if ((expression = ParseExpression()) != null)
            {
                arguments.Add(expression);
                continue;
            }

            throw new Exception("No expression provided in function call arguments");
        }

        return arguments;
    }

    //TODO: finish implementing ParseVariableReference()
    public VariableUsagePlainNode? ParseVariableReference()
    {
        var wordToken = handler.MatchAndRemove(TokenType.WORD);
        if (wordToken != null)
        {
            var variableRef = new VariableUsagePlainNode(wordToken.GetValue(), thisClass.Name);
            if (currentFunction.VariablesInScope.ContainsKey(wordToken.GetValue()))
            {
                var variable = currentFunction.VariablesInScope[wordToken.GetValue()];
                if (variable.IsConstant)
                {
                    variableRef.NewIsInFuncCallWithVar = true;
                    variableRef.IsInFuncCallWithVar = true;
                }
            }

            return variableRef;
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
                    return new AssignmentNode(variable, expression);
                }
                else
                    throw new Exception("Missing expression on right side of assignment");
            }
            else if (handler.MatchAndRemove(TokenType.COMMA) != null)
            {
                variable.NewIsInFuncCallWithVar = true;
                variable.IsInFuncCallWithVar = true;
                List<VariableUsagePlainNode> variables = [variable];
                do
                {
                    AcceptSeparators();
                    variable = ParseVariableReference();
                    if (variable != null)
                    {
                        variable.NewIsInFuncCallWithVar = true;
                        variable.IsInFuncCallWithVar = true;
                        variables.Add(variable);
                    }
                    else
                        throw new Exception(
                            "Missing variable reference after comma in multi-assignment statement"
                        );
                } while (handler.MatchAndRemove(TokenType.COMMA) != null);

                if (handler.MatchAndRemove(TokenType.EQUAL) != null)
                {
                    var functionCall = (FunctionCallNode?)ParseFunctionCall();
                    if (functionCall != null)
                    {
                        functionCall.Arguments.AddRange(variables);
                        return functionCall;
                    }
                    else
                        throw new Exception(
                            "Multi-assignment requires a function call as the target"
                        );
                }
                else
                    throw new Exception("Missing equal sign after multi-assignment");
            }
            else
                throw new Exception(
                    "Missing either comma or equal sign after variable reference for assignment"
                );
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

    public string? ParseBuiltInFunctionNode()
    {
        if (handler.MatchAndRemove(TokenType.PERIOD) != null)
        {
            if (handler.MatchAndRemove(TokenType.TIMES) != null)
            {
                if (handler.MatchAndRemove(TokenType.OPENPARENTHESIS) != null)
                {
                    if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) != null)
                    {
                        var variableDec = ParseVariableDeclaration();

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
                    return "write";
                }
                else
                {
                    throw new Exception(
                        "In ParseBuiltInFunctionNode method, Expected a print after period"
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
        statements = [];
        while (handler.MatchAndRemove(TokenType.NEWLINE) != null)
        {
            AcceptNewlines();
            currentLevel = 0;
            while (handler.MatchAndRemove(TokenType.TAB) != null)
            {
                currentLevel++;
            }

            if (currentLevel != blockLevel)
            {
                break;
            }

            var statement = ParseStatement();

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
                    currentFunction.VariablesInScope.Add(variableDec.Name, variableDec);
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
        var functionName = ParseBuiltInFunctionNode();

        if (functionToken != null)
        {
            functionName = functionToken.GetValue();
        }
        else if (functionName != null) { }
        else
        {
            return null;
        }

        FunctionCallNode functionCallNode = new FunctionCallNode(functionName);

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

        var variable = ParseVariableReference();
        if (variable != null)
        {
            return variable;
        }

        var functionCall = (FunctionCallNode)ParseFunctionCall()!;
        if (functionCall != null)
        {
            //Declare the temp variable to hold the return value
            var tempVar = new VariableDeclarationNode(
                false,
                new UnknownType(),
                "_temp_" + tempVarNum,
                thisClass.Name,
                false
            );
            currentFunction.VariablesInScope.Add(tempVar.Name, tempVar);
            currentFunction.LocalVariables.Add(tempVar);

            //Insert statement to call the function
            var varRef = new VariableUsagePlainNode(tempVar.Name, thisClass.Name);
            varRef.IsInFuncCallWithVar = true;
            varRef.NewIsInFuncCallWithVar = true;
            varRef.Type = new UnknownType();
            functionCall.Arguments.Add(varRef);
            statements.Add(functionCall);

            tempVarNum++;
            return varRef;
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

    private bool ParseInterfaceFunctions()
    {
        while (handler.MoreTokens())
        {
            int currentLevel;
            while (handler.MatchAndRemove(TokenType.NEWLINE) != null)
            {
                AcceptNewlines();
                currentLevel = 0;
                while (handler.MatchAndRemove(TokenType.TAB) != null)
                {
                    blockLevel++;
                    currentLevel++;
                }

                if (currentLevel != blockLevel)
                {
                    break;
                }
            }

            FunctionNode functionNode;
            Token? function;
            List<VariableDeclarationNode> parameters;
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
                    parameters = ParseParameters(true);
                    if (handler.MatchAndRemove(TokenType.CLOSEDPARENTHESIS) == null)
                        throw new Exception("Function declaration missing end parenthesis");
                }
                else
                {
                    throw new Exception("Function declaration missing open parenthesis");
                }

                if (handler.MatchAndRemove(TokenType.COLON) != null)
                {
                    parameters.AddRange(ParseParameters(false));
                }

                functionNode.ParameterVariables = parameters;
                foreach (var parameter in functionNode.ParameterVariables)
                {
                    if (parameter.Name != null)
                        functionNode.VariablesInScope.Add(parameter.Name, parameter);
                }

                currentFunction = functionNode;
                blockLevel--;
                return true;
            }
            else
            {
                blockLevel--;
                return false;
            }
        }

        blockLevel--;
        return false;
    }
}
