using LLVMSharp;

namespace Shank;

public class Parser
{
    private readonly Token.TokenType[] _shankTokenTypes =
    {
        Token.TokenType.Integer,
        Token.TokenType.Real,
        Token.TokenType.Boolean,
        Token.TokenType.Character,
        Token.TokenType.String,
        Token.TokenType.Array
    };

    private readonly Token.TokenType[] _shankTokenTypesPlusIdentifier =
    {
        Token.TokenType.Integer,
        Token.TokenType.Real,
        Token.TokenType.Boolean,
        Token.TokenType.Character,
        Token.TokenType.String,
        Token.TokenType.Array,
        Token.TokenType.Identifier
    };

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    private readonly List<Token> _tokens;

    private Token? MatchAndRemove(Token.TokenType t)
    {
        // If there are no Tokens left, return null.
        if (!_tokens.Any())
        {
            return null;
        }

        // Get the next Token.
        var retVal = _tokens[0];

        // If the TokenType does not match, return null.
        if (retVal.Type != t)
        {
            return null;
        }

        // Remove the Token.
        _tokens.RemoveAt(0);

        // Consume blank lines logic.
        // We don't want MatchAndRemove to be the permanent home of this logic because we want to
        // keep MAR simple. The eventual permanent fix will be to convert every
        // "MatchAndRemove(EndOfLine)" call in the whole project to an "ExpectsEndOfLine()" or a
        // "RequiresEndOfLine()" call (this will be a big undertaking).
        if (t == Token.TokenType.EndOfLine)
        {
            ConsumeBlankLines();
        }

        // Return the Token.
        return retVal;
    }

    private Token? MatchAndRemoveMultiple(params Token.TokenType[] t)
    {
        // If there are no Tokens left, return null.
        if (!_tokens.Any())
        {
            return null;
        }

        // Get the next Token.
        var retVal = _tokens[0];

        // If the TokenType does not match, return null.
        if (!t.Contains(retVal.Type))
        {
            return null;
        }

        // Remove the Token.
        _tokens.RemoveAt(0);

        // Consume blank lines logic.
        // We don't want MatchAndRemove to be the permanent home of this logic because we want to
        // keep MAR simple. The eventual permanent fix will be to convert every
        // "MatchAndRemove(EndOfLine)" call in the whole project to an "ExpectsEndOfLine()" or a
        // "RequiresEndOfLine()" call (this will be a big undertaking).
        if (retVal.Type == Token.TokenType.EndOfLine)
        {
            ConsumeBlankLines();
        }

        // Return the Token.
        return retVal;
    }

    private Token? Peek(int offset)
    {
        return _tokens.Count > offset ? _tokens[offset] : null;
    }

    private bool ExpectsEndOfLine()
    {
        var ret = MatchAndRemove(Token.TokenType.EndOfLine) is not null;

        if (ret)
        {
            ConsumeBlankLines();
        }

        return ret;
    }

    private void ConsumeBlankLines()
    {
        while (MatchAndRemove(Token.TokenType.EndOfLine) is not null) { }
    }

    private void RequiresEndOfLine()
    {
        if (!ExpectsEndOfLine())
        {
            throw new SyntaxErrorException("Expected end of line", Peek(0));
        }
    }

    private VariableReferenceNode? GetVariableReferenceNode()
    {
        if (MatchAndRemove(Token.TokenType.Identifier) is { } id)
        {
            if (MatchAndRemove(Token.TokenType.LeftBracket) is { })
            {
                var exp = Expression();
                if (exp == null)
                    throw new SyntaxErrorException(
                        "Need an expression after the left bracket!",
                        Peek(0)
                    );
                if (MatchAndRemove(Token.TokenType.RightBracket) is null)
                    throw new SyntaxErrorException(
                        "Need a right bracket after the expression!",
                        Peek(0)
                    );
                return new VariableReferenceNode(id.Value ?? string.Empty, exp);
            }
            return new VariableReferenceNode(id.Value ?? string.Empty);
        }
        return null;
    }

