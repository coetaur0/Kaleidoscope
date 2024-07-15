using Kaleidoscope.Syntax;

namespace Kaleidoscope.Parser;

/// <summary>
/// A parser for the Kaleidoscope language.
/// </summary>
public sealed class Parser
{
    private readonly Dictionary<string, int> _precedence = new()
    {
        ["="] = 2,
        ["<"] = 10,
        ["+"] = 20,
        ["-"] = 20,
        ["*"] = 30
    };

    private readonly Source _source;

    private readonly Lexer _lexer;

    private Token _nextToken;

    public Parser()
    {
        _source = new Source("");
        _lexer = new Lexer(_source);
        _nextToken = _lexer.Next()!.Value;
    }

    /// <summary>
    /// Parses a top-level item from some input string.
    /// </summary>
    public IItem ParseItem(string input)
    {
        Load(input);
        IItem result = _nextToken.Kind switch
        {
            TokenKind.Def => ParseDefinition(),
            TokenKind.Extern => ParseExtern(),
            _ => ParseTopLevelExpr()
        };
        Consume(TokenKind.Eof, "unexpected token");
        return result;
    }

    /// <summary>
    /// Loads a new input for the parser.
    /// </summary>
    private void Load(string input)
    {
        _source.Contents = input;
        _lexer.Reset();
        _nextToken = _lexer.Next()!.Value;
    }

    /// <summary>
    /// Parses a function definition.
    /// </summary>
    private Function ParseDefinition()
    {
        var start = Advance().Range.Start;
        var prototype = ParsePrototype();
        var body = ParseExpr();
        return new Function(prototype, body, new Syntax.Range(start, body.Range.End));
    }

    /// <summary>
    /// Parses an extern function.
    /// </summary>
    private Prototype ParseExtern()
    {
        Advance();
        return ParsePrototype();
    }

    /// <summary>
    /// Parses a top-level expression.
    /// </summary>
    private Function ParseTopLevelExpr()
    {
        var body = ParseExpr();
        return new Function(new Prototype("__anon_expr", [], false, 40, body.Range), body, body.Range);
    }

    /// <summary>
    /// Parses a function prototype.
    /// </summary>
    private Prototype ParsePrototype()
    {
        var start = _nextToken.Range;
        var precedence = 30;

        string name;
        int kind;
        switch (_nextToken.Kind)
        {
            case TokenKind.Identifier:
                Advance();
                name = _source[start];
                kind = 0;
                break;

            case TokenKind.Binary:
                Advance();
                var binOp = Consume(TokenKind.Op, "expect a binary operator").Range;
                name = $"binary{_source[binOp]}";
                kind = 2;
                if (_nextToken.Kind == TokenKind.Number)
                {
                    precedence = Convert.ToInt32(_source[Advance().Range]);
                }

                _precedence[_source[binOp]] = precedence;

                break;

            case TokenKind.Unary:
                Advance();
                var unOp = Consume(TokenKind.Op, "expect a unary operator").Range;
                name = $"unary{_source[unOp]}";
                kind = 1;
                break;

            default:
                throw Exception("expect a function or operator declaration", _nextToken.Range);
        }

        var paramStart = Consume(TokenKind.LeftParen, "expect a '('").Range.Start;
        var parameters = ParseList(ParseParameter, TokenKind.RightParen);
        var end = Consume(TokenKind.RightParen, "expect a ')'").Range.End;

        if (kind != 0 && parameters.Count != kind)
        {
            throw Exception("invalid number of operands for operator", new Syntax.Range(paramStart, end));
        }

        return new Prototype(name, parameters, kind != 0, precedence, start with { End = end });
    }

    /// <summary>
    /// Parses a function parameter.
    /// </summary>
    private string ParseParameter()
    {
        return _source[Consume(TokenKind.Identifier, "expect a parameter name").Range];
    }

    /// <summary>
    /// Parses an expression.
    /// </summary>
    private IExpr ParseExpr()
    {
        return ParseBinary(ParseUnary(), 0);
    }

    /// <summary>
    /// Parses a binary expression with a given left-hand side and a precedence level greater or equal to the input
    /// precedence.
    /// </summary>
    private IExpr ParseBinary(IExpr lhs, int precedence)
    {
        while (true)
        {
            var opPrecedence = _precedence.GetValueOrDefault(_source[_nextToken.Range], -1);
            if (opPrecedence < precedence)
            {
                return lhs;
            }

            var op = _source[Advance().Range];
            var rhs = ParseUnary();

            var nextPrecedence = _precedence.GetValueOrDefault(_source[_nextToken.Range], -1);
            if (opPrecedence < nextPrecedence)
            {
                rhs = ParseBinary(rhs, opPrecedence + 1);
            }

            lhs = new BinaryExpr(op, lhs, rhs, new Syntax.Range(lhs.Range.Start, rhs.Range.End));
        }
    }

