using Kaleidoscope.Syntax;
using Range = Kaleidoscope.Syntax.Range;

namespace Kaleidoscope.Parser;

/// <summary>
/// A parser for the Kaleidoscope language.
/// </summary>
public sealed class Parser
{
    /// <summary>
    /// Returns the binary operator and precedence level associated with some token kind.
    /// </summary>
    private static (BinaryOp, int) Precedence(TokenKind kind) => kind switch
    {
        TokenKind.Less => (BinaryOp.LessThan, 1),
        TokenKind.Plus => (BinaryOp.Add, 2),
        TokenKind.Minus => (BinaryOp.Subtract, 2),
        TokenKind.Times => (BinaryOp.Multiply, 3),
        _ => (BinaryOp.Multiply, -1)
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
        Advance();
        var prototype = ParsePrototype();
        var body = ParseExpr();
        return new Function(prototype, body, new Syntax.Range(prototype.Range.Start, body.Range.End));
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
        return new Function(new Prototype("__anon_expr", [], body.Range), body, body.Range);
    }

    /// <summary>
    /// Parses a function prototype.
    /// </summary>
    private Prototype ParsePrototype()
    {
        var name = Consume(TokenKind.Identifier, "expect a function name");

        Consume(TokenKind.LeftParen, "expect a '('");
        var parameters = ParseList(ParseParameter, TokenKind.RightParen);
        var end = Consume(TokenKind.RightParen, "expect a ')'").Range.End;

        return new Prototype(_source[name.Range], parameters, name.Range with { End = end });
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
        return ParseBinary(ParsePrimary(), 0);
    }

    /// <summary>
    /// Parses a binary expression with a given left-hand side and a precedence level greater or equal to the input
    /// precedence.
    /// </summary>
    private IExpr ParseBinary(IExpr lhs, int precedence)
    {
        while (true)
        {
            var (op, opPrecedence) = Precedence(_nextToken.Kind);
            if (opPrecedence < precedence)
            {
                return lhs;
            }

            Advance();
            var rhs = ParsePrimary();

            var (_, nextPrecedence) = Precedence(_nextToken.Kind);
            if (opPrecedence < nextPrecedence)
            {
                rhs = ParseBinary(rhs, opPrecedence + 1);
            }

            lhs = new BinaryExpr(op, lhs, rhs, new Syntax.Range(lhs.Range.Start, rhs.Range.End));
        }
    }

    /// <summary>
    /// Parses a primary expression.
    /// </summary>
    /// <returns></returns>
    private IExpr ParsePrimary()
    {
        switch (_nextToken.Kind)
        {
            case TokenKind.If:
                Advance();
                var condition = ParseExpr();
                Consume(TokenKind.Then, "expect the 'then' keyword");
                var then = ParseExpr();
                Consume(TokenKind.Else, "expect the 'else' keyword");
                var els = ParseExpr();
                return new IfExpr(condition, then, els, new Range(condition.Range.Start, els.Range.End));

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