    public ModuleNode? Module()
    {
        ModuleNode? module = null;
        string? moduleName;
        if (MatchAndRemove(Token.TokenType.Module) == null)
        {
            moduleName = null;
        }
        else
        {
            var token = MatchAndRemove(Token.TokenType.Identifier);
            if (token == null || token.Value == null)
            {
                throw new SyntaxErrorException(
                    "A file declared as a module must be followed by an identifier, not ",
                    Peek(0)
                );
            }
            moduleName = token.Value;
            MatchAndRemove(Token.TokenType.EndOfLine);
        }
        module = new ModuleNode(moduleName);
        while (_tokens.Count > 0)
        {
            if (MatchAndRemove(Token.TokenType.EndOfLine) != null)
                continue;
            else if (MatchAndRemove(Token.TokenType.Export) != null)
            {
                module.addExportNames(Export());
            }
            else if (MatchAndRemove(Token.TokenType.Import) != null)
            {
                if (Peek(1).Type == Token.TokenType.LeftBracket)
                    module.addImportNames(Import(), checkForFunctions());
                else
                    module.addImportName(Import());
            }
            else if (MatchAndRemove(Token.TokenType.Define) != null)
                module.addFunction(Function(moduleName));
            else if (MatchAndRemove(Token.TokenType.Record) != null)
                module.AddRecord(Record(moduleName));
            else if (MatchAndRemove(Token.TokenType.Test) != null)
                module.addTest(Test(moduleName));
            else
            {
                throw new SyntaxErrorException(
                    "Any statement at indent zero must begin with the keyword `import`,"
                        + " `export`, `define`, or `record`. The following is invalid: ",
                    Peek(0)
                );
            }
        }
        //if (MatchAndRemove(Token.TokenType.Module) != null)
        //{
        //    module = new Module();
        //}
        return module;
    }

    public FunctionNode? Function(string moduleName)
    {
        // Process function name.
        var name =
            MatchAndRemove(Token.TokenType.Identifier)
            ?? throw new SyntaxErrorException("Expected a function name", Peek(0));

        // Create the function node.
        var funcNode = new FunctionNode(name.GetIdentifierValue(), moduleName);

        // Process parameter variables.
        if (MatchAndRemove(Token.TokenType.LeftParen) == null)
            throw new SyntaxErrorException("Expected a left paren", Peek(0));
        var done = false;
        while (!done)
        {
            var vars = GetVariables();
            done = vars == null;
            if (vars != null)
            {
                funcNode.ParameterVariables.AddRange(vars);
                MatchAndRemove(Token.TokenType.Semicolon);
            }
        }
        funcNode.LineNum = Peek(0).LineNumber;
        if (MatchAndRemove(Token.TokenType.RightParen) == null)
            throw new SyntaxErrorException("Expected a right paren", Peek(0));
        MatchAndRemove(Token.TokenType.EndOfLine);

        // Process local variables.
        funcNode.LocalVariables.AddRange(ProcessConstants());
        funcNode.LocalVariables.AddRange(ProcessVariables());

        // Process function body and return function node.
        BodyFunction(funcNode);
        return funcNode;
    }

    private RecordNode Record(string moduleName)
    {
        var name =
            MatchAndRemove(Token.TokenType.Identifier)
            ?? throw new SyntaxErrorException("Expected a record name", Peek(0));
        RequiresEndOfLine();

        var recNode = new RecordNode(name.GetIdentifierValue(), moduleName);
        BodyRecord(recNode);
        return recNode;
    }

    private void BodyFunction(FunctionNode function)
    {
        Body(function.Statements);
    }

    private void BodyRecord(RecordNode record)
    {
        Body(record.Members, true);
    }

    private void Body(List<StatementNode> statements, bool isRecord = false)
    {
        if (MatchAndRemove(Token.TokenType.Indent) is null)
        {
            throw new SyntaxErrorException("Expected an indent", Peek(0));
        }

        Statements(statements, isRecord);

        if (MatchAndRemove(Token.TokenType.Dedent) is null)
        {
            throw new SyntaxErrorException("Expected a dedent", Peek(0));
        }
    }

