using System.Text.Json;
using LLVMSharp;

namespace Shank;

public class Parser
{
    // Only TokenTypes used to declare variables of that type appear in this list. The TokenTypes
    // for 'record' and 'enum' are not in this list because user-defined identifiers are used to
    // declare their types.
    private readonly Token.TokenType[] _shankTokenTypes =
    [
        Token.TokenType.Integer,
        Token.TokenType.Real,
        Token.TokenType.Boolean,
        Token.TokenType.Character,
        Token.TokenType.String,
        Token.TokenType.Array
    ];

    private readonly Token.TokenType[] _shankTokenTypesMinusArray =
    [
        Token.TokenType.Integer,
        Token.TokenType.Real,
        Token.TokenType.Boolean,
        Token.TokenType.Character,
        Token.TokenType.String
    ];

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

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    private readonly List<Token> _tokens;

    public static int Line { get; set; }

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
        while (MatchAndRemove(Token.TokenType.EndOfLine) is not null) { }
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
                return new VariableReferenceNode(
                    id.GetValueSafe(),
                    exp,
                    ASTNode.VrnExtType.ArrayIndex
                );
            }
            if (MatchAndRemove(Token.TokenType.Dot) is not null)
            {
                VariableReferenceNode? varRef = GetVariableReferenceNode();
                if (varRef is null)
                    throw new SyntaxErrorException(
                        "Need a record member reference after the dot!",
                        Peek(0)
                    );
                return new VariableReferenceNode(
                    id.GetValueSafe(),
                    varRef,
                    ASTNode.VrnExtType.RecordMember
                );
            }
            return new VariableReferenceNode(id.GetValueSafe());
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
                if (PeekAndCheck(1).Type == Token.TokenType.LeftBracket)
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
            else if (MatchAndRemove(Token.TokenType.Enum) != null)
                module.addEnum(MakeEnum(moduleName));
            else if (PeekSafe(0).Type == Token.TokenType.Variables)
                module.AddToGlobalVariables(ProcessVariables(moduleName));
            else
            {
                throw new SyntaxErrorException(
                    "Any statement at indent zero must begin with the keyword "
                        + "`import`, `export`, `define`, `enum`, `test`, `record`, or `variables`."
                        + " The following is invalid: ",
                    Peek(0)
                );
            }
        }
        return module;
    }

    public FunctionNode? Function(string? moduleName)
    {
        // If moduleName is null, set it to "default".
        moduleName ??= "default";

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
            var vars = GetVariables(moduleName);
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

        // Process local variables.
        funcNode.LocalVariables.AddRange(ProcessConstants(moduleName));
        funcNode.LocalVariables.AddRange(ProcessVariables(moduleName));

        // Process function body and return function node.
        BodyFunction(funcNode);
        return funcNode;
    }

    private RecordNode Record(string moduleName)
    {
        var name =
            MatchAndRemove(Token.TokenType.Identifier)
            ?? throw new SyntaxErrorException("Expected a record name", Peek(0));

        var genericTypeParameterNames = ParseGenericKeywordAndTypeParams();

        RequiresEndOfLine();

        var recNode = new RecordNode(name.GetValueSafe(), moduleName, genericTypeParameterNames);
        BodyRecord(recNode);
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

    private RecordMemberNode? RecordMember()
    {
        var typeToken = MatchAndRemoveMultiple(_shankTokenTypesPlusIdentifier);
        if (typeToken is null)
        {
            return null;
        }

        var nameToken =
            MatchAndRemove(Token.TokenType.Identifier)
            ?? throw new SyntaxErrorException("Expected a name", Peek(0));

        RequiresEndOfLine();

        return _shankTokenTypes.Contains(typeToken.Type)
            ? new RecordMemberNode(
                nameToken.GetValueSafe(),
                GetDataTypeFromTokenType(typeToken.Type)
            )
            : new RecordMemberNode(nameToken.GetValueSafe(), typeToken.GetValueSafe());
    }

    public static VariableNode.DataType GetDataTypeFromConstantNodeType(ASTNode constantNode) =>
        constantNode switch
        {
            IntNode => VariableNode.DataType.Integer,
            FloatNode => VariableNode.DataType.Real,
            StringNode => VariableNode.DataType.String,
            CharNode => VariableNode.DataType.Character,
            BooleanExpressionNode or BoolNode => VariableNode.DataType.Boolean,
            _
                => throw new InvalidOperationException(
                    "Bad constant node type for converting to data type"
                )
        };

    private static VariableNode.DataType GetDataTypeFromTokenType(Token.TokenType tt) =>
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
        if (
            Peek(1)?.Type
            is Token.TokenType.Assignment
                or Token.TokenType.LeftBracket
                or Token.TokenType.Dot
        )
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

    /// <summary>
    /// Process an arbitrary number of consecutive variables declaration lines (i.e. lines that
    /// start with the 'variables' keyword).
    /// </summary>
    /// <param name="parentModule"></param>
    /// <returns></returns>
    private List<VariableNode> ProcessVariables(string? parentModule)
    {
        var retVal = new List<VariableNode>();

        while (MatchAndRemove(Token.TokenType.Variables) != null)
        {
            var nextOnes = GetVariables(parentModule);

            // TODO: We are potentially adding null to retVal here.
            retVal.AddRange(nextOnes);

            MatchAndRemove(Token.TokenType.EndOfLine);
        }
        return retVal;
    }

    private List<VariableNode> GetVariables(
        List<string> names,
        bool isConstant,
        string parentModuleName
    )
    {
        var typeToken =
            MatchAndRemoveMultiple(_shankTokenTypesPlusIdentifier)
            ?? throw new SyntaxErrorException("Expected type", Peek(0));

        return typeToken.Type switch
        {
            Token.TokenType.Array => GetVariablesArray(names, isConstant, parentModuleName),
            Token.TokenType.Identifier
                => GetVariablesUnknown(
                    names,
                    isConstant,
                    parentModuleName,
                    typeToken.GetValueSafe()
                ),
            _ when _shankTokenTypesMinusArray.Contains(typeToken.Type)
                => GetVariablesBasic(
                    names,
                    isConstant,
                    parentModuleName,
                    GetDataTypeFromTokenType(typeToken.Type)
                ),
            Token.TokenType.RefersTo => GetRefersToVariables(names, isConstant, parentModuleName),
            _ => throw new SyntaxErrorException("Expected a valid type", Peek(0))
        };
    }

    private List<VariableNode> GetRefersToVariables(
        List<string> names,
        bool isConstant,
        string parentModuleName
    )
    {
        Token? t;
        var ret = names
            .Select(
                n =>
                    new VariableNode()
                    {
                        IsConstant = isConstant,
                        Type = VariableNode.DataType.Reference,
                        Name = n,
                        ModuleName = parentModuleName,
                        UnknownType =
                            (t = MatchAndRemove(Token.TokenType.Identifier)) == null
                                ? throw new SyntaxErrorException(
                                    "Could not get reference record type",
                                    Peek(0)
                                )
                                : t.Value,
                    }
            )
            .ToList();
        return ret;
    }

    private List<VariableNode> GetVariablesBasic(
        List<string> names,
        bool isConstant,
        string parentModuleName,
        VariableNode.DataType type,
        string? unknownType = null
    )
    {
        var ret = names
            .Select(
                n =>
                    new VariableNode()
                    {
                        IsConstant = isConstant,
                        Type = type,
                        Name = n,
                        ModuleName = parentModuleName,
                        UnknownType = unknownType,
                    }
            )
            .ToList();
        CheckForRange(ret);
        return ret;
    }

    private List<VariableNode> GetVariablesArray(
        List<string> names,
        bool isConstant,
        string parentModuleName
    )
    {
        // Parse the first part of the declaration.
        var ret = GetVariablesBasic(
            names,
            isConstant,
            parentModuleName,
            VariableNode.DataType.Array
        );

        // Require 'of'.
        RequiresToken(Token.TokenType.Of);

        // Get the array type.
        var arrayType =
            MatchAndRemoveMultiple(_shankTokenTypesMinusArray)
            ?? throw new SyntaxErrorException("Expected a type", Peek(0));

        // Set the array type on all the VariableNodes.
        ret.ForEach(d => d.ArrayType = GetDataTypeFromTokenType(arrayType.Type));

        // Return the VariableNodes.
        return ret;
    }

    private List<VariableNode> GetVariablesUnknown(
        List<string> names,
        bool isConstant,
        string parentModuleName,
        string unknownType
    )
    {
        // Parse the first part of the declaration.
        var ret = GetVariablesBasic(
            names,
            isConstant,
            parentModuleName,
            VariableNode.DataType.Unknown,
            unknownType
        );

        // Get the generic type arguments if they exist.
        List<Token> typeArgs = [];
        if (MatchAndRemoveMultiple(_shankTokenTypesPlusIdentifier) is { } firstArg)
        {
            ParseCommaSeparatedTokens(firstArg, typeArgs, _shankTokenTypesPlusIdentifier);
        }

        // Set the GenericTypeArgs on the VariableNodes to return.
        ret.ForEach(
            vn =>
                vn.GenericTypeArgs =
                    typeArgs.Count > 0
                        ? typeArgs.Select(ta => GetDataTypeFromTokenType(ta.Type)).ToList()
                        : null
        );

        // Return the VariableNodes.
        return ret;
    }

    private List<VariableNode>? GetVariables(string? parentModuleName)
    {
        // TODO: GetVariables is used to parse function definition parameters (the contents of the
        // parentheses that follow a function name) AND the lines immediately following a function
        // signature that start with the 'variables' keyword.
        // It only makes sense for the 'var' keyword to appear in a function definition parameters
        // context, but here we are allowing it regardless of the context.
        var isConstant = MatchAndRemove(Token.TokenType.Var) is null;

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

        return GetVariables(names, isConstant, parentModuleName ?? "default");
    }

    private void RequiresToken(Token.TokenType tokenType)
    {
        if (MatchAndRemove(tokenType) is null)
        {
            throw new SyntaxErrorException("Expected a " + tokenType, Peek(0));
        }
    }

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

    private void CheckForRange(List<VariableNode> retVal)
    {
        if (MatchAndRemove(Token.TokenType.From) is null)
        {
            return;
        }

        var fromNode = ProcessNumericConstant(
            MatchAndRemove(Token.TokenType.Number)
                ?? throw new SyntaxErrorException("Expected a number", Peek(0))
        );
        retVal.ForEach(v => v.From = fromNode);

        RequiresToken(Token.TokenType.To);

        var toNode = ProcessNumericConstant(
            MatchAndRemove(Token.TokenType.Number)
                ?? throw new SyntaxErrorException("Expected a number", Peek(0))
        );
        retVal.ForEach(v => v.To = toNode);
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
                                Type = VariableNode.DataType.Real,
                                IsConstant = true,
                                Name = name.Value ?? "",
                                ModuleName = parentModuleName
                            }
                            : new VariableNode()
                            {
                                InitialValue = node,
                                Type = VariableNode.DataType.Integer,
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
                                Type = VariableNode.DataType.Character,
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
                                    Type = VariableNode.DataType.String,
                                    IsConstant = true,
                                    Name = name.Value ?? "",
                                    ModuleName = parentModuleName
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

    private ASTNode ProcessNumericConstant(Token num)
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
            var vars = GetVariables(parentModuleName);
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
        EnumNode enumNode = new EnumNode(token.Value, parentModuleName, GetEnumElements());
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
