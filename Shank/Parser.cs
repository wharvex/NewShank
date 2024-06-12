using Shank.ASTNodes;

namespace Shank;

public class Parser
{
    private readonly Token.TokenType[] _shankTokenTypesPlusIdentifier =
    [
        Token.TokenType.Integer,
        Token.TokenType.Real,
        Token.TokenType.Boolean,
        Token.TokenType.Character,
        Token.TokenType.String,
        Token.TokenType.Array,
        Token.TokenType.RefersTo,
        Token.TokenType.Identifier
    ];

    private readonly Token.TokenType[] _indentZeroTokenTypes =
    [
        Token.TokenType.Define,
        Token.TokenType.Record,
        Token.TokenType.Enum,
        Token.TokenType.Variables,
        Token.TokenType.Export,
        Token.TokenType.Import,
        Token.TokenType.Test
    ];

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    private readonly List<Token> _tokens;

    public static int Line { get; set; }
    public static string FileName { get; set; }

    private Token? MatchAndRemove(Token.TokenType t)
    {
        // If there are no Tokens left, return null.
        if (_tokens.Count == 0)
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

        // Update line num tracker.
        if (t != Token.TokenType.EndOfLine)
        {
            Line = retVal.LineNumber;
        }

        // Consume blank lines logic.
        // TODO
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

    private Token? MatchAndRemoveMultiple(params Token.TokenType[] ts)
    {
        Token? ret = null;
        var i = 0;
        while (ret is null && i < ts.Length)
        {
            ret = MatchAndRemove(ts[i++]);
        }

        return ret;
    }

    private Token? Peek(int offset)
    {
        return _tokens.Count > offset ? _tokens[offset] : null;
    }

    private Token PeekSafe(int offset) =>
        Peek(offset) ?? throw new SyntaxErrorException("Unexpected EOF", null);

    private bool ExpectsEndOfLine()
    {
        var ret = MatchAndRemove(Token.TokenType.EndOfLine) is not null;

        // TODO
        // Uncomment this part when we implement the overhaul described in MatchAndRemove. For now,
        // this code would be redundant.
        //if (ret)
        //{
        //    ConsumeBlankLines();
        //}

        return ret;
    }

    private void ConsumeBlankLines()
    {
        while (MatchAndRemove(Token.TokenType.EndOfLine) is not null)
        {
            //while (MatchAndRemove(Token.TokenType.Indent) is not null) { }
        }
    }

    private void RequiresEndOfLine()
    {
        if (!ExpectsEndOfLine())
        {
            throw new SyntaxErrorException("Expected end of line", Peek(0));
        }
    }

    private void checkTokenListSize(int offset)
    {
        if (_tokens.Count < offset)
            throw new Exception($"Out of bounds, cannot peek {offset} tokens ahead.");
    }

    private Token PeekAndCheck(int offset)
    {
        checkTokenListSize(offset);
        return _tokens[offset];
    }

    private VariableUsageNode? GetVariableUsageNode()
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
                return new VariableUsageNode(
                    id.GetValueSafe(),
                    exp,
                    VariableUsageNode.VrnExtType.ArrayIndex
                );
            }