    private void Statements(List<StatementNode> statements, bool isRecord = false)
    {
        StatementNode? s;
        do
        {
            s = Statement(isRecord);

            if (s is null)
            {
                continue;
            }

            statements.Add(s);
        } while (s is not null);
    }

    private StatementNode? Statement(bool isRecord = false)
    {
        // Try to parse a statement as a record member declaration based on the value of an
        // "isRecord" flag. We need to use this flag because two arbitrary identifiers in a row
        // could be a record member declaration or a function call from the Parser's point of view,
        // so we need to know the context.
        return Assignment()
            ?? While()
            ?? Repeat()
            ?? For()
            ?? If()
            ?? (isRecord ? RecordMember() : FunctionCall());
    }

    private bool IsTokenShankType(Token t)
    {
        return _shankTokenTypes.Contains(t.Type);
    }

    private RecordMemberNode? RecordMember()
    {
        var typeToken = MatchAndRemoveMultiple(_shankTokenTypesPlusIdentifier);
        if (typeToken is null)
        {
            return null;
        }

        var nameToken = MatchAndRemove(Token.TokenType.Identifier);
        if (nameToken is null)
        {
            return null;
        }

        RequiresEndOfLine();

        return IsTokenShankType(typeToken)
            ? new RecordMemberNode(
                nameToken.GetIdentifierValue(),
                GetDataTypeFromTokenType(typeToken.Type)
            )
            : new RecordMemberNode(nameToken.GetIdentifierValue(), typeToken.GetIdentifierValue());
    }

    private VariableNode.DataType GetDataTypeFromTokenType(Token.TokenType tt) =>
        tt switch
        {
            Token.TokenType.Integer => VariableNode.DataType.Integer,
            Token.TokenType.Real => VariableNode.DataType.Real,
            Token.TokenType.Boolean => VariableNode.DataType.Boolean,
            Token.TokenType.Character => VariableNode.DataType.Character,
            Token.TokenType.String => VariableNode.DataType.String,
            Token.TokenType.Array => VariableNode.DataType.Array,
            _ => throw new InvalidOperationException("Bad TokenType for conversion into DataType"),
        };

    private FunctionCallNode? FunctionCall()
    {
        var name = MatchAndRemove(Token.TokenType.Identifier);
        if (name == null)
            return null;
        var parameters = new List<ParameterNode>();
        int lineNum = name.LineNumber;
        while (MatchAndRemove(Token.TokenType.EndOfLine) == null)
        {
            var isVariable = MatchAndRemove(Token.TokenType.Var) != null;
            var variable = GetVariableReferenceNode();
            if (variable == null)
            { // might be a constant
                var f = Factor();
                if (f == null)
                    throw new SyntaxErrorException(
                        $"Expected a constant or a variable instead of {_tokens[0]}",
                        Peek(0)
                    );
                parameters.Add(new ParameterNode(f));
            }
            else
                parameters.Add(new ParameterNode(variable, isVariable));

            MatchAndRemove(Token.TokenType.Comma);
        }

        var retVal = new FunctionCallNode(name.Value != null ? name.Value : string.Empty);
        retVal.Parameters.AddRange(parameters);
        retVal.LineNum = lineNum;
        return retVal;
    }

    private StatementNode? If()
    {
        if (MatchAndRemove(Token.TokenType.If) == null)
            return null;
        var boolExp = BooleanExpression();
        if (boolExp == null)
            throw new SyntaxErrorException("Expected a boolean expression in the if.", Peek(0));
        if (MatchAndRemove(Token.TokenType.Then) == null)
            throw new SyntaxErrorException("Expected a then in the if.", Peek(0));
        MatchAndRemove(Token.TokenType.EndOfLine);
        var body = new List<StatementNode>();
        Body(body);
        return new IfNode(boolExp, body, elseAndElseIf());
    }

