using System.Diagnostics;
using Optional;
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
        Token.TokenType.Constants,
        Token.TokenType.Export,
        Token.TokenType.Import,
        Token.TokenType.Test
    ];

    public InterpretOptions? ActiveInterpretOptions { get; set; }

    public bool GetVuopTestFlag()
    {
        return ActiveInterpretOptions?.VuOpTest ?? false;
    }

    public Parser(List<Token> tokens, InterpretOptions? options = null)
    {
        _tokens = tokens;
        ActiveInterpretOptions = options;
        if (GetVuopTestFlag())
            OutputHelper.DebugPrintTxt("starting vuop test debug output", "vuop");
    }

    private readonly List<Token> _tokens;

    private bool afterDefault = false;
    public static int Line { get; set; }
    public static string FileName { get; set; }

    public delegate StatementNode? StatementGetter(string moduleName);

    /// <summary>
    ///    Method <c>MatchAndRemove</c> removes I token if the TokenType matches
    ///    the parameter's type
    /// </summary>
    /// <param name="t">TokenType passed in</param>
    /// <returns>Token's value (<see cref="Token"/>) otherwise null</returns>

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
    ///     Method <c>MatchAndRemoveMultiple</c> removes multiple tokens if the TokenType
    ///     matches the array of parameters passed in
    /// </summary>
    /// <param name="ts">List of TokenTypes</param>
    /// <returns>Values of the tokens removed (<see cref="Token"/>) otherwise null</returns>

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
    ///     Method <c>Peek</c> reads the value of the next value with <e>n</e> offset
    ///     and returns the value. If the offset passed in is beyond the end of the file,
    ///     the function may return null.
    /// </summary>
    /// <param name="offset">Number of characters you are peeking ahead</param>
    /// <returns>Token value (<see cref="Token"/>) or else null</returns>

    private Token? Peek(int offset)
    {
        return _tokens.Count > offset ? _tokens[offset] : null;
    }

    /// <summary>
    ///     Method <c>PeekSafe</c> reads the value of the next value with <e>n</e> offset
    ///     and returns the value fetched from <see cref="Peek"/>. If it returns null, an exception is thrown.
    ///     See Peek documentation for null cases.
    /// </summary>
    /// <param name="offset">Number of characters you are peeking ahead</param>
    /// <returns>Token value (<see cref="Token"/>)</returns>
    /// <exception cref="SyntaxErrorException">If the offset is at or past the end of the file</exception>

    private Token PeekSafe(int offset) =>
        Peek(offset) ?? throw new SyntaxErrorException("Unexpected EOF", null);

    /// <summary>
    ///     Method <c>ExpectsEndOfLine</c> reads attempts to match the next token to EndOfLine.
    ///     If found, the Token is returned and removed from our list. If not null is returned.
    /// </summary>
    /// <returns>Value representing if a EndOfLine token is present (<see cref="bool"/>)</returns>
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
    ///     Method <c>ConsumeBlankLines</c> removes the next token repeatedly until its type
    ///     no longer matches EndOfLine
    /// </summary>

    private void ConsumeBlankLines()
    {
        while (MatchAndRemove(Token.TokenType.EndOfLine) is not null) { }
    }

    /// <summary>
    ///     Method <c>RequiresEndOfLine</c> checks the return value of <c>RequiresEndOfLine</c>.
    ///     If not present an exception is thrown.
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
    ///     Method <c>GetVariableUsagePlainNode</c> parses a variable usage according to the
    ///     following syntax example:
    ///
    ///             IDENTIFIER[expression]
    ///
    ///                 or
    ///
    ///             IDENTIFIER.reference
    /// </summary>
    /// <returns>new <see cref="VariableUsagePlainNode"/> otherwise exception</returns>
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

    private VariableUsagePlainNode? GetVariableUsagePlainNode(string moduleName)
    {
        //array index indentifier case
        if (MatchAndRemove(Token.TokenType.Identifier) is { } id)
        {
            if (MatchAndRemove(Token.TokenType.LeftBracket) is not null)
            {
                var exp = Expression(moduleName);

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
                    VariableUsagePlainNode.VrnExtType.ArrayIndex,
                    moduleName
                );
            }

            //record member case
            if (MatchAndRemove(Token.TokenType.Dot) is not null)
            {
                VariableUsagePlainNode? varRef = GetVariableUsagePlainNode(moduleName);

                if (varRef is null)
                    throw new SyntaxErrorException(
                        "Need a record member reference after the dot!",
                        Peek(0)
                    );

                return new VariableUsagePlainNode(
                    id.GetValueSafe(),
                    varRef,
                    VariableUsagePlainNode.VrnExtType.RecordMember,
                    moduleName
                );
            }

            //return the variable name as is
            return new VariableUsagePlainNode(id.GetValueSafe(), moduleName);
        }

        return null;
    }

    /// <summary>
    ///    Method <c>GetVariableUsageNode</c> parses nested variable usages according to the
    ///     following syntax example:
    ///
    ///             IDENTIFIER[expression][expression]...
    ///
    ///                 or
    ///
    ///             IDENTIFIER.reference.reference...
    /// </summary>
    /// <returns><see cref="VariableUsageTempNode"/> or <see cref="VariableUsageIndexNode"/> or <see cref="VariableUsageMemberNode"/> else exception</returns>
    /// <exception cref="SyntaxErrorException">Expression is not present in an array index</exception>
    /// <exception cref="UnreachableException">if neither an index or member is encountered (should never be reached)</exception>
    private VariableUsageNodeTemp? GetVariableUsageNode(string moduleName)
    {
        //get the identifier
        var vupToken = MatchAndRemove(Token.TokenType.Identifier);

        //return null if not found
        if (vupToken is null)
        {
            return null;
        }

        //get the first plain variable usage
        var vupNode = new VariableUsagePlainNode(vupToken.GetValueSafe(), moduleName);

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
                        Expression(moduleName)
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
    ///     Method <c>MemberAccess</c> creATES MemberAccessNode containing the value of the next token
    /// </summary>
    /// <returns> <see cref="MemberAccessNode"/> with value</returns>

    private MemberAccessNode MemberAccess() =>
        new MemberAccessNode(RequiresAndReturnsToken(Token.TokenType.Identifier).GetValueSafe());

    /// <summary>
    ///     Method <c>Module</c> parses a module and consumes constructs contained within
    /// </summary>
    /// <returns><see cref="ModuleNode"/> containing the constructs as its contents</returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>If a module identifier is not present</description>
    ///         </item>
    ///         <item>
    ///             <description>If an indent-zero token to start a construct is not found</description>
    ///         </item>
    ///         <item>
    ///             <description>If an import does not have an identifier</description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="NotImplementedException">The context for our indent-zero token does not exist</exception>
    public ModuleNode Module()
    {
        //Get the module name if there is one, or set it to default.
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
                    ret.AddToGlobalVariables(ProcessVariablesDoWhile(moduleName, true));
                    break;
                case Token.TokenType.Constants:
                    ret.AddToGlobalVariables(ProcessConstantsDoWhile(moduleName, true));
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

    /// <summary>
    ///     <para>
    ///         Method <c>Function</c> parses a function and returns a node contain its identifier, parameters, and body contents.
    ///         An extension for each function name is created based on its parameters so overloads don't produce name collisions.
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The module that is processed along with the function(string)</param>
    /// <returns> <see cref="FunctionNode"/> containing the functions contents</returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>A functions name is not found</description>
    ///         </item>
    ///         <item>
    ///             <description>Functions parameters are not closed in parenthesis</description>
    ///         </item>
    ///     </list>
    /// </exception>

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
                false,
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
        // funcNode.OverloadNameExt = overloadNameExt;

        funcNode.LineNum = Peek(0).LineNumber;

        RequiresToken(Token.TokenType.RightParen);

        funcNode.GenericTypeParameterNames = ParseGenericKeywordAndTypeParams() ?? [];

        RequiresEndOfLine();

        // Process local variables .
        funcNode.LocalVariables.AddRange(ProcessConstants(moduleName));
        funcNode.LocalVariables.AddRange(ProcessVariables(moduleName, false));

        // Process function body and return function node.
        BodyFunction(funcNode, moduleName);

        return funcNode;
    }

    /// <summary>
    ///     <para>
    ///         Method <c>Record</c> parses a record along consisting of its identifier, parameters, and body contents
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the parent module</param>
    /// <returns>A <see cref="RecordNode"/> containing its contents</returns>

    private RecordNode Record(string moduleName)
    {
        //identifier of the record is caught
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

    /// <summary>
    ///     <para>
    ///         Method <c>ParseGenericKeywordAndTypeParams</c> parses a generic keyword along with its list of type parameters (identifiers)
    ///     </para>
    /// </summary>
    /// <returns> <see cref="List{T} "/> (<see cref="string"/>) of type parameters</returns>
    /// <exception cref="SyntaxErrorException">A generic is not immediately followed by an identifier token</exception>
    private List<string>? ParseGenericKeywordAndTypeParams()
    {
        //match and remove the generic
        if (MatchAndRemove(Token.TokenType.Generic) is null)
        {
            return null;
        }

        //find the identifier
        var typeParam =
            MatchAndRemove(Token.TokenType.Identifier)
            ?? throw new SyntaxErrorException("Expected an identifier", Peek(0));

        //parse the list of identifiers
        List<string> ret = [];
        ParseCommaSeparatedIdentifiers(typeParam, ret);

        return ret;
    }

    /// <summary>
    ///     <para>
    ///         Method <c>BodyFunction</c> parses and returns a body function
    ///     </para>
    /// </summary>
    /// <param name="function">The function to be parsed</param>
    /// <param name="moduleName">The parent module to whcih the function belongs to</param>

    private void BodyFunction(FunctionNode function, string moduleName)
    {
        StatementsBody(function.Statements, moduleName);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>BodyRecord</c> parses the body of a record and returns its contents
    ///     </para>
    /// </summary>
    /// <param name="parentModule">The name of the parent module</param>
    /// <returns><see cref="List{T}"/> (<see cref="VariableDeclarationNode"/>) of the contents of the body</returns>

    private List<VariableDeclarationNode> BodyRecord(string parentModule)
    {
        //StatementsBody(record.Members, true);
        List<ASTNode> bodyContents = [];
        Body(bodyContents, parentModule, GetVariablesRecord);
        return bodyContents.Cast<VariableDeclarationNode>().ToList();
    }

    /// <summary>
    ///     Method <c>StatementsBody</c> parses and returns a statement body
    /// </summary>
    /// <param name="statements">The template for the list of statements contained within the StatementBody</param>
    /// <param name="moduleName">The name of the parent module to which the statement body belongs</param>

    private void StatementsBody(List<StatementNode> statements, string moduleName)
    {
        RequiresToken(Token.TokenType.Indent);

        Statements(statements, moduleName);

        RequiresToken(Token.TokenType.Dedent);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>Body</c> parses the contents of a generic body
    ///     </para>
    /// </summary>
    /// <param name="bodyContents">Template for the contents of the body</param>
    /// <param name="parentModuleName">The name of the parent module</param>
    /// <param name="bodyContentsParser">The parsable body contents (refer to <c>BodyRecord</c> method for source)</param>
    private void Body(
        List<ASTNode> bodyContents,
        string parentModuleName,
        Action<List<ASTNode>, string> bodyContentsParser
    )
    {
        RequiresToken(Token.TokenType.Indent);

        //fancy method encapsulation that generates the parameters for the method passed in as bodyContentsParser
        bodyContentsParser(bodyContents, parentModuleName);

        RequiresToken(Token.TokenType.Dedent);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>Statements</c> parses and returns a list of statements
    ///     </para>
    /// </summary>
    /// <param name="statements">Our template provided to build our list of statements on</param>
    /// <param name="moduleName">The name of the parent module to which the list of statements belongs</param>
    private void Statements(List<StatementNode> statements, string moduleName)
    {
        //parse while we have more statements
        do
        {
            var s = Statement(moduleName);

            if (s is null)
            {
                break;
            }

            statements.Add(s);
        } while (true);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>Statement</c> parses and returns a statement based on the type
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the parent module to which the statement belongs</param>
    /// <returns><see cref="AssignmentNode"/> or <see cref="StringNode"/> or <see cref="RepeatNode"/> or <see cref="ForNode"/> or <see cref="IfNode"/> or <see cref="FunctionCallNode"/> containg the contents of the statement</returns>
    private StatementNode? Statement(string moduleName)
    {
        StatementGetter assignment = GetVuopTestFlag() ? NewAssignment : Assignment;
        StatementGetter functionCall = GetVuopTestFlag() ? NewFunctionCall : FunctionCall;
        return assignment(moduleName)
            ?? While(moduleName)
            ?? Repeat(moduleName)
            ?? For(moduleName)
            ?? If(moduleName)
            ?? functionCall(moduleName);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>GetDataTypeFromConstantNodeType</c> gets the data type of the object passed
    ///         in by returning a new type object
    ///     </para>
    /// </summary>
    /// <param name="constantNode">The node passed in</param>
    /// <returns><see cref="IntegerType"/> or <see cref="RealType"/> or <see cref="StringType"/> or <see cref="CharacterType"/> or <see cref="BooleanType"/> representing the type of the object passed in</returns>
    /// <exception cref="InvalidOperationException">If the type of the node passed in cannot be read</exception>
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

    /// <summary>
    ///     <para>
    ///         Method <c>Type</c> reads a valid construct type
    ///     </para>
    /// </summary>
    /// <param name="declarationContext">The declaration context for the construct</param>
    /// <returns>A <see cref="Shank.Type"/> object representing the type of the parsed construct</returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>If a valid type declartion is not found</description>
    ///         </item>
    ///         <item>
    ///             <description>If refersTo is being used on a non-record type</description>
    ///         </item>
    ///     </list>
    /// </exception>

    private Type Type(VariableDeclarationNode.DeclarationContext declarationContext)
    {
        //matching and removing multiple of a valid type token
        var typeToken =
            MatchAndRemoveMultiple(_shankTokenTypesPlusIdentifier)
            ?? throw new SyntaxErrorException("expected start of a type", Peek(0));

        //switch statement on how to handle the type token
        return typeToken.Type switch
        {
            //if the type is a real number create a new RealType object
            //Range is checked against float values
            Token.TokenType.Real
                => new RealType(CheckRange((_, _) => Option.None<string>(), RealType.DefaultRange)),

            //custom type is returned if an identifier is found
            Token.TokenType.Identifier
                => CustomType(declarationContext, typeToken),

            //IntegerType is returned if integer type is found
            Token.TokenType.Integer
                => new IntegerType(CheckRange(NormalRangeVerifier, IntegerType.DefaultRange)),

            //BooleanType is returned if boolean type is found
            Token.TokenType.Boolean
                => new BooleanType(),
            Token.TokenType.Character
                => new CharacterType(CheckRange(NormalRangeVerifier, CharacterType.DefaultRange)),
            Token.TokenType.String
                => new StringType(CheckRange(NormalRangeVerifier, StringType.DefaultRange)),
            Token.TokenType.Array => ArrayTypeParser(declarationContext, typeToken),
            // we cannot check unknown type for refersTo being on enum, but if we have refersTo integer we can check that at parse time
            Token.TokenType.RefersTo
                => ReferenceType(declarationContext, typeToken),
            _ => throw new SyntaxErrorException("Unknown type", typeToken)
        };
    }

    private ReferenceType ReferenceType(
        VariableDeclarationNode.DeclarationContext declarationContext,
        Token typeToken
    )
    {
        var innerType = Type(declarationContext);
        return new ReferenceType(
            innerType is UnknownType or ArrayType
                ? innerType
                : throw new SyntaxErrorException(
                    "attempted to use refersTo (dynamic memory management) on non record or record type",
                    typeToken
                )
        );
    }

    /// <summary>
    ///     <para>
    ///         Method <c>Repeat</c> returns the parameter passed in while a next iteration is present
    ///     </para>
    /// </summary>
    /// <typeparam name="T">The type to be used for our generator</typeparam>
    /// <param name="generator">Contents of the next iteration to whatever was passed in</param>
    /// <returns>Content of the generator as an <see cref="IEnumerable{T}"/></returns>
    static IEnumerable<T> Repeat<T>(Func<T> generator)
    {
        yield return generator();
    }

    /// <summary>
    ///     <para>
    ///         Method <c>CustomType</c> parses a list of type parameters to a custom type and if present and returns them as a collection contained\
    ///         within a generic unknown type
    ///     </para>
    /// </summary>
    /// <param name="declarationContext">The context of the type identifier</param>
    /// <param name="typeToken">The type token encountered</param>
    /// <returns><see cref="UnknownType"/> token containing its value and parameters (custom type)</returns>
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

    /// <summary>
    ///     <para>
    ///         Method <c>InBetweenOpt</c>
    ///     </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="first"></param>
    /// <param name="parser"></param>
    /// <param name="last"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="SyntaxErrorException"></exception>

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

    private ArrayType ArrayTypeParser(
        VariableDeclarationNode.DeclarationContext declarationContext,
        Token? arrayToken
    )
    {
        var range =
            CheckRangeInner(NormalRangeVerifier, ArrayType.DefaultRange)
            ?? throw new SyntaxErrorException(
                "Array in variables declared without a size",
                arrayToken
            );

        var _ =
            MatchAndRemove(Token.TokenType.Of)
            ?? throw new SyntaxErrorException("Array declared without type missing of", arrayToken);

        return new ArrayType(Type(declarationContext), range);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>CheckRange</c> checks the range passed in by using the functionality provided by <c>CheckRangeInner</c>.
    ///         NOTE: Is this function necessary?
    ///     </para>
    /// </summary>
    /// <param name="isFloat">Whether or not the range provided is measured using float values</param>
    /// <param name="defaultRange">The expected default range</param>
    /// <returns><see cref="Range"/> object returned from <c>CheckRangeInner</c></returns>
    private Range CheckRange(
        Func<ExpressionNode, ExpressionNode, Option<string>> rangeVerifier,
        Range defaultRange
    )
    {
        return CheckRangeInner(rangeVerifier, defaultRange) ?? defaultRange;
    }

    private Option<string> NormalRangeVerifier(ExpressionNode to, ExpressionNode from)
    {
        return Option
            .Some("Expected integer type limits found float ones")
            .Filter(to is FloatNode || from is FloatNode);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>CheckRangeInner</c> parses a range expression (see Shank documentation) and determines whether or not it is valid. If so the range is returned.
    ///         The types of range values should be consistent (FROM float TO float / FROM int TO int).
    ///     </para>
    /// </summary>
    /// <param name="isFloat">If the range requires bounds of float type</param>
    /// <param name="defaultRange">The default range expected</param>
    /// <returns><see cref="Range"/> object containing its upper and lower bounds</returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>If the value for our lower bound is not a number</description>
    ///         </item>
    ///             <description>If the value for our upper bound is not a number</description>
    ///         <item>
    ///             <description>If a integer range was expected but the bounds are floats</description>
    ///         </item>
    ///         <item>
    ///             <description>If the lower bound is outside of the expected range</description>
    ///         </item>
    ///         <item>
    ///             <description>If the upper bound is outside of the expected range</description>
    ///         </item>
    ///     </list>
    /// </exception>
    private Range? CheckRangeInner(
        Func<ExpressionNode, ExpressionNode, Option<string>> rangeVerifier,
        Range defaultRange
    )
    {
        // remove the `from  token which signifies the start of a range
        // - if it is not there return null
        // - it is up to the caller to determine what they want to do if there is no range
        if (MatchAndRemove(Token.TokenType.From) is null)
        {
            return null;
        }

        //lower bound of range
        var fromToken = MatchAndRemove(Token.TokenType.Number);

        //the beginning number is processed and turned into an appropriate node
        var fromNode = ProcessNumericConstant(
            fromToken ?? throw new SyntaxErrorException("Expected a number", Peek(0))
        );

        //look for the To token to indicate range
        RequiresToken(Token.TokenType.To);

        //the end of our range
        var toToken = MatchAndRemove(Token.TokenType.Number);

        //the end of the range is processed and turned into an appropriate node
        var toNode = ProcessNumericConstant(
            toToken ?? throw new SyntaxErrorException("Expected a number", Peek(0))
        );

        //checks semantically to make sure our range node types are what is expected


        var verifier = rangeVerifier(fromNode, toNode);
        verifier.MatchSome(s => throw new SyntaxErrorException(s, null));

        //get the value of the float is a FloatNode type is found else get the value of the IntNode
        var fromValue = fromNode is FloatNode from ? from.Value : ((IntNode)fromNode).Value;
        var toValue = toNode is FloatNode to ? to.Value : ((IntNode)toNode).Value;

        // TODO: check range is not inverted?
        //catch the from value if invalid
        if (fromValue < defaultRange.From || fromValue > defaultRange.To)
        {
            throw new SyntaxErrorException(
                $"range starting at {fromValue} is  not valid for this type",
                fromToken
            );
        }
        //catch the tovalue if it is invalid
        if (toValue < defaultRange.From || toValue > defaultRange.To)
        {
            throw new SyntaxErrorException(
                $"range ending at {toValue} is  not valid for this type",
                toToken
            );
        }

        //return the range
        return new Range(fromValue, toValue);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>FunctionCall</c> parses and returns a function call statement
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the parent module to which the function call belongs</param>
    /// <returns><see cref="FunctionCallNode"/> containing the contents of the function call statement</returns>
    /// <exception cref="SyntaxErrorException">If an invalid constant or expression was found for a non-variable argument</exception>
    private FunctionCallNode? FunctionCall(string moduleName)
    {
        //parse the identifier
        var name = MatchAndRemove(Token.TokenType.Identifier);
        if (name == null)
            return null;

        //parse the arguments
        var arguments = new List<ExpressionNode>();

        int lineNum = name.LineNumber;

        while (ExpectsEndOfLine() == false)
        {
            //check to see if it is a variable
            var isVariable = MatchAndRemove(Token.TokenType.Var) != null;

            if (!isVariable)
            {
                //add the constant or expression to the list if not a variable
                var e = Expression(moduleName);
                if (e == null)
                    throw new SyntaxErrorException(
                        $"Expected a constant or a variable instead of {_tokens[0]}",
                        Peek(0)
                    );
                if (e is VariableUsagePlainNode variableUsagePlainNode)
                    variableUsagePlainNode.IsInFuncCallWithVar = false;
                arguments.Add(e);
            }
            else
            {
                //add the variable to list if is a variable
                var variable = GetVariableUsagePlainNode(moduleName);
                variable.IsInFuncCallWithVar = true;
                arguments.Add(variable);
            }

            MatchAndRemove(Token.TokenType.Comma);
        }

        //construct the function call
        var retVal = new FunctionCallNode(name.Value != null ? name.Value : string.Empty);
        retVal.Arguments.AddRange(arguments);
        retVal.LineNum = lineNum;

        return retVal;
    }

    private FunctionCallNode? NewFunctionCall(string moduleName)
    {
        // Get the function name.
        var name = MatchAndRemove(Token.TokenType.Identifier);
        if (name is null)
            return null;

        // Get the arguments
        var arguments = new List<ExpressionNode>();
        var lineNum = name.LineNumber;
        while (!ExpectsEndOfLine())
        {
            // Check for the `var` keyword.
            var hasVar = MatchAndRemove(Token.TokenType.Var) is not null;

            if (!hasVar)
            {
                // Require an expression.
                var e =
                    Expression(moduleName)
                    ?? throw new SyntaxErrorException("Expected a constant or a variable", Peek(0));

                // Record the `var` status.
                if (e is VariableUsageNodeTemp variableUsagePlainNode)
                    variableUsagePlainNode.NewIsInFuncCallWithVar = false;

                arguments.Add(e);
            }
            else
            {
                // Require a variable.
                var variable =
                    GetVariableUsageNode(moduleName)
                    ?? throw new SyntaxErrorException(
                        "Cannot apply `var' keyword to a function argument that is not a single variable.",
                        Peek(0)
                    );

                // Record the `var` status.
                variable.NewIsInFuncCallWithVar = true;

                arguments.Add(variable);
            }

            MatchAndRemove(Token.TokenType.Comma);
        }

        // Build the function call.
        var retVal = new FunctionCallNode(name.GetValueSafe()) { LineNum = lineNum };
        retVal.Arguments.AddRange(arguments);

        return retVal;
    }

    /// <summary>
    ///     <para>
    ///         Method <c>If</c> parses and returns an if statement
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the parent module to which the if statement belongs</param>
    /// <returns><see cref="IfNode"/> containing the contenets of the if statement</returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>A boolean expression was not found within the if conditional</description>
    ///         </item>
    ///         <item>
    ///             <description>A "then" keyword does not follow the if conditional</description>
    ///         </item>
    ///     </list>
    /// </exception>
    private StatementNode? If(string moduleName)
    {
        //parse the keyword
        if (MatchAndRemove(Token.TokenType.If) == null)
            return null;

        //parse the conditional
        // var boolExp = BooleanExpression(moduleName);
        //
        // if (boolExp == null)
        //     throw new SyntaxErrorException("Expected a boolean expression in the if.", Peek(0));
        var expression = Expression(moduleName);

        if (expression == null)
            throw new SyntaxErrorException("Expected a boolean expression in the if.", Peek(0));

        //parse "then" keyword
        if (MatchAndRemove(Token.TokenType.Then) == null)
            throw new SyntaxErrorException("Expected a then in the if.", Peek(0));

        //body statements are on the next line
        RequiresEndOfLine();

        //template for our body of statements
        var body = new List<StatementNode>();

        //parse our body of statements
        StatementsBody(body, moduleName);
        //return new IfNode(boolExp, body, ElseAndElseIf(moduleName));
        return new IfNode(expression, body, ElseAndElseIf(moduleName));
    }

    private IfNode? ElseAndElseIf(string moduleName)
    {
        if (MatchAndRemove(Token.TokenType.Elsif) != null)
        {
            var boolExp =
                BooleanExpression(moduleName)
                ?? throw new SyntaxErrorException(
                    "Expected a boolean expression in the elsif.",
                    Peek(0)
                );
            RequiresToken(Token.TokenType.Then);
            RequiresEndOfLine();
            var body = new List<StatementNode>();
            StatementsBody(body, moduleName);
            return new IfNode(boolExp, body, ElseAndElseIf(moduleName));
        }

        if (MatchAndRemove(Token.TokenType.Else) != null)
        {
            RequiresEndOfLine();

            var body = new List<StatementNode>();
            StatementsBody(body, moduleName);
            return new ElseNode(body);
        }

        return null;
    }

    /// <summary>
    ///     <para>
    ///         Method <c>While</c> parses and returns a while statement
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the parent module to which the while statement belongs</param>
    /// <returns> <see cref="WhileNode"/> containing the contents of the while statement</returns>

    private StatementNode? While(string moduleName)
    {
        //parse the keyword
        if (MatchAndRemove(Token.TokenType.While) == null)
            return null;

        //parse the boolean expression
        // var boolExp = BooleanExpression(moduleName);
        var expression = Expression(moduleName);

        if (expression == null)
            throw new SyntaxErrorException(
                "Expected a boolean expression at the end of the repeat.",
                Peek(0)
            );

        //make sure we put the body content on a new line
        RequiresEndOfLine();

        //template for our parsed statements
        var statements = new List<StatementNode>();

        //parse the statments using the template
        StatementsBody(statements, moduleName);

        // return new WhileNode(boolExp, statements);
        return new WhileNode(expression, statements);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>Repeat</c> parses and returns a repeat statement
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of hte parent module to which the repeat statement belongs</param>
    /// <returns><see cref="RepeatNode"/> containing the contents of the repeat statement</returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>An until statement does not exist at the end of the repeat</description>
    ///         </item>
    ///         <item>
    ///             <description>A boolean expression does not follow an "until" keyword</description>
    ///         </item>
    ///     </list>
    /// </exception>

    private StatementNode? Repeat(string moduleName)
    {
        //parse the keyword
        if (MatchAndRemove(Token.TokenType.Repeat) == null)
            return null;

        //make sure our statements are on the next line
        RequiresEndOfLine();

        //template for our statements
        var statements = new List<StatementNode>();

        //parse the body full of statements
        StatementsBody(statements, moduleName);

        //find the UNTIL keyword
        if (MatchAndRemove(Token.TokenType.Until) == null)
            throw new SyntaxErrorException("Expected an until to end the repeat.", Peek(0));

        //parse a boolean expression
        // var boolExp = BooleanExpression(moduleName);

        var expression = Expression(moduleName);

        if (expression == null)
            throw new SyntaxErrorException(
                "Expected a boolean expression at the end of the repeat.",
                Peek(0)
            );

        //make sure other statements are on lines following
        RequiresEndOfLine();

        return new RepeatNode(expression, statements);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>For</c> parses and returns a for statement
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the parent module to which the for statement belongs</param>
    /// <returns><see cref="ForNode"/> containing the contents of the for statement</returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>A variable is not used in the for statement's conditional</description>
    ///         </item>
    ///         <item>
    ///             <description>A "from" keyword is not present in the for statement's conditional</description>
    ///         </item>
    ///         <item>
    ///             <description>A lower bound expression is not present in the for statement's conditional </description>
    ///         </item>
    ///         <item>
    ///             <description>A "to" keyword is not present in the for statement's conditional</description>
    ///         </item>
    ///         <item>
    ///             <description>An upper bound expression is not present in the for statement's conditional</description>
    ///         </item>
    ///     </list>
    /// </exception>

    private StatementNode? For(string moduleName)
    {
        //parse the keyword
        if (MatchAndRemove(Token.TokenType.For) == null)
            return null;

        //get the variable to be used for the for conditionals
        var indexVariable = GetVuopTestFlag()
            ? GetVariableUsageNode(moduleName)
            : GetVariableUsagePlainNode(moduleName);
        if (indexVariable == null)
            throw new SyntaxErrorException("Expected a variable in the for statement.", Peek(0));

        if (MatchAndRemove(Token.TokenType.From) == null)
            throw new SyntaxErrorException("Expected a from in the for statement.", Peek(0));

        //parse the lower bound's expression
        var fromExp = Expression(moduleName);
        if (fromExp == null)
            throw new SyntaxErrorException(
                "Expected a from expression in the for statement.",
                Peek(0)
            );
        if (MatchAndRemove(Token.TokenType.To) == null)
            throw new SyntaxErrorException("Expected a to in the for statement.", Peek(0));

        //parse the upper bound's expression
        var toExp = Expression(moduleName);
        if (toExp == null)
            throw new SyntaxErrorException(
                "Expected a to expression in the for statement.",
                Peek(0)
            );

        //require the body statements to be on the next line
        RequiresEndOfLine();

        //template for our body statements
        var statements = new List<StatementNode>();

        //parse the statements using our template
        StatementsBody(statements, moduleName);
        return GetVuopTestFlag()
            ? new ForNode(fromExp, toExp, statements, indexVariable)
            : new ForNode((VariableUsagePlainNode)indexVariable, fromExp, toExp, statements);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>BooleanExpression</c> parses an returns an expression but only if it is a boolean expression
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The parent module's name to whcih the boolean expression belongs to</param>
    /// <returns><see cref="BooleanExpressionNode"/> containing the boolean expression's contents</returns>
    /// <exception cref="SyntaxErrorException">If the expression parsed does not match the format of a boolean expression</exception>
    private BooleanExpressionNode BooleanExpression(string moduleName)
    {
        //expression is parsed
        var expression = Expression(moduleName);

        //check to see if the expression matches a boolean expression format
        if (expression is not BooleanExpressionNode ben)
            throw new SyntaxErrorException("Expected a boolean expression", Peek(0));
        return ben;
    }

    /// <summary>
    ///     <para>
    ///         Method <c>Assignment</c> parses an assignment if it is present
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the parent module containing the assignment</param>
    /// <returns>new <see cref="AssignmentNode"/> else <see cref="null"/></returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>A valid identifier is not found</description>
    ///         </item>
    ///         <item>
    ///             <description>A valid expression is not found</description>
    ///         </item>
    ///     </list>
    /// </exception>
    private StatementNode? Assignment(string moduleName)
    {
        if (
            Peek(1)?.Type
            is Token.TokenType.Assignment
                or Token.TokenType.LeftBracket
                or Token.TokenType.Dot
        )
        {
            //get the variable side
            var target = GetVariableUsagePlainNode(moduleName);
            if (target == null)
                throw new SyntaxErrorException(
                    "Found an assignment without a valid identifier.",
                    Peek(0)
                );

            //parse the assignment operator
            MatchAndRemove(Token.TokenType.Assignment);

            //parse the expression side
            var expression = ParseExpressionLine(moduleName);
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
    ///     <para>
    ///         Method <c>NewAssignment</c> parses an assignment if it is present
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the parent module to which the assigment belongs</param>
    /// <returns>new <see cref="AssignmentNode"/> containing its contents else <see cref="null"/></returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>If a variable usage was expected but not found</description>
    ///         </item>
    ///         <item>
    ///             <description>If an expression was expected but not found</description>
    ///         </item>
    ///     </list>
    /// </exception>
    private StatementNode? NewAssignment(string moduleName)
    {
        //makes sure our assignment is on the current line being parsed
        if (!FindBeforeEol(Token.TokenType.Assignment))
            return null;

        //the variable is parsed
        var target =
            GetVariableUsageNode(moduleName)
            ?? throw new SyntaxErrorException("Expected variable usage", Peek(0));

        //the assignment (:=) is parsed
        RequiresToken(Token.TokenType.Assignment);

        //the expression is parsed
        var expression =
            ParseExpressionLine(moduleName)
            ?? throw new SyntaxErrorException("Expected expression", Peek(0));

        //new assignment node containing the contents
        return new AssignmentNode(target, expression, true);
    }

    /// <summary>
    ///     Method <c>FindBeforeEol</c> checks to see if the token matching the type passed in is before the next EndOfLine token
    /// </summary>
    /// <param name="tokenType">The desired token's type</param>
    /// <returns>true if found or else false (<see cref="bool"/>)</returns>
    private bool FindBeforeEol(Token.TokenType tokenType)
    {
        var i = 0;
        var next = Peek(i);

        // Short-circuit if this line needs to fall out of "Statement" first.
        if (next?.Type == Token.TokenType.Dedent)
            return false;

        //looks to see if the required token is before an the end of the line
        while (next is not null && next.Type != Token.TokenType.EndOfLine)
        {
            if (next.Type == tokenType)
            {
                return true;
            }

            next = Peek(++i);
        }

        return false;
    }

    /// <summary>
    ///     Process an arbitrary number of consecutive variables declaration lines (i.e. lines that
    ///     start with the 'variables' keyword).
    /// </summary>
    /// <param name="parentModule">The parent module to which the variable declarations belong</param>
    /// <returns>A <see cref="List{T}"/> (<see cref="VariableDeclarationNode"/>) containing the variable declarations</returns>
    private List<VariableDeclarationNode> ProcessVariables(string parentModule, bool isGlobal)
    {
        var retVal = new List<VariableDeclarationNode>();

        while (MatchAndRemove(Token.TokenType.Variables) != null)
        {
            var nextOnes = GetVariables(
                parentModule,
                isGlobal,
                VariableDeclarationNode.DeclarationContext.VariablesLine
            );

            // TODO: We are potentially adding null to retVal here.
            retVal.AddRange(nextOnes);

            RequiresEndOfLine();
        }

        return retVal;
    }

    /// <summary>
    ///     <para>
    ///         Method <c>ProcessVaraiblesDoWhile</c> parses a list of variable declarations while more are found and returns them in a list
    ///     </para>
    /// </summary>
    /// <param name="parentModule">The parent module to which the declaration belongs</param>
    /// <param name="isGlobal">Whether or not the list of variable declarations is global</param>
    /// <returns><see cref="List{T}"/> (<see cref="VariableDeclarationNode"/>) of variable declarations containing the contents of each declaration encoutnered</returns>
    private List<VariableDeclarationNode> ProcessVariablesDoWhile(
        string parentModule,
        bool isGlobal
    )
    {
        var retVal = new List<VariableDeclarationNode>();

        //parse variable declarations until none are found
        do
        {
            var nextOnes = GetVariables(
                parentModule,
                isGlobal,
                VariableDeclarationNode.DeclarationContext.VariablesLine
            );

            // TODO: List.AddRange throws an ArgumentNullException if nextOnes is null.
            retVal.AddRange(nextOnes);

            RequiresEndOfLine();
        } while (MatchAndRemove(Token.TokenType.Variables) != null);

        return retVal;
    }

    /// <summary>
    ///     <para>
    ///         Method <c>CreateVariables</c> creates a list of vraibel declarations given the names, properties and context of the variables
    ///     </para>
    /// </summary>
    /// <param name="names">A list of the variable names to be created</param>
    /// <param name="isConstant">Whether or not the list of variables is set to remain constant</param>
    /// <param name="isGlobal">Whether or not the list of variables is global</param>
    /// <param name="parentModuleName">The parent modules name to which the list of variables belongs</param>
    /// <param name="declarationContext">The context to which the list of variables is declared</param>
    /// <returns><see cref="List{T}"/> (<see cref="VariableDeclarationNode"/>) of newly created variable declarations</returns>
    /// <exception cref="SyntaxErrorException">A default value is not found following a default value declaration</exception>
    private List<VariableDeclarationNode> CreateVariables(
        List<string> names,
        bool isConstant,
        bool isGlobal,
        string parentModuleName,
        VariableDeclarationNode.DeclarationContext declarationContext
    )
    {
        var type = Type(declarationContext);

        if (MatchAndRemove(Token.TokenType.Equal) == null)
        {
            if (!afterDefault)
                return CreateVariablesBasic(names, isConstant, isGlobal, parentModuleName, type);
            throw new SyntaxErrorException(
                "Only default values can be after a default value in a function declaration",
                Peek(0)
            );
        }

        if (declarationContext == VariableDeclarationNode.DeclarationContext.VariablesLine)
        {
            // Creates the variables with their default values added unto them
            return CreateDefaultVariables(names, isConstant, isGlobal, parentModuleName, type);
        }

        if (declarationContext == VariableDeclarationNode.DeclarationContext.FunctionSignature)
        {
            afterDefault = true;
            return CreateDefaultVariables(names, isConstant, isGlobal, parentModuleName, type);
        }
        return CreateVariablesBasic(names, isConstant, isGlobal, parentModuleName, type);
    }

    /// <summary>
    ///     <para>
    ///
    ///     </para>
    /// </summary>
    /// <param name="names">A list of variable names to be created</param>
    /// <param name="isConstant">Whether or not the list of variables is set to remain constant</param>
    /// <param name="isGlobal">Whether or not the list of variables is global</param>
    /// <param name="parentModuleName">The parent modules name to which the list of variables belongs</param>
    /// <param name="type"></param>
    /// <returns></returns>
    private List<VariableDeclarationNode> CreateVariablesBasic(
        List<string> names,
        bool isConstant,
        bool isGlobal,
        string parentModuleName,
        Type type
    )
    {
        // ranges parsed in the type
        return names
            .Select(
                n => new VariableDeclarationNode(isConstant, type, n, parentModuleName, isGlobal)
            )
            .ToList();
    }

    /// <summary>
    ///     <para>
    ///         Method <c>CreateDefaultVariables</c>
    ///     </para>
    /// </summary>
    /// <param name="names"></param>
    /// <param name="isConstant"></param>
    /// <param name="isGlobal"></param>
    /// <param name="parentModuleName"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="SyntaxErrorException"></exception>
    private List<VariableDeclarationNode> CreateDefaultVariables(
        List<string> names,
        bool isConstant,
        bool isGlobal,
        string parentModuleName,
        Type type
    )
    {
        var expression = Expression(parentModuleName);
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
                        IsDefaultValue = true,
                        IsGlobal = isGlobal,
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
    ///         Method <c>GetMutability</c> determines if a character will remain constant based on its context and whether
    ///         or not the "var" keyword is present
    ///     </para>
    /// </summary>
    /// <param name="declarationContext">The context of the variable declaration</param>
    /// <param name="hasVar">If a "var" keyword is present</param>
    /// <param name="varToken">A token containing the "var" keyword (used for error messages)</param>
    /// <returns><c>true</c> if the character is mutable or else <c>false</c> (<see cref="bool"/>) </returns>
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
    /// <exception cref="NotImplementedException">An invalid variable declaration context has been reached</exception>
    private static bool GetMutability(
        VariableDeclarationNode.DeclarationContext declarationContext,
        bool hasVar,
        Token? varToken
    ) =>
        declarationContext switch
        {
            VariableDeclarationNode.DeclarationContext.RecordDeclaration when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in a record declaration.",
                    varToken
                ),
            VariableDeclarationNode.DeclarationContext.RecordDeclaration => false,
            VariableDeclarationNode.DeclarationContext.EnumDeclaration when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in an enum declaration.",
                    varToken
                ),
            VariableDeclarationNode.DeclarationContext.EnumDeclaration => false,
            VariableDeclarationNode.DeclarationContext.FunctionSignature when hasVar => false,
            VariableDeclarationNode.DeclarationContext.FunctionSignature when !hasVar => true,
            VariableDeclarationNode.DeclarationContext.VariablesLine when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in a variables line.",
                    varToken
                ),
            VariableDeclarationNode.DeclarationContext.VariablesLine => false,
            VariableDeclarationNode.DeclarationContext.ConstantsLine when hasVar
                => throw new SyntaxErrorException(
                    "Keyword `var' not allowed in a constants line.",
                    varToken
                ),
            VariableDeclarationNode.DeclarationContext.ConstantsLine => true,
            _
                => throw new NotImplementedException(
                    "Invalid variable declaration context `"
                        + declarationContext
                        + "' for determining variable mutability."
                )
        };

    /// <summary>
    ///     <para>
    ///         Method <c>GetVariables</c> parses a comma separated list of variables and creates them
    ///     </para>
    /// </summary>
    /// <param name="parentModuleName">the module in which the variables reside</param>
    /// <param name="declarationContext">the conext of the variable declaration (used to get the mutability of the variable)</param>
    /// <returns><see cref="List{T}"/> (<see cref="VariableDeclarationNode"/>) of variable declarations</returns>
    private List<VariableDeclarationNode>? GetVariables(
        string parentModuleName,
        bool isGlobal,
        VariableDeclarationNode.DeclarationContext declarationContext
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

        return CreateVariables(names, isConstant, isGlobal, parentModuleName, declarationContext);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>GetVariablesRecord</c> finds all variables and their defined ranges and constructs them
    ///     </para>
    /// </summary>
    /// <param name="vars">List of variables in the record</param>
    /// <param name="parentModuleName">The name of the parent module to which the record belongs</param>
    /// <exception cref="SyntaxErrorException">If the record does not contain any constituent members</exception>

    private void GetVariablesRecord(List<ASTNode> vars, string parentModuleName)
    {
        //get all variable declarations for the record
        var newVars =
            GetVariables(
                parentModuleName,
                false,
                VariableDeclarationNode.DeclarationContext.RecordDeclaration
            )
            ?? throw new SyntaxErrorException(
                "A record declaration needs at least one constituent member.",
                Peek(0)
            );

        do
        {
            RequiresEndOfLine();

            //range is defined for each variable
            vars.AddRange(newVars);

            //get the rest of the variables until we have none
            newVars = GetVariables(
                parentModuleName,
                false,
                VariableDeclarationNode.DeclarationContext.RecordDeclaration
            );
        } while (newVars is not null);
    }

    /// <summary>
    ///     <para>
    ///         Method <c>RequiresToken</c> attempts to match and remove the next token if it matches the tokentype passed in. If not, an exception is thrown.
    ///     </para>
    /// </summary>
    /// <param name="tokenType">The inputted tokentype</param>
    /// <exception cref="SyntaxErrorException">If the tokentype passed in does not match the expected tokentype</exception>

    private void RequiresToken(Token.TokenType tokenType)
    {
        if (MatchAndRemove(tokenType) is null)
        {
            throw new SyntaxErrorException("Expected " + tokenType, Peek(0));
        }
    }

    /// <summary>
    ///     <para>
    ///         Method <c>RequiresAndReturnsToken</c> return a token if the TokenType matches that of the Token passed in
    ///     </para>
    /// </summary>
    /// <param name="tokenType">TokenType of the requested token</param>
    /// <returns><see cref="Token"/> otherwise throw an exception</returns>
    /// <exception cref="SyntaxErrorException">If the TokenType passed in does not match the TokenType of the next Token</exception>

    private Token RequiresAndReturnsToken(Token.TokenType tokenType) =>
        MatchAndRemove(tokenType)
        ?? throw new SyntaxErrorException("Expected a " + tokenType, Peek(0));

    /// <summary>
    ///     <para>
    ///         Method <c>ParseCommaSeparatedTokens</c> parses a list of tokens separated by commas that matches the list of token types passed in
    ///     </para>
    /// </summary>
    /// <param name="firstToken">A Token representing the first Token in the list</param>
    /// <param name="tokens">Tokens representing the remainder of our list</param>
    /// <param name="matchAgainst">List of TokenTypes to match against</param>
    /// <exception cref="SyntaxErrorException">If the TokenType of a Token in the list doesn't match what was expected</exception>
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
    ///         Method <c>ParseCommaSeparatedIdentifiers</c> parses a comma separated list of identifiers
    ///     </para>
    /// </summary>
    /// <param name="firstId">The Token of the first identifier in the list</param>
    /// <param name="idValues">The values of the remaining list of identifiers (string)</param>

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

    private List<VariableDeclarationNode> ProcessConstants(
        string? parentModuleName,
        bool isGlobal = false
    )
    {
        var retVal = new List<VariableDeclarationNode>();
        while (MatchAndRemove(Token.TokenType.Constants) != null)
        {
            retVal.AddRange(ProcessConstant(parentModuleName, isGlobal));
        }

        return retVal;
    }

    private List<VariableDeclarationNode> ProcessConstantsDoWhile(
        string? parentModuleName,
        bool isGlobal = false
    )
    {
        var retVal = new List<VariableDeclarationNode>();
        do
        {
            retVal.AddRange(ProcessConstant(parentModuleName, isGlobal));
        } while (MatchAndRemove(Token.TokenType.Constants) != null);

        return retVal;
    }

    private List<VariableDeclarationNode> ProcessConstant(
        string? parentModuleName,
        bool isGlobal = false
    )
    {
        var retVal = new List<VariableDeclarationNode>();
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
                            IsGlobal = isGlobal,
                            ModuleName = parentModuleName
                        }
                        : new VariableDeclarationNode()
                        {
                            InitialValue = node,
                            Type = new IntegerType(),
                            IsConstant = true,
                            Name = name.Value ?? "",
                            IsGlobal = isGlobal,
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
                            IsGlobal = isGlobal,
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
                                IsGlobal = isGlobal,
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
                                        IsGlobal = isGlobal,
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
        return retVal;
    }

    /// <summary>
    ///     <para>
    ///         Method <c>ProcessNumericConstant</c> processes a number that is passed in and returns an appropriate node based on whether or not
    ///         the number contains a decimal point. If a value in the number is absent, empty space is returned.
    ///     </para>
    /// </summary>
    /// <param name="num">The number being processed</param>
    /// <returns><see cref="FloatNode"/> or <see cref="IntNode"/> containing the value of the number</returns>
    private ExpressionNode ProcessNumericConstant(Token num)
    {
        return (num.Value ?? "").Contains('.')
            ? new FloatNode(float.Parse(num.Value ?? ""))
            : new IntNode(int.Parse(num.Value ?? ""));
    }

    public ExpressionNode? ParseExpressionLine(string moduleName)
    {
        var retVal = Expression(moduleName);
        RequiresEndOfLine();
        return retVal;
    }

    /// <summary>
    ///     <para>
    ///         Method <c>Expression</c> parses and returns an expression
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the parent module to which the expression belongs</param>
    /// <returns>An <see cref="ExpressionNode"/> containing the expression contents</returns>
    public ExpressionNode? Expression(string moduleName)
    {
        //get the term in the expression
        var lt = Term(moduleName);
        if (lt == null)
            return null;
        return ExpressionRHS(lt, moduleName);
    }

    public ExpressionNode? ExpressionRHS(ExpressionNode lt, string moduleName)
    {
        if (MatchAndRemove(Token.TokenType.Plus) != null)
        {
            var rt = Term(moduleName);
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.Plus, rt);
            return ExpressionRHS(lt, moduleName);
        }
        else if (MatchAndRemove(Token.TokenType.Minus) != null)
        {
            var rt = Term(moduleName);
            if (rt == null)
                throw new SyntaxErrorException("Expected a term.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.Minus, rt);
            return ExpressionRHS(lt, moduleName);
        }
        else if (MatchAndRemove(Token.TokenType.LessEqual) != null)
        {
            var rt = Term(moduleName);
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
            var rt = Term(moduleName);
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
            var rt = Term(moduleName);
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
            var rt = Term(moduleName);
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
            var rt = Term(moduleName);
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
            var rt = Term(moduleName);
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

    /// <summary>
    ///     <para>
    ///         Method <c>Term</c> parses and returns a term
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the parent module to which the term belongs</param>
    /// <returns>A <see cref="ExpressionNode"/> containing the terms contents</returns>
    private ExpressionNode? Term(string moduleName)
    {
        //get the factor in the term
        var lt = Factor(moduleName);
        if (lt == null)
            return null;
        return TermRHS(lt, moduleName);
    }

    private ExpressionNode? TermRHS(ExpressionNode lt, string moduleName)
    {
        if (MatchAndRemove(Token.TokenType.Times) != null)
        {
            var rt = Factor(moduleName);
            if (rt == null)
                throw new SyntaxErrorException("Expected a factor.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.Times, rt);
            return TermRHS(lt, moduleName);
        }
        else if (MatchAndRemove(Token.TokenType.Divide) != null)
        {
            var rt = Factor(moduleName);
            if (rt == null)
                throw new SyntaxErrorException("Expected a factor.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.Divide, rt);
            return TermRHS(lt, moduleName);
        }
        else if (MatchAndRemove(Token.TokenType.Mod) != null)
        {
            var rt = Factor(moduleName);
            if (rt == null)
                throw new SyntaxErrorException("Expected a factor.", Peek(0));
            lt = new MathOpNode(lt, MathOpNode.MathOpType.Modulo, rt);
            return TermRHS(lt, moduleName);
        }
        else
        {
            return lt;
        }
    }

    /// <summary>
    ///     <para>
    ///         Method <c>Factor</c> parses a factor and returns it
    ///     </para>
    /// </summary>
    /// <param name="moduleName">The name of the parent module to which the factor belongs</param>
    /// <returns><see cref="ExpressionNode"/> or <see cref="VariableUsageNodeTemp"/> or <see cref="StringNode"/> or <see cref="CharNode"/> or <see cref="BoolNode"/> or <see cref="FloatNode"/> or <see cref="IntNode"/> containing the factor's contents</returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>If a left parenthesis is not followed by a right parenthesis in a parenthesis enclosed expression</description>
    ///         </item>
    ///         <item>
    ///             <description>If a character constant is invalid</description>
    ///         </item>
    ///     </list>
    /// </exception>
    private ExpressionNode? Factor(string moduleName)
    {
        //parse an expression inside of parenthesis
        if (MatchAndRemove(Token.TokenType.LeftParen) != null)
        {
            var exp = Expression(moduleName);
            if (MatchAndRemove(Token.TokenType.RightParen) == null)
                throw new SyntaxErrorException("Expected a right paren.", Peek(0));
            return exp;
        }

        if (GetVuopTestFlag())
        {
            if (GetVariableUsageNode(moduleName) is { } v)
            {
                return v;
            }
        }
        else
        {
            if (GetVariableUsagePlainNode(moduleName) is { } variable)
            {
                return variable;
            }
        }

        //if it is a string literal
        if (MatchAndRemove(Token.TokenType.StringContents) is { } sc)
        {
            return new StringNode(sc.Value ?? string.Empty);
        }

        //if it is character contents
        if (MatchAndRemove(Token.TokenType.CharContents) is { } cc)
        {
            if (cc.Value is null || cc.Value.Length != 1)
                throw new SyntaxErrorException($"Invalid character constant {cc.Value}", Peek(0));
            return new CharNode(cc.Value[0]);
        }

        //if it is a boolean true
        if (MatchAndRemove(Token.TokenType.True) is { })
            return new BoolNode(true);

        //if it is a boolean false
        if (MatchAndRemove(Token.TokenType.False) is { })
            return new BoolNode(false);

        //parse the number before the type is checked
        var token = MatchAndRemove(Token.TokenType.Number);
        if (token == null || token.Value == null)
            return null;

        //if it is a float
        if (token.Value.Contains("."))
            return new FloatNode(float.Parse(token.Value));

        //else return an integer
        return new IntNode(int.Parse(token.Value));
    }

    /// <summary>
    ///     Method <c>Export</c> parses a list of export identifiers which are separated by commas
    /// </summary>
    /// <returns><see cref="LinkedList{T}"/> (<see cref="string"/>) of export identifiers</returns>
    /// <exception cref="SyntaxErrorException">
    /// <list type="bullet">
    ///     <item>
    ///         <description>Export call is not followed by an identifier</description>
    ///     </item>
    ///     <item>
    ///         <description>Comma in an export call is not followed by another identifier</description>
    ///     </item>
    /// </list>
    /// </exception>

    //private string? Export()
    private LinkedList<string> Export()
    {
        //match and remove its identifier
        var token = MatchAndRemove(Token.TokenType.Identifier);

        //catch if missing
        if (token == null || token.Value == null)
            throw new SyntaxErrorException(
                "An export call must be followed by an identifier, not ",
                Peek(0)
            );

        //parses our list of exports (comma separated)
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

    /// <summary>
    ///     <para>
    ///         Method <c>checkForFunctions</c> parses a list of imports and adds the a list which is returned
    ///     </para>
    /// </summary>
    /// <returns><see cref="LinkedList{T}"/> (<see cref="string"/>) of import statements </returns>
    /// <exception cref="SyntaxErrorException">
    ///     <list>
    ///         <item>
    ///             <desciption>An import function list does not begin with an identifier</desciption>
    ///         </item>
    ///         <item>
    ///             <description>A function identifier list is not separated by commas</description>
    ///         </item>
    ///     </list>
    /// </exception>

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

        test = new TestNode(testName, parentModuleName, token.Value);
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
                false,
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
        test.LocalVariables.AddRange(ProcessVariables(parentModuleName, false));

        // Process function body and return function node.

        BodyFunction(test, parentModuleName);
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