            if (MatchAndRemove(Token.TokenType.Dot) is not null)
            {
                VariableUsageNode? varRef = GetVariableUsageNode();
                if (varRef is null)
                    throw new SyntaxErrorException(
                        "Need a record member reference after the dot!",
                        Peek(0)
                    );
                return new VariableUsageNode(
                    id.GetValueSafe(),
                    varRef,
                    VariableUsageNode.VrnExtType.RecordMember
                );
            }
            return new VariableUsageNode(id.GetValueSafe());
        }

        return null;
    }

    private VariableUsageExpressionNode? GetVariableUsageExpression()
    {
        var vuNameToken = MatchAndRemove(Token.TokenType.Identifier);
        if (vuNameToken is null)
        {
            return null;
        }

        return null;
    }

    public ModuleNode Module()
    {
        // Get the module name if there is one, or set it to default.
        var moduleName = MatchAndRemove(Token.TokenType.Module) is null
            ? "default"
            : MatchAndRemove(Token.TokenType.Identifier)?.GetValueSafe()
                ?? throw new SyntaxErrorException("Expected a module name", Peek(0));

        // Require EOL if a module declaration was found.
        if (!moduleName.Equals("default"))
        {
            RequiresEndOfLine();
        }

        // Create the module.
        var ret = new ModuleNode(moduleName);

        // Allow the file to start with blank lines.
        ConsumeBlankLines();

        // Require a token that can start an "indent-zero" language construct in Shank.
        var indentZeroToken =
            MatchAndRemoveMultiple(_indentZeroTokenTypes)
            ?? throw new SyntaxErrorException("Cannot parse a blank file or module", Peek(0));

        // Continue parsing constructs until an indent-zero token is not found.
        while (indentZeroToken is not null)
        {
            // Handle the construct based on what type it is.
            switch (indentZeroToken.Type)
            {
                case Token.TokenType.Export:
                    ret.addExportNames(Export());
                    break;
                case Token.TokenType.Import:
                    // An import statement starts with the `import' keyword, then a mandatory module
                    // name to import from, then optional square brackets surrounding one or more
                    // names of specific constructs from that module to import.

                    // Require the import module name.
                    var importModuleName =
                        MatchAndRemove(Token.TokenType.Identifier)?.GetValueSafe()
                        ?? throw new SyntaxErrorException(
                            "Expected an import module name",
                            Peek(0)
                        );

                    // Check for the optional square brackets.
                    if (PeekSafe(1).Type is Token.TokenType.LeftBracket)
                    {
                        ret.addImportNames(importModuleName, checkForFunctions());
                    }
                    else
                    {
                        ret.addImportName(importModuleName);
                    }

                    break;
                case Token.TokenType.Define:
                    ret.addFunction(Function(moduleName));
                    break;
                case Token.TokenType.Record:
                    ret.AddRecord(Record(moduleName));
                    break;
                case Token.TokenType.Test:
                    ret.addTest(Test(moduleName));
                    break;
                case Token.TokenType.Enum:
                    ret.addEnum(MakeEnum(moduleName));
                    break;
                case Token.TokenType.Variables:
                    ret.AddToGlobalVariables(ProcessVariablesDoWhile(moduleName));
                    break;
                default:
                    throw new NotImplementedException(
                        "Support for parsing indent zero token "
                            + indentZeroToken
                            + " has not been implemented."
                    );
            }

            ConsumeBlankLines();

            // Check for another construct.
            indentZeroToken = MatchAndRemoveMultiple(_indentZeroTokenTypes);
        }

        return ret;
    }

    public FunctionNode Function(string moduleName)
    {
        // Process function name.
        var name =
            MatchAndRemove(Token.TokenType.Identifier)
            ?? throw new SyntaxErrorException("Expected a function name", Peek(0));

        // Create the function node.
        var funcNode = new FunctionNode(name.GetValueSafe(), moduleName);

        // Process parameter variables.
        if (MatchAndRemove(Token.TokenType.LeftParen) == null)
            throw new SyntaxErrorException("Expected a left paren", Peek(0));
        var done = false;
        while (!done)
        {
            var vars = GetVariables(moduleName, VariableNode.DeclarationContext.FunctionSignature);
            done = vars == null;
            if (vars != null)
            {
                funcNode.ParameterVariables.AddRange(vars);
                MatchAndRemove(Token.TokenType.Semicolon);
            }
        }

        // Create an extension for the function's name based on its parameters, so overloads don't
        // produce name collisions in the functions dictionary.
        var overloadNameExt = "";
        funcNode.ParameterVariables.ForEach(vn => overloadNameExt += vn.ToStringForOverloadExt());
        funcNode.OverloadNameExt = overloadNameExt;

        funcNode.LineNum = Peek(0).LineNumber;

        RequiresToken(Token.TokenType.RightParen);

        funcNode.GenericTypeParameterNames = ParseGenericKeywordAndTypeParams();

        RequiresEndOfLine();

        // Process local variables .
        funcNode.LocalVariables.AddRange(ProcessConstants(moduleName));
        funcNode.LocalVariables.AddRange(ProcessVariables(moduleName));

        // Process function body and return function node.
        BodyFunction(funcNode);
        return funcNode;
    }

    private RecordNode Record(string moduleName)
    {
        var name = RequiresAndReturnsToken(Token.TokenType.Identifier);

        var genericTypeParameterNames = ParseGenericKeywordAndTypeParams();

        RequiresEndOfLine();

        var recNode = new RecordNode(
            name.GetValueSafe(),
            moduleName,
            BodyRecord(moduleName),
            genericTypeParameterNames
        );
        return recNode;
    }

    private List<string>? ParseGenericKeywordAndTypeParams()
    {
        if (MatchAndRemove(Token.TokenType.Generic) is null)
        {
            return null;
        }

        var typeParam =
            MatchAndRemove(Token.TokenType.Identifier)
            ?? throw new SyntaxErrorException("Expected an identifier", Peek(0));

        List<string> ret = [];
        ParseCommaSeparatedIdentifiers(typeParam, ret);

        return ret;
    }

    private void BodyFunction(FunctionNode function)
    {
        StatementsBody(function.Statements);
    }

    private List<VariableNode> BodyRecord(string parentModule)
    {
        //StatementsBody(record.Members, true);
        List<ASTNode> bodyContents = [];
        Body(bodyContents, parentModule, GetVariablesRecord);
        return bodyContents.Cast<VariableNode>().ToList();
    }

    private void StatementsBody(List<StatementNode> statements, bool isRecord = false)
    {
        RequiresToken(Token.TokenType.Indent);

        Statements(statements, isRecord);

        RequiresToken(Token.TokenType.Dedent);
    }

    private void Body(
        List<ASTNode> bodyContents,
        string parentModuleName,
        Action<List<ASTNode>, string> bodyContentsParser
    )
    {
        RequiresToken(Token.TokenType.Indent);

        bodyContentsParser(bodyContents, parentModuleName);

        RequiresToken(Token.TokenType.Dedent);
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

    private void Statements2(List<ASTNode> statements)
    {
        StatementNode? s = Statement2();
        while (s is not null)
        {
            statements.Add(s);
            s = Statement2();
        }
    }

    private StatementNode? Statement(bool isRecord = false)
    {
        // In order to know if we should parse a statement as a record member declaration, we use
        // an "isRecord" flag. We need to use this flag because two arbitrary identifiers in a row
        // could be a record member declaration or a function call from the Parser's point of view,
        // so we need to know the context.

        // if (Peek(0)?.Type is Token.TokenType.EndOfLine)
        // {
        //     MatchAndRemove(Token.TokenType.EndOfLine);
        // }

        return Assignment()
            ?? While()
            ?? Repeat()
            ?? For()
            ?? If()
            ?? (isRecord ? RecordMember() : FunctionCall());
    }

    private StatementNode? Statement2()
    {
        return Assignment() ?? While() ?? Repeat() ?? For() ?? If() ?? FunctionCall();
    }

    private RecordMemberNode? RecordMember()
    {
        var nameToken = MatchAndRemove(Token.TokenType.Identifier);

        if (nameToken is null)
        {
            return null;
        }

        RequiresToken(Token.TokenType.Colon);
        var type = Type(VariableNode.DeclarationContext.RecordDeclaration);

        RequiresEndOfLine();
        return new RecordMemberNode(nameToken.GetValueSafe(), type);
    }

    public static Type GetDataTypeFromConstantNodeType(ASTNode constantNode) =>
        constantNode switch
        {
            IntNode => new IntegerType(),
            // TODO: math op node?
            FloatNode => new RealType(),
            StringNode => new StringType(),
            CharNode => new CharacterType(),
            BooleanExpressionNode or BoolNode => new BooleanType(),
            _
                => throw new InvalidOperationException(
                    "Bad constant node type for converting to data type."
                )
        };

    // assumptions you want to parse a type
    private Type Type(VariableNode.DeclarationContext declarationContext)
    {
        var typeToken =
            MatchAndRemoveMultiple(_shankTokenTypesPlusIdentifier)
            ?? throw new SyntaxErrorException("expected start of a type", Peek(0));

        return typeToken.Type switch
        {
            Token.TokenType.Real => new RealType(CheckRange(true, Range.DefaultFloat())),
            Token.TokenType.Identifier => CustomType(declarationContext, typeToken),
            Token.TokenType.Integer => new IntegerType(CheckRange(true, Range.DefaultInteger())),
            Token.TokenType.Boolean => new BooleanType(),
            Token.TokenType.Character
                => new CharacterType(CheckRange(true, Range.DefaultCharacter())),
            Token.TokenType.String => new StringType(CheckRange(true, Range.DefaultSmallInteger())),
            Token.TokenType.Array => ArrayType(declarationContext, typeToken),
            Token.TokenType.RefersTo => new ReferenceType(Type(declarationContext)),
            _ => throw new SyntaxErrorException("Unknown type", typeToken)
        };
    }

    static IEnumerable<T> Repeat<T>(Func<T> generator)
    {
        yield return generator();
    }

    private Type CustomType(VariableNode.DeclarationContext declarationContext, Token typeToken)
    {
        // see if there are type parameters
        var token = Peek(0);
        // and that they are token types the correspond with types
        // if so parse the type
        var first =
            token is not null
            && (
                _shankTokenTypesPlusIdentifier.Contains(token.Type)
                || token.Type == Token.TokenType.LeftParen
            )
                ? TypeParser()
                : null;
        // then parse each comma followed by another type parameter until we do find any more commas
        var typeParams = (
            first == null
                ? []
                : Enumerable
                    .Concat(
                        [first],
                        Repeat(
                                () =>
                                    MatchAndRemove(Token.TokenType.Comma) is null
                                        ? null
                                        : TypeParser()
                            )
                            .TakeWhile(r => r is not null)
                    )
                    .ToList()
        )!;
        return new UnknownType(typeToken.GetValueSafe(), typeParams!);
        // parses a type optionally surrounded by parenthesis
        Type TypeParser() =>
            InBetweenOpt(
                Token.TokenType.LeftParen,
                () => Type(declarationContext),
                Token.TokenType.RightParen,
                "record"
            );
    }

    private T InBetweenOpt<T>(
        Token.TokenType first,
        Func<T> parser,
        Token.TokenType last,
        string type
    )
    {
        if (MatchAndRemove(first) is { } t)
        {
            var result = parser();
            _ =
                MatchAndRemove(last)
                ?? throw new SyntaxErrorException(
                    "Could not find closing parenthesis after " + type,
                    t
                );
            return result;
        }

        return parser();
    }

    private ArrayType ArrayType(
        VariableNode.DeclarationContext declarationContext,
        Token? arrayToken
    )
    {
        var range = CheckRangeInner(false, Range.DefaultSmallInteger());
        if (range is null && declarationContext == VariableNode.DeclarationContext.VariablesLine)
        {
            throw new SyntaxErrorException(
                "Array in variables declared without a size",
                arrayToken
            );
        }

        var _ =
            MatchAndRemove(Token.TokenType.Of)
            ?? throw new SyntaxErrorException("Array declared without type missing of", arrayToken);

        return new ArrayType(Type(declarationContext));
    }

    private Range CheckRange(bool isFloat, Range defaultRange)
    {
        return CheckRangeInner(isFloat, defaultRange) ?? defaultRange;
    }

    private Range? CheckRangeInner(bool isFloat, Range defaultRange)
    {
        if (MatchAndRemove(Token.TokenType.From) is null)
        {
            return defaultRange;
        }

        var fromToken = MatchAndRemove(Token.TokenType.Number);
        var fromNode = ProcessNumericConstant(
            fromToken ?? throw new SyntaxErrorException("Expected a number", Peek(0))
        );

        RequiresToken(Token.TokenType.To);

        var toToken = MatchAndRemove(Token.TokenType.Number);
        var toNode = ProcessNumericConstant(
            toToken ?? throw new SyntaxErrorException("Expected a number", Peek(0))
        );
        if (!isFloat && fromNode is FloatNode || toNode is FloatNode)
        {
            throw new SyntaxErrorException("Expected integer type limits found float ones", null);
        }

        var fromValue = fromNode is FloatNode from ? from.Value : ((IntNode)fromNode).Value;
        var toValue = toNode is FloatNode to ? to.Value : ((IntNode)toNode).Value;
        // TODO: check range is not inverted?
        if (fromValue < defaultRange.From || fromValue > defaultRange.To)
        {
            throw new SyntaxErrorException(
                $"range starting at {fromValue} is  not valid for this type",
                fromToken
            );
        }
        if (toValue < defaultRange.From || toValue > defaultRange.To)
        {
            throw new SyntaxErrorException(
                $"range ending at {toValue} is  not valid for this type",
                toToken
            );
        }
        return new Range(fromValue, toValue);
    }

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
            var variable = GetVariableUsageNode();
            if (variable == null)
            {
                // might be a constant
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
        StatementsBody(body);
        return new IfNode(boolExp, body, ElseAndElseIf());
    }

    private IfNode? ElseAndElseIf()
    {
        if (MatchAndRemove(Token.TokenType.Elsif) != null)
        {
            var boolExp =
                BooleanExpression()
                ?? throw new SyntaxErrorException(
                    "Expected a boolean expression in the elsif.",
                    Peek(0)
                );
            RequiresToken(Token.TokenType.Then);
            RequiresEndOfLine();
            var body = new List<StatementNode>();
            StatementsBody(body);
            return new IfNode(boolExp, body, ElseAndElseIf());
        }

        if (MatchAndRemove(Token.TokenType.Else) != null)
        {
            MatchAndRemove(Token.TokenType.EndOfLine);
            var body = new List<StatementNode>();
            StatementsBody(body);
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
        StatementsBody(statements);
        return new WhileNode(boolExp, statements);
    }

    private StatementNode? Repeat()
    {
        if (MatchAndRemove(Token.TokenType.Repeat) == null)
            return null;
        MatchAndRemove(Token.TokenType.EndOfLine);
        var statements = new List<StatementNode>();
        StatementsBody(statements);
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
        var indexVariable = GetVariableUsageNode();
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
        StatementsBody(statements);
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
        if (
            Peek(1)?.Type
            is Token.TokenType.Assignment
                or Token.TokenType.LeftBracket
                or Token.TokenType.Dot
        )
        {
            var target = GetVariableUsageNode();
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

    /// <summary>
    /// Process an arbitrary number of consecutive variables declaration lines (i.e. lines that
    /// start with the 'variables' keyword).
    /// </summary>
    /// <param name="parentModule"></param>
    /// <returns></returns>
    private List<VariableNode> ProcessVariables(string parentModule)
    {
        var retVal = new List<VariableNode>();

        while (MatchAndRemove(Token.TokenType.Variables) != null)
        {
            var nextOnes = GetVariables(
                parentModule,
                VariableNode.DeclarationContext.VariablesLine
            );

            // TODO: We are potentially adding null to retVal here.
            retVal.AddRange(nextOnes);

            MatchAndRemove(Token.TokenType.EndOfLine);
        }

        return retVal;
    }

    private List<VariableNode> ProcessVariablesDoWhile(string parentModule)
    {
        var retVal = new List<VariableNode>();

        do
        {
            var nextOnes = GetVariables(
                parentModule,
                VariableNode.DeclarationContext.VariablesLine
            );

            // TODO: List.AddRange throws an ArgumentNullException if nextOnes is null.
            retVal.AddRange(nextOnes);

            MatchAndRemove(Token.TokenType.EndOfLine);
        } while (MatchAndRemove(Token.TokenType.Variables) != null);

        return retVal;
    }

    private List<VariableNode> CreateVariables(
        List<string> names,
        bool isConstant,
        string parentModuleName,
        VariableNode.DeclarationContext declarationContext
    )
    {
        var type = Type(declarationContext);

        return CreateVariablesBasic(names, isConstant, parentModuleName, type);
    }

    private List<VariableNode> CreateVariablesBasic(
        List<string> names,
        bool isConstant,
        string parentModuleName,
        Type type
    )
    {
        // ranges parsed in the type
        return names
            .Select(
                n =>
                    new VariableNode()
                    {
                        IsConstant = isConstant,
                        Type = type,
                        Name = n,
                        ModuleName = parentModuleName,
                    }
            )
            .ToList();
    }

    private Type GetTypeFromToken(Token t) =>
        t.Type switch
        {
            Token.TokenType.Integer => new IntegerType(),
            Token.TokenType.Real => new RealType(),
            Token.TokenType.Boolean => new BooleanType(),
            Token.TokenType.Character => new CharacterType(),
            Token.TokenType.String => new StringType(),
            Token.TokenType.Identifier => new UnknownType(t.GetValueSafe()),
            _
                => throw new NotImplementedException(
                    "Bad TokenType for generating a TypeUsage: " + t.Type
                )
        };

    private static bool GetMutability(
        VariableNode.DeclarationContext declarationContext,
        bool hasVar,
        Token? varToken
    ) =>
        declarationContext switch
        {
            VariableNode.DeclarationContext.RecordDeclaration when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in a record declaration.",
                    varToken
                ),
            VariableNode.DeclarationContext.RecordDeclaration => false,
            VariableNode.DeclarationContext.EnumDeclaration when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in an enum declaration.",
                    varToken
                ),
            VariableNode.DeclarationContext.EnumDeclaration => false,
            VariableNode.DeclarationContext.FunctionSignature when hasVar => false,
            VariableNode.DeclarationContext.FunctionSignature when !hasVar => true,
            VariableNode.DeclarationContext.VariablesLine when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in a variables line.",
                    varToken
                ),
            VariableNode.DeclarationContext.VariablesLine => false,
            VariableNode.DeclarationContext.ConstantsLine when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in a constants line.",
                    varToken
                ),
            VariableNode.DeclarationContext.ConstantsLine => true,
            _
                => throw new NotImplementedException(
                    "Invalid variable declaration context `"
                        + declarationContext
                        + "' for determining variable mutability."
                )
        };

    private List<VariableNode>? GetVariables(
        string parentModuleName,
        VariableNode.DeclarationContext declarationContext
    )
    {
        var varToken = MatchAndRemove(Token.TokenType.Var);
        var hasVar = varToken is not null;
        var isConstant = GetMutability(declarationContext, hasVar, varToken);

        // Get variable names.
        List<string> names = [];
        if (MatchAndRemove(Token.TokenType.Identifier) is { } nameToken)
        {
            ParseCommaSeparatedIdentifiers(nameToken, names);
        }
        else
        {
            return null;
        }

        // Require a colon.
        RequiresToken(Token.TokenType.Colon);

        return CreateVariables(names, isConstant, parentModuleName, declarationContext);
    }

    private void GetVariablesRecord(List<ASTNode> vars, string parentModuleName)
    {
        var newVars =
            GetVariables(parentModuleName, VariableNode.DeclarationContext.RecordDeclaration)
            ?? throw new SyntaxErrorException(
                "A record declaration needs at least one constituent member.",
                Peek(0)
            );

        do
        {
            RequiresEndOfLine();
            vars.AddRange(newVars);
            newVars = GetVariables(
                parentModuleName,
                VariableNode.DeclarationContext.RecordDeclaration
            );
        } while (newVars is not null);
    }

    private void RequiresToken(Token.TokenType tokenType)
    {
        if (MatchAndRemove(tokenType) is null)
        {
            throw new SyntaxErrorException("Expected " + tokenType, Peek(0));
        }
    }

    private Token RequiresAndReturnsToken(Token.TokenType tokenType) =>
        MatchAndRemove(tokenType)
        ?? throw new SyntaxErrorException("Expected a " + tokenType, Peek(0));

    private void ParseCommaSeparatedTokens(
        Token firstToken,
        List<Token> tokens,
        Token.TokenType[] matchAgainst
    )
    {
        tokens.Add(firstToken);

        while (MatchAndRemove(Token.TokenType.Comma) is not null)
        {
            tokens.Add(
                MatchAndRemoveMultiple(matchAgainst)
                    ?? throw new SyntaxErrorException(
                        "Expected one of: " + string.Join(", ", matchAgainst),
                        Peek(0)
                    )
            );
        }
    }

    private void ParseCommaSeparatedIdentifiers(Token firstId, List<string> idValues)
    {
        List<Token> tokens = [];
        ParseCommaSeparatedTokens(firstId, tokens, [Token.TokenType.Identifier]);
        tokens.ForEach(t => idValues.Add(t.GetValueSafe()));
    }

    private (ASTNode, ASTNode)? CheckForRange()
    {
        if (MatchAndRemove(Token.TokenType.From) is null)
        {
            return null;
        }

        var fromNode = ProcessNumericConstant(
            MatchAndRemove(Token.TokenType.Number)
                ?? throw new SyntaxErrorException("Expected a number", Peek(0))
        );

        RequiresToken(Token.TokenType.To);

        var toNode = ProcessNumericConstant(
            MatchAndRemove(Token.TokenType.Number)
                ?? throw new SyntaxErrorException("Expected a number", Peek(0))
        );
        return (fromNode, toNode);
    }

    private List<VariableNode> ProcessConstants(string? parentModuleName)
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
                    var node = ProcessNumericConstant(num);
                    retVal.Add(
                        node is FloatNode
                            ? new VariableNode()
                            {
                                InitialValue = node,
                                Type = new RealType(),
                                IsConstant = true,
                                Name = name.Value ?? "",
                                ModuleName = parentModuleName
                            }
                            : new VariableNode()
                            {
                                InitialValue = node,
                                Type = new IntegerType(),
                                IsConstant = true,
                                Name = name.Value ?? "",
                                ModuleName = parentModuleName
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
                                Type = new CharacterType(),
                                IsConstant = true,
                                Name = name.Value ?? "",
                                ModuleName = parentModuleName
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
                                    Type = new StringType(),
                                    IsConstant = true,
                                    Name = name.Value ?? "",
                                    ModuleName = parentModuleName
                                }
                            );
                        }
                        else
                        {
                            var enm = MatchAndRemove(Token.TokenType.Identifier);
                            var paren = MatchAndRemove(Token.TokenType.LeftParen);
                            if (enm != null)
                            {
                                if (paren == null)
                                {
                                    retVal.Add(
                                        new VariableNode()
                                        {
                                            InitialValue = new StringNode(enm.Value),
                                            Type = new UnknownType(enm.Value),
                                            IsConstant = true,
                                            Name = name.Value ?? "",
                                            ModuleName = parentModuleName,
                                        }
                                    );
                                }
                                else
                                    throw new Exception(
                                        "Constant records have not been implemented yet"
                                    );
                            }
                            else
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

    private ASTNode ProcessNumericConstant(Token num)
    {
        return (num.Value ?? "").Contains('.')
            ? new FloatNode(float.Parse(num.Value ?? ""))
            : new IntNode(int.Parse(num.Value ?? ""));
    }

    public ExpressionNode? ParseExpressionLine()
    {
        var retVal = Expression();
        MatchAndRemove(Token.TokenType.EndOfLine);
        return retVal;
    }

    public ExpressionNode? Expression()
    {
        var lt = Term();
        if (lt == null)
            return null;
        return ExpressionRHS(lt);
    }

    public ExpressionNode? ExpressionRHS(ExpressionNode lt)
    {
        if (MatchAndRemove(Token.TokenType.Plus) != null)
        {
            var rt = Term();
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.Plus, rt);
            return ExpressionRHS(lt);
        }
        else if (MatchAndRemove(Token.TokenType.Minus) != null)
        {
            var rt = Term();
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.Minus, rt);
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

    private ExpressionNode? Term()
    {
        var lt = Factor();
        if (lt == null)
            return null;
        return TermRHS(lt);
    }

    private ExpressionNode? TermRHS(ExpressionNode lt)
    {
        if (MatchAndRemove(Token.TokenType.Times) != null)
        {
            var rt = Factor();
            if (rt == null)
                throw new SyntaxErrorException("Expected a factor.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.Times, rt);
            return TermRHS(lt);
        }
        else if (MatchAndRemove(Token.TokenType.Divide) != null)
        {
            var rt = Factor();
            if (rt == null)
                throw new SyntaxErrorException("Expected a factor.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.Divide, rt);
            return TermRHS(lt);
        }
        else if (MatchAndRemove(Token.TokenType.Mod) != null)
        {
            var rt = Factor();
            if (rt == null)
                throw new SyntaxErrorException("Expected a factor.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.Modulo, rt);
            return TermRHS(lt);
        }
        else
        {
            return lt;
        }
    }

    private ExpressionNode? Factor()
    {
        if (MatchAndRemove(Token.TokenType.LeftParen) != null)
        {
            var exp = Expression();
            if (MatchAndRemove(Token.TokenType.RightParen) == null)
                throw new SyntaxErrorException("Expected a right paren.", Peek(0));
            return exp;
        }

        if (GetVariableUsageNode() is { } variable)
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

    // TODO: A lot of code in this method duplicates the code from where normal functions are parsed.
    private TestNode Test(string parentModuleName)
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
            var vars = GetVariables(
                parentModuleName,
                VariableNode.DeclarationContext.FunctionSignature
            );
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

        test.LocalVariables.AddRange(ProcessConstants(parentModuleName));
        test.LocalVariables.AddRange(ProcessVariables(parentModuleName));

        // Process function body and return function node.

        BodyFunction(test);
        return test;
    }

    private EnumNode MakeEnum(string? parentModuleName)
    {
        Token? token = MatchAndRemove(Token.TokenType.Identifier);
        if (token == null)
        {
            throw new SyntaxErrorException(
                "Expecting identifier after enum declaration, ",
                Peek(0)
            );
        }

        if (MatchAndRemove(Token.TokenType.Equal) == null)
        {
            throw new SyntaxErrorException("Expecting equal in enum declaration, ", Peek(0));
        }

        if (MatchAndRemove(Token.TokenType.LeftBracket) == null)
        {
            throw new SyntaxErrorException(
                "Expecting left bracket in endum declaration, ",
                Peek(0)
            );
        }

        EnumNode enumNode = new EnumNode(token.Value, parentModuleName, GetEnumElements().ToList());
        return enumNode;
    }

    private LinkedList<string> GetEnumElements()
    {
        LinkedList<string> enums = new LinkedList<string>();
        Token? token = MatchAndRemove(Token.TokenType.Identifier);
        if (token == null)
        {
            throw new SyntaxErrorException(
                "Expecting identifier after left bracket in enum declaration, ",
                Peek(0)
            );
        }

        enums.AddLast(token.Value);
        while (MatchAndRemove(Token.TokenType.Comma) != null)
        {
            token = MatchAndRemove(Token.TokenType.Identifier);
            if (token == null)
            {
                throw new SyntaxErrorException(
                    "Expecting identifier after comma in enum declaration, ",
                    Peek(0)
                );
            }

            enums.AddLast(token.Value);
        }

        if (MatchAndRemove(Token.TokenType.RightBracket) == null)
        {
            throw new SyntaxErrorException(
                "Expecting right bracket in enum declaration, ",
                Peek(0)
            );
        }

        return enums;
    }
}