    private IfNode? elseAndElseIf()
    {
        if (MatchAndRemove(Token.TokenType.Elsif) != null)
        {
            var boolExp = BooleanExpression();
            if (boolExp == null)
                throw new SyntaxErrorException(
                    "Expected a boolean expression in the elsif.",
                    Peek(0)
                );
            if (MatchAndRemove(Token.TokenType.Then) == null)
                throw new SyntaxErrorException("Expected a then in the if.", Peek(0));
            MatchAndRemove(Token.TokenType.EndOfLine);
            var body = new List<StatementNode>();
            Body(body);
            return new IfNode(boolExp, body, elseAndElseIf());
        }

        if (MatchAndRemove(Token.TokenType.Else) != null)
        {
            MatchAndRemove(Token.TokenType.EndOfLine);
            var body = new List<StatementNode>();
            Body(body);
            return new ElseNode(body);
        }
        return null;
    }

    private StatementNode? While()
    {
        if (MatchAndRemove(Token.TokenType.While) == null)
            return null;
        var boolExp = BooleanExpression();
        MatchAndRemove(Token.TokenType.EndOfLine);
        var statements = new List<StatementNode>();
        Body(statements);
        return new WhileNode(boolExp, statements);
    }

    private StatementNode? Repeat()
    {
        if (MatchAndRemove(Token.TokenType.Repeat) == null)
            return null;
        MatchAndRemove(Token.TokenType.EndOfLine);
        var statements = new List<StatementNode>();
        Body(statements);
        if (MatchAndRemove(Token.TokenType.Until) == null)
            throw new SyntaxErrorException("Expected an until to end the repeat.", Peek(0));
        var boolExp = BooleanExpression();
        if (boolExp == null)
            throw new SyntaxErrorException(
                "Expected a boolean expression at the end of the repeat.",
                Peek(0)
            );
        MatchAndRemove(Token.TokenType.EndOfLine);
        return new RepeatNode(boolExp, statements);
    }

    private StatementNode? For()
    {
        if (MatchAndRemove(Token.TokenType.For) == null)
            return null;
        var indexVariable = GetVariableReferenceNode();
        if (indexVariable == null)
            throw new SyntaxErrorException("Expected a variable in the for statement.", Peek(0));
        if (MatchAndRemove(Token.TokenType.From) == null)
            throw new SyntaxErrorException("Expected a from in the for statement.", Peek(0));
        var fromExp = Expression();
        if (fromExp == null)
            throw new SyntaxErrorException(
                "Expected a from expression in the for statement.",
                Peek(0)
            );
        if (MatchAndRemove(Token.TokenType.To) == null)
            throw new SyntaxErrorException("Expected a to in the for statement.", Peek(0));
        var toExp = Expression();
        if (toExp == null)
            throw new SyntaxErrorException(
                "Expected a to expression in the for statement.",
                Peek(0)
            );
        MatchAndRemove(Token.TokenType.EndOfLine);
        var statements = new List<StatementNode>();
        Body(statements);
        return new ForNode(indexVariable, fromExp, toExp, statements);
    }

    private BooleanExpressionNode BooleanExpression()
    {
        var expression = Expression();
        if (expression is not BooleanExpressionNode ben)
            throw new SyntaxErrorException("Expected a boolean expression", Peek(0));
        return ben;
    }

    private StatementNode? Assignment()
    {
        if (Peek(1)?.Type is Token.TokenType.Assignment or Token.TokenType.LeftBracket)
        {
            var target = GetVariableReferenceNode();
            if (target == null)
                throw new SyntaxErrorException(
                    "Found an assignment without a valid identifier.",
                    Peek(0)
                );
            MatchAndRemove(Token.TokenType.Assignment);
            var expression = ParseExpressionLine();
            if (expression == null)
                throw new SyntaxErrorException(
                    "Found an assignment without a valid right hand side.",
                    Peek(0)
                );
            return new AssignmentNode(target, expression);
        }
        else
        {
            return null;
        }
    }

    private List<VariableNode> ProcessVariables()
    {
        var retVal = new List<VariableNode>();

        while (MatchAndRemove(Token.TokenType.Variables) != null)
        {
            var nextOnes = GetVariables();
            retVal.AddRange(nextOnes);
            MatchAndRemove(Token.TokenType.EndOfLine);
        }
        return retVal;
    }

