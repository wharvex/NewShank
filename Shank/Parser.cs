using System.Diagnostics;
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

    public InterptOptions? InterpreterOptions { get; set; }

    public Parser(List<Token> tokens, InterptOptions? options = null)
    {
        _tokens = tokens;
        InterpreterOptions = options;
    }

    private readonly List<Token> _tokens;

    private bool afterDefault = false;
    public static int Line { get; set; }
    public static string FileName { get; set; }

    /// <summary>
    /// <para>
    ///    Method <c>MatchAndRemove</c> removes I token if the TokenType matches
    ///    the parameter's type
    /// </para>
    /// </summary>
    /// <param name="t">TokenType passed in</param>
    /// <returns>Token's value otherwise null</returns>
    ///

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

    /// <summary>
    /// <para>
    ///     Method <c>MatchAndRemoveMultiple</c> removes multiple tokens if the TokenType
    ///     matches the array of parameters passed in
    /// </para>
    /// </summary>
    /// <param name="ts">List of TokenTypes</param>
    /// <returns>Values of the tokens removeed otherwise null</returns>

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

    /// <summary>
    /// <para>
    ///     Method <c>Peek</c> reads the value of the next value with <e>n</e> offset
    ///     and returns the value. If the offset passed in is beyond the end of the file,
    ///     the function may return null.
    /// </para>
    /// </summary>
    /// <param name="offset">Number of characters you are peeking ahead</param>
    /// <returns>Token value or else null</returns>

    private Token? Peek(int offset)
    {
        return _tokens.Count > offset ? _tokens[offset] : null;
    }

    /// <summary>
    /// <para>
    ///     Method <c>PeekSafe</c> reads the value of the next value with <e>n</e> offset
    ///     and returns the value fetched from <e>Peek</e>. If it returns null, an exception is thrown.
    ///     See Peek documentation for null cases.
    /// </para>
    /// </summary>
    /// <param name="offset">Number of characters you are peeking ahead</param>
    /// <returns>Token value</returns>
    /// <exception cref="SyntaxErrorException">If the offset is at or past the end of the file</exception>

    private Token PeekSafe(int offset) =>
        Peek(offset) ?? throw new SyntaxErrorException("Unexpected EOF", null);

    /// <summary>
    /// <para>
    ///     Method <c>ExpectsEndOfLine</c> reads attempts to match the next token to EndOfLine.
    ///     If found, the Token is returned and removed from our list. If not null is returned.
    /// </para>
    /// </summary>
    /// <returns>Token value otherwise null</returns>
    private bool ExpectsEndOfLine()
    {
        var ret = MatchAndRemove(Token.TokenType.EndOfLine) is not null;

        // TODO
        // Uncomment this part when we implement the overhaul described in MatchAndRemove. For now,
        // this code would be redundant.

        if (ret)
        {
            ConsumeBlankLines();
        }

        return ret;
    }

    /// <summary>
    ///     <para>
    ///     Method <c>ConsumeBlankLines</c> removes the next token repeatedly until its type
    ///     no longer matches EndOfLine
    ///     </para>
    /// </summary>

    private void ConsumeBlankLines()
    {
        while (MatchAndRemove(Token.TokenType.EndOfLine) is not null) { }
    }

    /// <summary>
    ///     <para>
    ///     Method <c>RequiresEndOfLine</c> checks the return value of <c>RequiresEndOfLine</c>.
    ///     If not present an exception is thrown.
    ///     </para>
    /// </summary>
    /// <exception cref="SyntaxErrorException">An EndOfLine token is not encountered</exception>

    private void RequiresEndOfLine()
    {
        if (!ExpectsEndOfLine())
        {
            throw new SyntaxErrorException("Expected end of line", Peek(0));
        }
    }

    /// <summary>
    ///     <para>
    ///     Method <c>GetVariableUsagePlainNode</c> parses a variable usage according to the
    ///     following syntax example:
    ///
    ///             IDENTIFIER[expression]
    ///
    ///                 or
    ///
    ///             IDENTIFIER.reference
    ///     </para>
    /// </summary>
    /// <returns>new VariableUsagePlainNode otherwise exception</returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Need an expression after the left bracket!</description>
    ///         </item>
    ///         <item>
    ///             <description>Need a right bracket after the expression!</description>
    ///         </item>
    ///         <item>
    ///             <description>Need a record member reference after the dot!</description>
    ///         </item>
    ///     </list>
    /// </exception>

    private VariableUsagePlainNode? GetVariableUsagePlainNode()
    {
        //array index indentifier case
        if (MatchAndRemove(Token.TokenType.Identifier) is { } id)
        {
            if (MatchAndRemove(Token.TokenType.LeftBracket) is not null)
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

                return new VariableUsagePlainNode(
                    id.GetValueSafe(),
                    exp,
                    VariableUsagePlainNode.VrnExtType.ArrayIndex
                );
            }

            //record member case
            if (MatchAndRemove(Token.TokenType.Dot) is not null)
            {
                VariableUsagePlainNode? varRef = GetVariableUsagePlainNode();

                if (varRef is null)
                    throw new SyntaxErrorException(
                        "Need a record member reference after the dot!",
                        Peek(0)
                    );

                return new VariableUsagePlainNode(
                    id.GetValueSafe(),
                    varRef,
                    VariableUsagePlainNode.VrnExtType.RecordMember
                );
            }

            //return the variable name as is
            return new VariableUsagePlainNode(id.GetValueSafe());
        }

        return null;
    }

    /// <summary>
    ///     <para>
    ///    Method <c>GetVariableUsageNode</c> parses nested variable usages according to the
    ///    following syntax example:
    ///
    ///             IDENTIFIER[expression][expression]...
    ///
    ///                 or
    ///
    ///             IDENTIFIER.reference.reference...
    ///    </para>
    /// </summary>
    /// <returns>VariableUsageTempNode or VariableUsageIndexNode or VariableUsageMemberNode else exception</returns>
    /// <exception cref="SyntaxErrorException">Expression is not present in an array index</exception>
    /// <exception cref="UnreachableException">if neither an index or member is encountered (should never be reached)</exception>
    private VariableUsageNodeTemp? GetVariableUsageNode()
    {
        //get the identifier
        var vupToken = MatchAndRemove(Token.TokenType.Identifier);

        //return null if not found
        if (vupToken is null)
        {
            return null;
        }

        //get the first plain variable usage
        var vupNode = new VariableUsagePlainNode(vupToken.GetValueSafe());

        //remove either a left bracket or a dot
        var nextToken = MatchAndRemoveMultiple(Token.TokenType.LeftBracket, Token.TokenType.Dot);

        //returns the plain node if their is no more tokens
        if (nextToken is null)
        {
            return vupNode;
        }

        //store our plain node for now
        VariableUsageNodeTemp left = vupNode;

        //store our complete node once we get it
        VariableUsageNodeTemp leftRight;

        while (true)
        {
            switch (nextToken.Type)
            {
                //if variable is array accessor
                case Token.TokenType.LeftBracket:
                    leftRight = new VariableUsageIndexNode(
                        left,
                        Expression()
                            ?? throw new SyntaxErrorException("Expected expression", Peek(0))
                    );
                    RequiresToken(Token.TokenType.RightBracket);
                    break;
                //if variable uses a member record
                case Token.TokenType.Dot:
                    leftRight = new VariableUsageMemberNode(left, MemberAccess());
                    break;
                //hopefully we don't get here
                default:
                    throw new UnreachableException();
            }

            //check again for another array accessor or member record
            nextToken = MatchAndRemoveMultiple(Token.TokenType.LeftBracket, Token.TokenType.Dot);

            //just return what we have if nothing is present
            if (nextToken is null)
            {
                return leftRight;
            }

            //else make the new left what we currently have and start the while loop over
            left = leftRight;
        }
    }

    /// <summary>
    ///     <para>
    ///         Method <c>MemberAccess</c> creates a new MemberAccessNode assuming that the next token
    ///         is an identifier. For exception cases, please refer to the documentation of <c>RequiresAndReturnsToken</c>
    ///     </para>
    /// </summary>
    /// <returns>new MemberAccessNode</returns>

    private MemberAccessNode MemberAccess() =>
        new MemberAccessNode(RequiresAndReturnsToken(Token.TokenType.Identifier).GetValueSafe());

    /// <summary>
    ///     <para>
    ///         Method <c>Module</c> 
    ///     </para>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="SyntaxErrorException"></exception>
    /// <exception cref="NotImplementedException"></exception>
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
                //if we have exports add them to the module
                case Token.TokenType.Export:
                    ret.addExportNames(Export());
                    break;
                //module contains import list
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

                //module contains function definition
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
            var vars = GetVariables(
                moduleName,
                VariableDeclarationNode.DeclarationContext.FunctionSignature
            );
            done = vars == null;
            if (vars != null)
            {
                funcNode.ParameterVariables.AddRange(vars);
                MatchAndRemove(Token.TokenType.Semicolon);
            }
        }

        afterDefault = false;

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

    private List<VariableDeclarationNode> BodyRecord(string parentModule)
    {
        //StatementsBody(record.Members, true);
        List<ASTNode> bodyContents = [];
        Body(bodyContents, parentModule, GetVariablesRecord);
        return bodyContents.Cast<VariableDeclarationNode>().ToList();
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

        if (InterpreterOptions is not null)
        {
            OutputHelper.DebugPrintTxt(InterpreterOptions.VuOpTest.ToString(), "vuop");
        }
        return (
                InterpreterOptions is not null && InterpreterOptions.VuOpTest
                    ? NewAssignment()
                    : Assignment()
            )
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
        var type = Type(VariableDeclarationNode.DeclarationContext.RecordDeclaration);

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
    private Type Type(VariableDeclarationNode.DeclarationContext declarationContext)
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
            // we cannot check unknown type for refersTo being on enum, but if we have refersTo integer we can check that at parse time
            Token.TokenType.RefersTo
                => new ReferenceType(
                    Type(declarationContext) is UnknownType u
                        ? u
                        : throw new SyntaxErrorException(
                            "attempted to use refersTo (dynamic memory management) on non record type",
                            typeToken
                        )
                ),
            _ => throw new SyntaxErrorException("Unknown type", typeToken)
        };
    }

    static IEnumerable<T> Repeat<T>(Func<T> generator)
    {
        yield return generator();
    }

    private Type CustomType(
        VariableDeclarationNode.DeclarationContext declarationContext,
        Token typeToken
    )
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
        VariableDeclarationNode.DeclarationContext declarationContext,
        Token? arrayToken
    )
    {
        var range = CheckRangeInner(false, Range.DefaultSmallInteger());
        if (
            range is null
            && declarationContext == VariableDeclarationNode.DeclarationContext.VariablesLine
        )
        {
            throw new SyntaxErrorException(
                "Array in variables declared without a size",
                arrayToken
            );
        }

        var _ =
            MatchAndRemove(Token.TokenType.Of)
            ?? throw new SyntaxErrorException("Array declared without type missing of", arrayToken);

        return new ArrayType(Type(declarationContext), range);
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
        while (ExpectsEndOfLine() == false)
        {
            var isVariable = MatchAndRemove(Token.TokenType.Var) != null;
            var variable = GetVariableUsagePlainNode();
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

        RequiresEndOfLine();

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
            RequiresEndOfLine();

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

        RequiresEndOfLine();

        var statements = new List<StatementNode>();
        StatementsBody(statements);
        return new WhileNode(boolExp, statements);
    }

    private StatementNode? Repeat()
    {
        if (MatchAndRemove(Token.TokenType.Repeat) == null)
            return null;

        RequiresEndOfLine();

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

        RequiresEndOfLine();

        return new RepeatNode(boolExp, statements);
    }

    private StatementNode? For()
    {
        if (MatchAndRemove(Token.TokenType.For) == null)
            return null;
        var indexVariable = GetVariableUsagePlainNode();
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

        RequiresEndOfLine();

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
            var target = GetVariableUsagePlainNode();
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

    private StatementNode? NewAssignment()
    {
        OutputHelper.DebugPrintTxt("hello from new assignment", "vuop", true);
        if (
            Peek(1)?.Type
            is Token.TokenType.Assignment
                or Token.TokenType.LeftBracket
                or Token.TokenType.Dot
        )
        {
            var target = GetVariableUsagePlainNode();
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
    /// <para>
    /// Process an arbitrary number of consecutive variables declaration lines (i.e. lines that
    /// start with the 'variables' keyword).
    /// </para>
    /// </summary>
    /// <param name="parentModule">The parent module passed in as a string</param>
    /// <returns>List of variable declarations</returns>
    private List<VariableDeclarationNode> ProcessVariables(string parentModule)
    {
        var retVal = new List<VariableDeclarationNode>();

        while (MatchAndRemove(Token.TokenType.Variables) != null)
        {
            var nextOnes = GetVariables(
                parentModule,
                VariableDeclarationNode.DeclarationContext.VariablesLine
            );

            // TODO: We are potentially adding null to retVal here.
            retVal.AddRange(nextOnes);

            RequiresEndOfLine();
        }

        return retVal;
    }

    private List<VariableDeclarationNode> ProcessVariablesDoWhile(string parentModule)
    {
        var retVal = new List<VariableDeclarationNode>();

        do
        {
            var nextOnes = GetVariables(
                parentModule,
                VariableDeclarationNode.DeclarationContext.VariablesLine
            );

            // TODO: List.AddRange throws an ArgumentNullException if nextOnes is null.
            retVal.AddRange(nextOnes);

            RequiresEndOfLine();
        } while (MatchAndRemove(Token.TokenType.Variables) != null);

        return retVal;
    }

    private List<VariableDeclarationNode> CreateVariables(
        List<string> names,
        bool isConstant,
        string parentModuleName,
        VariableDeclarationNode.DeclarationContext declarationContext
    )
    {
        var type = Type(declarationContext);

        if (MatchAndRemove(Token.TokenType.Equal) == null)
        {
            if (!afterDefault)
                return CreateVariablesBasic(names, isConstant, parentModuleName, type);
            throw new SyntaxErrorException(
                "Only default values can be after a default value in a function declaration",
                Peek(0)
            );
        }

        if (declarationContext == VariableDeclarationNode.DeclarationContext.VariablesLine)
        {
            // Creates the variables with their default values added unto them
            return CreateDefaultVariables(names, isConstant, parentModuleName, type);
        }

        if (declarationContext == VariableDeclarationNode.DeclarationContext.FunctionSignature)
        {
            afterDefault = true;
            return CreateDefaultVariables(names, isConstant, parentModuleName, type);
        }
        return CreateVariablesBasic(names, isConstant, parentModuleName, type);
    }

    private List<VariableDeclarationNode> CreateVariablesBasic(
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
                    new VariableDeclarationNode()
                    {
                        IsConstant = isConstant,
                        Type = type,
                        Name = n,
                        ModuleName = parentModuleName,
                    }
            )
            .ToList();
    }

    private List<VariableDeclarationNode> CreateDefaultVariables(
        List<string> names,
        bool isConstant,
        string parentModuleName,
        Type type
    )
    {
        var expression = Expression();
        if (expression == null)
            throw new SyntaxErrorException(
                "Found an assignment without a valid right hand side.",
                Peek(0)
            );

        // ranges parsed in the type
        return names
            .Select(
                n =>
                    new VariableDeclarationNode()
                    {
                        IsConstant = isConstant,
                        Type = type,
                        Name = n,
                        ModuleName = parentModuleName,
                        InitialValue = expression,
                        IsDefaultValue = true
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

    /// <summary>
    ///     <para> 
    ///         Method <c>GetMutability</c> determines the ability for an object to change 
    ///         once it is created by reading the declaration context and determinning 
    ///         whether the variable type is inferred.
    ///     </para>
    /// </summary>
    /// <param name="declarationContext">How the variable declaration is being used</param>
    /// <param name="hasVar">whether or not "var" keyword is present (inferred data type)</param>
    /// <param name="varToken">the variable value itself (used in exceptions)</param>
    /// <returns>boolean true or false</returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Keyword `var' not allowed in a record declaration.</description>
    ///         </item>
    ///         <item> 
    ///             <description>Keyword `var' not allowed in an enum declaration.</description>
    ///         </item>
    ///         <item> 
    ///             <description>Keyword `var' not allowed in a variables line.</description>
    ///         </item>
    ///         <item> 
    ///             <description>Keyword `var' not allowed in a constants line.</description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="NotImplementedException">A valid variable context was not reached</exception>
    private static bool GetMutability(
        VariableDeclarationNode.DeclarationContext declarationContext,
        bool hasVar,
        Token? varToken
    ) =>
        //read this basically as a switch statement with each lambda expression
        //representing a "case"
        declarationContext switch
        {
            //when the declaration context is a Record Declaration and "var"keyword is present
            VariableDeclarationNode.DeclarationContext.RecordDeclaration when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in a record declaration.",
                    varToken
                ),
            //when the declaration context is a Record Declaration
            VariableDeclarationNode.DeclarationContext.RecordDeclaration => false,

            //when the declartion context is an Enum Declaration and "var" keyword is present
            VariableDeclarationNode.DeclarationContext.EnumDeclaration when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in an enum declaration.",
                    varToken
                ),

            //when the declaration context is a Enum Declaration
            VariableDeclarationNode.DeclarationContext.EnumDeclaration => false,

            //when the declartion context is a Function Signature and "var" keyword is present
            VariableDeclarationNode.DeclarationContext.FunctionSignature when hasVar => false,

            //when the declartion context is a Function Signature and "var" keyword is absent
            VariableDeclarationNode.DeclarationContext.FunctionSignature when !hasVar => true,

            //when the declaration context is a Variables Line and "var" keyword is present
            VariableDeclarationNode.DeclarationContext.VariablesLine when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in a variables line.",
                    varToken
                ),

            //when the declaration context is a Variables Line
            VariableDeclarationNode.DeclarationContext.VariablesLine => false,

            //when the declaration context is a Constants Line and "var" keyword is present
            VariableDeclarationNode.DeclarationContext.ConstantsLine when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in a constants line.",
                    varToken
                ),

            //when declaration context is a Constants Line 
            VariableDeclarationNode.DeclarationContext.ConstantsLine => true,
            _
            //default case
                => throw new NotImplementedException(
                    "Invalid variable declaration context `"
                        + declarationContext
                        + "' for determining variable mutability."
                )
        };

    /// <summary>
    ///     <para>
    ///          Method <c>GetVariables</c>    
    ///     </para>
    /// </summary>
    /// <param name="parentModuleName"></param>
    /// <param name="declarationContext"></param>
    /// <returns></returns>
    private List<VariableDeclarationNode>? GetVariables(
        string parentModuleName,
        VariableDeclarationNode.DeclarationContext declarationContext
    )
    {
        //remove the variable
        var varToken = MatchAndRemove(Token.TokenType.Var);
        var hasVar = varToken is not null;

        //determine if the variable is immutable (remains constant)
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
            GetVariables(
                parentModuleName,
                VariableDeclarationNode.DeclarationContext.RecordDeclaration
            )
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
                VariableDeclarationNode.DeclarationContext.RecordDeclaration
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

    /// <summary>
    ///     Method <c>RequiresAndReturnsToken</c> attempts to MatchAndRemove a token if its TokenType matches
    ///     the type that is passed in 
    /// </summary>
    /// <param name="tokenType">TokenType expected</param>
    /// <returns>Expected Token</returns>
    /// <exception cref="SyntaxErrorException">TokenTypes do not match</exception>

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

    /// <summary>
    ///     <para>
    ///     Method <c>ParseCommaSeparatedIdentifiers</c> 
    ///     </para>
    /// </summary>
    /// <param name="firstId"></param>
    /// <param name="idValues"></param>

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

    private List<VariableDeclarationNode> ProcessConstants(string? parentModuleName)
    {
        var retVal = new List<VariableDeclarationNode>();
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
                            ? new VariableDeclarationNode()
                            {
                                InitialValue = node,
                                Type = new RealType(),
                                IsConstant = true,
                                Name = name.Value ?? "",
                                ModuleName = parentModuleName
                            }
                            : new VariableDeclarationNode()
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
                            new VariableDeclarationNode()
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
                                new VariableDeclarationNode()
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
                                        new VariableDeclarationNode()
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

            RequiresEndOfLine();
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
        RequiresEndOfLine();
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

        if (GetVariableUsagePlainNode() is { } variable)
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

    /// <summary>
    ///     Method <c>Export</c> matches and removes a list of exports (identifiers) and returns the list for use
    /// </summary>
    /// <returns>LinkedList of export values (string)</returns>
    /// <exception cref="SyntaxErrorException">An export call is not followed by an identifier</exception>
    private LinkedList<string> Export()
    {
        //matches and removes the identifier
        var token = MatchAndRemove(Token.TokenType.Identifier);

        //if no identifier is found or the identifier does not have a defined value 
        if (token == null || token.Value == null)
            throw new SyntaxErrorException(
                "An export call must be followed by an identifier, not ",
                Peek(0)
            );


        LinkedList<string> exports = new LinkedList<string>();

        //add the token's value to the export list
        exports.AddLast(token.Value);

        //while a comma is present 
        while (MatchAndRemove(Token.TokenType.Comma) != null)
        {
            //match and remove the next identifier
            token = MatchAndRemove(Token.TokenType.Identifier);

            //check to see if the identifier is actually present
            if (token == null || token.Value == null)
                throw new SyntaxErrorException(
                    "An comma in an export call must be followed by an identifer, ",
                    Peek(0)
                );

            //add it to exports
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

    /// <summary>
    /// <para>
    ///     Method <c>checkForFunctions</c> reads an import statement and determines the functions
    ///     associated with it. These functions are removed, stored, and returned in a list
    /// </para>
    /// </summary>
    /// <returns>LinkedList of import functions</returns>
    /// <exception cref="SyntaxErrorException">
    /// <list type="bullet"> 
    ///     <item>
    ///         <description>Brackets do not contain a list of functions</description>
    ///     </item>
    ///     <item>
    ///         <description>Any two function names aren't separated by a comma</description>
    ///     </item>
    /// </list>
    /// </exception>
    private LinkedList<string> checkForFunctions()
    {
        var functionsToImport = new LinkedList<string>();

        MatchAndRemove(Token.TokenType.LeftBracket);


        //while there is more in the import list
        while (MatchAndRemove(Token.TokenType.RightBracket) == null)
        {
            //m,atch and remove the method identifier
            var token = MatchAndRemove(Token.TokenType.Identifier);

            if (token == null || token.Value == null)
            {
                throw new SyntaxErrorException(
                    "Expecting an identifer after a left bracket in an import statement, not ",
                    Peek(0)
                );
            }

            //add the import function to our list
            functionsToImport.AddLast(token.Value);


            //match and remove hte next comma if there are more methods to import
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
                VariableDeclarationNode.DeclarationContext.FunctionSignature
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

        RequiresEndOfLine();

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
