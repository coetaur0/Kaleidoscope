using System.Text;
using Kaleidoscope.Syntax;

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

    private readonly List<Diagnostic> _diagnostics;

    private bool _panic;

    public Parser()
    {
        _source = new Source(null, "");
        _lexer = new Lexer(_source);
        _nextToken = _lexer.Next()!.Value;
        _diagnostics = new List<Diagnostic>();
        _panic = false;
    }

    /// <summary>
    /// Parses a top-level item from some input string.
    /// </summary>
    public IItem ParseItem(string input)
    {
        Load(input);
        IItem? result = _nextToken.Kind switch
        {
            TokenKind.Def => ParseDefinition(),
            TokenKind.Extern => ParseExtern(),
            _ => ParseTopLevelExpr()
        };

        Consume(TokenKind.Eof, "unexpected token");

        if (result is null || _diagnostics.Count != 0)
        {
            throw new ParseException(WriteError());
        }

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
        _diagnostics.Clear();
        _panic = false;
    }

    /// <summary>
    /// Parses a function definition.
    /// </summary>
    private Function? ParseDefinition()
    {
        Advance();
        var prototype = ParsePrototype();
        if (prototype is null)
        {
            return null;
        }

        var body = ParseExpr();
        return body is null ? null : new Function(prototype, body);
    }

    /// <summary>
    /// Parses an extern function.
    /// </summary>
    private Prototype? ParseExtern()
    {
        Advance();
        return ParsePrototype();
    }

    /// <summary>
    /// Parses a top-level expression.
    /// </summary>
    private Function? ParseTopLevelExpr()
    {
        var body = ParseExpr();
        return body is null ? null : new Function(new Prototype("__anon_expr", []), body);
    }

    /// <summary>
    /// Parses a function prototype.
    /// </summary>
    private Prototype? ParsePrototype()
    {
        var name = Consume(TokenKind.Identifier, "expect a function name");
        if (name is null)
        {
            return null;
        }

        Consume(TokenKind.LeftParen, "expect a '('");
        var parameters = ParseList(ParseParameter, TokenKind.RightParen);
        Consume(TokenKind.RightParen, "expect a ')'");

        return new Prototype(_source[name.Value.Range], parameters);
    }

    /// <summary>
    /// Parses a function parameter.
    /// </summary>
    private string? ParseParameter()
    {
        var name = Consume(TokenKind.Identifier, "expect a parameter name");
        return name is null ? null : _source[name.Value.Range];
    }

    /// <summary>
    /// Parses an expression.
    /// </summary>
    private IExpr? ParseExpr()
    {
        var lhs = ParsePrimary();
        return lhs is null ? null : ParseBinary(lhs, 0);
    }

    /// <summary>
    /// Parses a binary expression with a given left-hand side and a precedence level greater or equal to the input
    /// precedence.
    /// </summary>
    private IExpr? ParseBinary(IExpr lhs, int precedence)
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
            if (rhs is null)
            {
                return null;
            }

            var (_, nextPrecedence) = Precedence(_nextToken.Kind);
            if (opPrecedence < nextPrecedence)
            {
                rhs = ParseBinary(rhs, opPrecedence + 1);
                if (rhs is null)
                {
                    return null;
                }
            }

            lhs = new BinaryExpr(op, lhs, rhs);
        }
    }

    /// <summary>
    /// Parses a primary expression.
    /// </summary>
    /// <returns></returns>
    private IExpr? ParsePrimary()
    {
        switch (_nextToken.Kind)
        {
            case TokenKind.Identifier:
                return ParseIdentifier();

            case TokenKind.Number:
                return new NumberExpr(double.Parse(_source[Advance().Range]));

            case TokenKind.LeftParen:
                Advance();
                var expr = ParseExpr();
                Consume(TokenKind.RightParen, "expect a ')'");
                return expr;

            default:
                EmitDiagnostic("expect an expression", _nextToken.Range);
                return null;
        }
    }

    /// <summary>
    /// Parses a variable or call expression.
    /// </summary>
    private IExpr ParseIdentifier()
    {
        var name = _source[Advance().Range];

        if (_nextToken.Kind != TokenKind.LeftParen)
        {
            return new VariableExpr(name);
        }

        Advance();
        var args = ParseList(ParseExpr, TokenKind.RightParen);

        Consume(TokenKind.RightParen, "expect a ')'");

        return new CallExpr(name, args);
    }

    /// <summary>
    /// Parses a comma-separated list of items using some parse function.
    /// </summary>
    private List<T> ParseList<T>(Func<T?> parse, TokenKind end)
    {
        var result = new List<T>();
        TokenKind[] sync = [TokenKind.Comma, end];

        while (_nextToken.Kind != TokenKind.Eof && _nextToken.Kind != end)
        {
            var item = parse();
            if (item is not null)
            {
                result.Add(item);
            }

            Synchronise(sync);

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
    private Token? Consume(TokenKind kind, string message)
    {
        if (_nextToken.Kind == kind)
        {
            return Advance();
        }

        EmitDiagnostic(message, _nextToken.Range);
        return null;
    }

    /// <summary>
    /// Emits an error diagnostic with some given message and source range. 
    /// </summary>
    private void EmitDiagnostic(string message, Range range)
    {
        if (_panic)
        {
            return;
        }

        _panic = true;
        _diagnostics.Add(new Diagnostic(message, range));
    }

    /// <summary>
    /// Synchronises the parser at the next token of one of the specified kinds.
    /// </summary>
    private void Synchronise(TokenKind[] kinds)
    {
        if (!_panic)
        {
            return;
        }

        while (_nextToken.Kind != TokenKind.Eof && !kinds.Contains(_nextToken.Kind))
        {
            Advance();
        }

        _panic = false;
    }

    /// <summary>
    /// Writes the list of diagnostics encountered during parsing in an error message.
    /// </summary>
    /// <returns></returns>
    private string WriteError()
    {
        var str = new StringBuilder($"Syntax errors in {_source.Path ?? "input"}:");

        foreach (var diagnostic in _diagnostics)
        {
            str.Append($"\n\t- {diagnostic}.");
        }

        return str.ToString();
    }
}