    private List<VariableNode>? GetVariables()
    {
        var names = new List<string>();
        var isConstant = MatchAndRemove(Token.TokenType.Var) == null;
        var name = MatchAndRemove(Token.TokenType.Identifier);
        if (name == null)
            return null;
        names.Add(name.Value ?? string.Empty);
        while (MatchAndRemove(Token.TokenType.Comma) != null)
        {
            name = MatchAndRemove(Token.TokenType.Identifier);
            if (name == null)
                throw new SyntaxErrorException("Expected a name", Peek(0));
            names.Add(name.Value ?? string.Empty);
        }
        if (MatchAndRemove(Token.TokenType.Colon) == null)
            throw new SyntaxErrorException("Expected a colon", Peek(0));
        if (MatchAndRemove(Token.TokenType.Integer) != null)
        {
            var retVal = names
                .Select(
                    n =>
                        new VariableNode()
                        {
                            InitialValue = null,
                            IsConstant = isConstant,
                            Type = VariableNode.DataType.Integer,
                            Name = n
                        }
                )
                .ToList();
            CheckForRange(retVal);
            return retVal;
        }
        else if (MatchAndRemove(Token.TokenType.Real) != null)
        {
            var retVal = names
                .Select(
                    n =>
                        new VariableNode()
                        {
                            InitialValue = null,
                            IsConstant = isConstant,
                            Type = VariableNode.DataType.Real,
                            Name = n
                        }
                )
                .ToList();
            CheckForRange(retVal);
            return retVal;
        }
        else if (MatchAndRemove(Token.TokenType.Boolean) != null)
        {
            var retVal = names
                .Select(
                    n =>
                        new VariableNode()
                        {
                            InitialValue = null,
                            IsConstant = isConstant,
                            Type = VariableNode.DataType.Boolean,
                            Name = n
                        }
                )
                .ToList();
            CheckForRange(retVal);
            return retVal;
        }
        else if (MatchAndRemove(Token.TokenType.Character) != null)
        {
            var retVal = names
                .Select(
                    n =>
                        new VariableNode()
                        {
                            InitialValue = null,
                            IsConstant = isConstant,
                            Type = VariableNode.DataType.Character,
                            Name = n
                        }
                )
                .ToList();
            CheckForRange(retVal);
            return retVal;
        }
        else if (MatchAndRemove(Token.TokenType.String) != null)
        {
            var retVal = names
                .Select(
                    n =>
                        new VariableNode()
                        {
                            InitialValue = null,
                            IsConstant = isConstant,
                            Type = VariableNode.DataType.String,
                            Name = n
                        }
                )
                .ToList();
            CheckForRange(retVal);
            return retVal;
        }
        else if (MatchAndRemove(Token.TokenType.Array) != null)
        {
            var retVal = names
                .Select(
                    n =>
                        new VariableNode()
                        {
                            InitialValue = null,
                            IsConstant = isConstant,
                            Type = VariableNode.DataType.Array,
                            Name = n
                        }
                )
                .ToList();
            CheckForRange(retVal);
            if (MatchAndRemove(Token.TokenType.Of) == null)
                throw new SyntaxErrorException(
                    $"In the declaration of the array for {retVal.First().Name}, no array type found.",
                    Peek(0)
                );
            var arrayType = _tokens[0];
            _tokens.RemoveAt(0);
            switch (arrayType.Type)
            {
                case Token.TokenType.Integer:
                    retVal.ForEach(d => d.ArrayType = VariableNode.DataType.Integer);
                    break;
                case Token.TokenType.Real:
                    retVal.ForEach(d => d.ArrayType = VariableNode.DataType.Real);
                    break;
                case Token.TokenType.Boolean:
                    retVal.ForEach(d => d.ArrayType = VariableNode.DataType.Boolean);
                    break;
                case Token.TokenType.Character:
                    retVal.ForEach(d => d.ArrayType = VariableNode.DataType.Character);
                    break;
                case Token.TokenType.String:
                    retVal.ForEach(d => d.ArrayType = VariableNode.DataType.String);
                    break;
                default:
                    throw new SyntaxErrorException(
                        $"In the declaration of the array for {retVal.First().Name}, invalid array type {arrayType.Type} found.",
                        Peek(0)
                    );
            }
            return retVal;
        }
        else
            throw new SyntaxErrorException("Unknown data type!", Peek(0));
    }