    /// <summary>
    /// Parses a unary expression.
    /// </summary>
    private IExpr ParseUnary()
    {
        if (_nextToken.Kind != TokenKind.Op)
        {
            return ParsePrimary();
        }

        var start = Advance().Range;
        var op = _source[start];
        var operand = ParseUnary();
        return new UnaryExpr(op, operand, operand.Range with { Start = start.Start });
    }

    /// <summary>
    /// Parses a primary expression.
    /// </summary>
    private IExpr ParsePrimary()
    {
        switch (_nextToken.Kind)
        {
            case TokenKind.If:
                var ifStart = Advance().Range.Start;
                var condition = ParseExpr();
                Consume(TokenKind.Then, "expect the 'then' keyword");
                var then = ParseExpr();
                Consume(TokenKind.Else, "expect the 'else' keyword");
                var els = ParseExpr();
                return new IfExpr(condition, then, els, els.Range with { Start = ifStart });

            case TokenKind.For:
                var forStart = Advance().Range.Start;
                var forVarName = _source[Consume(TokenKind.Identifier, "expect a variable name").Range];
                if (_nextToken.Kind != TokenKind.Op && _source[_nextToken.Range] != "=")
                {
                    throw Exception("expect a '='", _nextToken.Range);
                }

                Advance();
                var start = ParseExpr();
                Consume(TokenKind.Comma, "expect a ','");
                var end = ParseExpr();

                IExpr step = new NumberExpr(1, _nextToken.Range);
                if (_nextToken.Kind == TokenKind.Comma)
                {
                    Advance();
                    step = ParseExpr();
                }

                Consume(TokenKind.In, "expect the 'in' keyword");
                var forBody = ParseExpr();
                return new ForExpr(forVarName, start, end, step, forBody, forBody.Range with { Start = forStart });

            case TokenKind.Var:
                var varStart = Advance().Range.Start;
                var varName = _source[Consume(TokenKind.Identifier, "expect a variable name").Range];
                if (_nextToken.Kind != TokenKind.Op && _source[_nextToken.Range] != "=")
                {
                    throw Exception("expect a '='", _nextToken.Range);
                }

                Advance();
                var value = ParseExpr();
                Consume(TokenKind.In, "expect the 'in' keyword");
                var body = ParseExpr();
                return new VarInExpr(varName, value, body, body.Range with { Start = varStart });

            case TokenKind.Identifier:
                return ParseIdentifier();

            case TokenKind.Number:
                var range = Advance().Range;
                return new NumberExpr(double.Parse(_source[range]), range);

            case TokenKind.LeftParen:
                Advance();
                var expr = ParseExpr();
                Consume(TokenKind.RightParen, "expect a ')'");
                return expr;

            default:
                throw Exception("expect an expression", _nextToken.Range);
        }
    }

    /// <summary>
    /// Parses a variable or call expression.
    /// </summary>
    private IExpr ParseIdentifier()
    {
        var range = Advance().Range;
        var name = _source[range];

        if (_nextToken.Kind != TokenKind.LeftParen)
        {
            return new VariableExpr(name, range);
        }

        Advance();
        var args = ParseList(ParseExpr, TokenKind.RightParen);

        var end = Consume(TokenKind.RightParen, "expect a ')'").Range.End;

        return new CallExpr(name, args, range with { End = end });
    }

    /// <summary>
    /// Parses a comma-separated list of items using some parse function.
    /// </summary>
    private List<T> ParseList<T>(Func<T> parse, TokenKind end)
    {
        var result = new List<T>();

        while (_nextToken.Kind != TokenKind.Eof && _nextToken.Kind != end)
        {
            result.Add(parse());

            if (_nextToken.Kind == TokenKind.Comma)
            {
                Advance();
            }
            else
            {
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Advances the parser's position in the token stream.
    /// </summary>
    private Token Advance()
    {
        var token = _nextToken;

        if (_nextToken.Kind != TokenKind.Eof)
        {
            _nextToken = _lexer.Next()!.Value;
        }

        return token;
    }

    /// <summary>
    /// Consumes the next token in the lexer's stream if it is of the specified kind, or emits an error diagnostic with
    /// some message otherwise.
    /// </summary>
    private Token Consume(TokenKind kind, string message)
    {
        if (_nextToken.Kind == kind)
        {
            return Advance();
        }

        throw Exception(message, _nextToken.Range);
    }

    /// <summary>
    /// Creates a new parse exception with a given message and source range.
    /// </summary>
    private static ParseException Exception(string message, Syntax.Range range)
    {
        return new ParseException($"Syntax error at {range}: {message}.");
    }
}