    private void CheckForRange(List<VariableNode> retVal)
    {
        if (MatchAndRemove(Token.TokenType.From) == null)
            return;
        var fromToken = _tokens[0];
        _tokens.RemoveAt(0);
        var fromNode = ProcessConstant(fromToken);
        if (fromToken.Type == Token.TokenType.Number)
            retVal.ForEach(v => v.From = fromNode);
        else
            throw new SyntaxErrorException(
                $"In the declaration of {retVal.First().Name}, invalid from value {fromToken} found.",
                Peek(0)
            );
        if (MatchAndRemove(Token.TokenType.To) == null)
            throw new SyntaxErrorException(
                $"In the declaration of {retVal.First().Name}, no 'to' found.",
                Peek(0)
            );
        var toToken = _tokens[0];
        _tokens.RemoveAt(0);
        var toNode = ProcessConstant(toToken);
        if (toToken.Type == Token.TokenType.Number)
            retVal.ForEach(v => v.To = toNode);
        else
            throw new SyntaxErrorException(
                $"In the declaration of {retVal.First().Name}, invalid to value {toToken} found.",
                Peek(0)
            );
    }

    private List<VariableNode> ProcessConstants()
    {
        var retVal = new List<VariableNode>();
        while (MatchAndRemove(Token.TokenType.Constants) != null)
        {
            while (true)
            {
                var name = MatchAndRemove(Token.TokenType.Identifier);
                if (name == null)
                    throw new SyntaxErrorException("Expected a name", Peek(0));
                if (MatchAndRemove(Token.TokenType.Equal) == null)
                    throw new SyntaxErrorException("Expected an equal sign", Peek(0));
                var num = MatchAndRemove(Token.TokenType.Number);
                if (num != null)
                {
                    var node = ProcessConstant(num);
                    retVal.Add(
                        node is FloatNode
                            ? new VariableNode()
                            {
                                InitialValue = node,
                                Type = VariableNode.DataType.Real,
                                IsConstant = true,
                                Name = name.Value ?? ""
                            }
                            : new VariableNode()
                            {
                                InitialValue = node,
                                Type = VariableNode.DataType.Integer,
                                IsConstant = true,
                                Name = name.Value ?? ""
                            }
                    );
                }
                else
                {
                    var chr = MatchAndRemove(Token.TokenType.CharContents);
                    if (chr != null)
                    {
                        retVal.Add(
                            new VariableNode()
                            {
                                InitialValue = new CharNode((chr?.Value ?? " ")[0]),
                                Type = VariableNode.DataType.Character,
                                IsConstant = true,
                                Name = name.Value ?? ""
                            }
                        );
                    }
                    else
                    {
                        var str = MatchAndRemove(Token.TokenType.StringContents);
                        if (str != null)
                        {
                            retVal.Add(
                                new VariableNode()
                                {
                                    InitialValue = new StringNode(str?.Value ?? ""),
                                    Type = VariableNode.DataType.String,
                                    IsConstant = true,
                                    Name = name.Value ?? ""
                                }
                            );
                        }
                        else
                        {
                            throw new SyntaxErrorException("Expected a value", Peek(0));
                        }
                    }
                }

                if (MatchAndRemove(Token.TokenType.Comma) == null)
                    break;
            }

            MatchAndRemove(Token.TokenType.EndOfLine);
        }

        return retVal;
    }

    private ASTNode ProcessConstant(Token num)
    {
        return (num.Value ?? "").Contains('.')
            ? new FloatNode(float.Parse(num.Value ?? ""))
            : new IntNode(int.Parse(num.Value ?? ""));
    }

    public ASTNode? ParseExpressionLine()
    {
        var retVal = Expression();
        MatchAndRemove(Token.TokenType.EndOfLine);
        return retVal;
    }

    public ASTNode? Expression()
    {
        var lt = Term();
        if (lt == null)
            return null;
        return ExpressionRHS(lt);
    }

    public ASTNode? ExpressionRHS(ASTNode lt)
    {
        if (MatchAndRemove(Token.TokenType.Plus) != null)
        {
            var rt = Term();
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.plus, rt);
            return ExpressionRHS(lt);
        }
        else if (MatchAndRemove(Token.TokenType.Minus) != null)
        {
            var rt = Term();
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.minus, rt);
            return ExpressionRHS(lt);
        }
        else if (MatchAndRemove(Token.TokenType.LessEqual) != null)
        {
            var rt = Term();
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
            return new BooleanExpressionNode(
                lt,
                BooleanExpressionNode.BooleanExpressionOpType.le,
                rt
            );
        }
        else if (MatchAndRemove(Token.TokenType.LessThan) != null)
        {
            var rt = Term();
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
            return new BooleanExpressionNode(
                lt,
                BooleanExpressionNode.BooleanExpressionOpType.lt,
                rt
            );
        }
        else if (MatchAndRemove(Token.TokenType.GreaterEqual) != null)
        {
            var rt = Term();
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
            return new BooleanExpressionNode(
                lt,
                BooleanExpressionNode.BooleanExpressionOpType.ge,
                rt
            );
        }
        else if (MatchAndRemove(Token.TokenType.Greater) != null)
        {
            var rt = Term();
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
            return new BooleanExpressionNode(
                lt,
                BooleanExpressionNode.BooleanExpressionOpType.gt,
                rt
            );
        }
        else if (MatchAndRemove(Token.TokenType.Equal) != null)
        {
            var rt = Term();
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
            return new BooleanExpressionNode(
                lt,
                BooleanExpressionNode.BooleanExpressionOpType.eq,
                rt
            );
        }
        else if (MatchAndRemove(Token.TokenType.NotEqual) != null)
        {
            var rt = Term();
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
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

    private ASTNode? Term()
    {
        var lt = Factor();
        if (lt == null)
            return null;
        return TermRHS(lt);
    }

    private ASTNode? TermRHS(ASTNode lt)
    {
        if (MatchAndRemove(Token.TokenType.Times) != null)
        {
            var rt = Factor();
            if (rt == null)
                throw new SyntaxErrorException("Expected a factor.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.times, rt);
            return TermRHS(lt);
        }
        else if (MatchAndRemove(Token.TokenType.Divide) != null)
        {
            var rt = Factor();
            if (rt == null)
                throw new SyntaxErrorException("Expected a factor.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.divide, rt);
            return TermRHS(lt);
        }
        else if (MatchAndRemove(Token.TokenType.Mod) != null)
        {
            var rt = Factor();
            if (rt == null)
                throw new SyntaxErrorException("Expected a factor.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.modulo, rt);
            return TermRHS(lt);
        }
        else
        {
            return lt;
        }
    }

    private ASTNode? Factor()
    {
        if (MatchAndRemove(Token.TokenType.LeftParen) != null)
        {
            var exp = Expression();
            if (MatchAndRemove(Token.TokenType.RightParen) == null)
                throw new SyntaxErrorException("Expected a right paren.", Peek(0));
            return exp;
        }

        if (GetVariableReferenceNode() is { } variable)
        {
            return variable;
        }

        if (MatchAndRemove(Token.TokenType.StringContents) is { } sc)
        {
            return new StringNode(sc.Value ?? string.Empty);
        }
        if (MatchAndRemove(Token.TokenType.CharContents) is { } cc)
        {
            if (cc.Value is null || cc.Value.Length != 1)
                throw new SyntaxErrorException($"Invalid character constant {cc.Value}", Peek(0));
            return new CharNode(cc.Value[0]);
        }
        if (MatchAndRemove(Token.TokenType.True) is { })
            return new BoolNode(true);
        if (MatchAndRemove(Token.TokenType.False) is { })
            return new BoolNode(false);

        var token = MatchAndRemove(Token.TokenType.Number);
        if (token == null || token.Value == null)
            return null;
        if (token.Value.Contains("."))
            return new FloatNode(float.Parse(token.Value));
        return new IntNode(int.Parse(token.Value));
    }

    //private string? Export()
    private LinkedList<string> Export()
    {
        var token = MatchAndRemove(Token.TokenType.Identifier);
        if (token == null || token.Value == null)
            throw new SyntaxErrorException(
                "An export call must be followed by an identifier, not ",
                Peek(0)
            );
        LinkedList<string> exports = new LinkedList<string>();
        exports.AddLast(token.Value);
        while (MatchAndRemove(Token.TokenType.Comma) != null)
        {
            token = MatchAndRemove(Token.TokenType.Identifier);
            if (token == null || token.Value == null)
                throw new SyntaxErrorException(
                    "An comma in an export call must be followed by an identifer, ",
                    Peek(0)
                );
            exports.AddLast(token.Value);
        }
        //TODO: add handling for {} and [] from shank language definition
        return exports;
    }

    private string? Import()
    {
        var token = MatchAndRemove(Token.TokenType.Identifier);
        if (token == null || token.Value == null)
            throw new SyntaxErrorException(
                "An import call must be followed by an identifier, not ",
                Peek(0)
            );
        return token.Value;
    }

    private LinkedList<string> checkForFunctions()
    {
        var functionsToImport = new LinkedList<string>();
        MatchAndRemove(Token.TokenType.LeftBracket);
        while (MatchAndRemove(Token.TokenType.RightBracket) == null)
        {
            var token = MatchAndRemove(Token.TokenType.Identifier);
            if (token == null || token.Value == null)
            {
                throw new SyntaxErrorException(
                    "Expecting an identifer after a left bracket in an import statement, not ",
                    Peek(0)
                );
            }
            functionsToImport.AddLast(token.Value);
            if (Peek(1).Type == Token.TokenType.Identifier)
            {
                if (MatchAndRemove(Token.TokenType.Comma) == null)
                {
                    throw new SyntaxErrorException(
                        "Expecting a comma in between identifiers in an import statment, not ",
                        Peek(0)
                    );
                }
            }
        }
        return functionsToImport;
    }

    private TestNode Test(string? parentModuleName)
    {
        TestNode test;
        Token? token;
        if ((token = MatchAndRemove(Token.TokenType.Identifier)) == null)
        {
            throw new SyntaxErrorException(
                "Expected an identifier after 'test' token, not: ",
                Peek(0)
            );
        }
        string testName = token.Value;
        if (MatchAndRemove(Token.TokenType.For) == null)
        {
            throw new SyntaxErrorException("Expected an for token after test name, not: ", Peek(0));
        }
        if ((token = MatchAndRemove(Token.TokenType.Identifier)) == null)
        {
            throw new SyntaxErrorException(
                "Expected an identifier after 'for' token in test statement, not: ",
                Peek(0)
            );
        }
        test = new TestNode(testName, token.Value);
        if (parentModuleName != null)
            test.parentModuleName = parentModuleName;
        else
            test.parentModuleName = "default";
        if (MatchAndRemove(Token.TokenType.LeftParen) == null)
            throw new SyntaxErrorException("Expected a left paren", Peek(0));
        var done = false;
        while (!done)
        {
            var vars = GetVariables();
            done = vars == null;
            if (vars != null)
            {
                test.testingFunctionParameters.AddRange(vars);
                MatchAndRemove(Token.TokenType.Semicolon);
            }
        }
        if (MatchAndRemove(Token.TokenType.RightParen) == null)
            throw new SyntaxErrorException("Expected a right paren", Peek(0));
        test.LineNum = Peek(0).LineNumber;
        MatchAndRemove(Token.TokenType.EndOfLine);

        // Process local variables.

        test.LocalVariables.AddRange(ProcessConstants());
        test.LocalVariables.AddRange(ProcessVariables());

        // Process function body and return function node.

        BodyFunction(test);
        return test;
    }
}
