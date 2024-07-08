using Kaleidoscope.Parser;

namespace Kaleidoscope.Tests;

public sealed class ParserTests
{
    private readonly Parser.Parser _parser = new();

    [Fact]
    public void ParseFunction()
    {
        var function = _parser.ParseItem("def f(x, y) x + y");
        Assert.Equal("def f(x, y) (x + y)", function.ToString());
        function = _parser.ParseItem("def fact(x) if x < 0 then x else x * fact(x - 1)");
        Assert.Equal("def fact(x) if (x < 0) then x else (x * fact((x - 1)))", function.ToString());

        var exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("def (x y) x + y")
        );
        Assert.Equal("Syntax error at 1:5..1:6: expect a function name.", exception.Message);
        exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("def f(x y) x + y")
        );
        Assert.Equal("Syntax error at 1:9..1:10: expect a ')'.", exception.Message);
        exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("def f(x, y x + y")
        );
        Assert.Equal("Syntax error at 1:12..1:13: expect a ')'.", exception.Message);
        exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("def f(x, y)")
        );
        Assert.Equal("Syntax error at 1:12..1:12: expect an expression.", exception.Message);
    }

    [Fact]
    public void ParseExtern()
    {
        var ext = _parser.ParseItem("extern f(x, y)");
        Assert.Equal("f(x, y)", ext.ToString());

        var exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("extern f(x, y) x + y")
        );
        Assert.Equal("Syntax error at 1:16..1:17: unexpected token.", exception.Message);
    }

    [Fact]
    public void ParseIfExpr()
    {
        var expr = _parser.ParseItem("if x < 0 then x + 1 else x - 2");
        Assert.Equal("def __anon_expr() if (x < 0) then (x + 1) else (x - 2)", expr.ToString());

        var exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("if x < 0 x + 1 else x - 2")
        );
        Assert.Equal("Syntax error at 1:10..1:11: expect the 'then' keyword.", exception.Message);
        exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("if x < 0 then x + 1 x - 2")
        );
        Assert.Equal("Syntax error at 1:21..1:22: expect the 'else' keyword.", exception.Message);
        exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("if x < 0 then else x - 2")
        );
        Assert.Equal("Syntax error at 1:15..1:19: expect an expression.", exception.Message);
    }

    [Fact]
    public void ParseForExpr()
    {
        var expr = _parser.ParseItem("for i = 0, i < 5, 1 in print(i)");
        Assert.Equal("def __anon_expr() for i = 0, (i < 5), 1 in print(i)", expr.ToString());
        expr = _parser.ParseItem("for i = 0, i < 5 in print(i)");
        Assert.Equal("def __anon_expr() for i = 0, (i < 5), 1 in print(i)", expr.ToString());

        var exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("for i 0, i < 5, 1 in print(i)")
        );
        Assert.Equal("Syntax error at 1:7..1:8: expect a '='.", exception.Message);
        exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("for i = 0, in print(i)")
        );
        Assert.Equal("Syntax error at 1:12..1:14: expect an expression.", exception.Message);
        exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("for i = 0, i < 5, 1 print(i)")
        );
        Assert.Equal("Syntax error at 1:21..1:26: expect the 'in' keyword.", exception.Message);
    }

    [Fact]
    public void ParseBinaryExpr()
    {
        var expr = _parser.ParseItem("x + 3 * y - 1 < 42");
        Assert.Equal("def __anon_expr() (((x + (3 * y)) - 1) < 42)", expr.ToString());

        var exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("x - 3 *")
        );
        Assert.Equal("Syntax error at 1:8..1:8: expect an expression.", exception.Message);
    }

    [Fact]
    public void ParseCallExpr()
    {
        var expr = _parser.ParseItem("f(42, 1337)");
        Assert.Equal("def __anon_expr() f(42, 1337)", expr.ToString());

        var exception = Assert.Throws<ParseException>(
            () => _parser.ParseItem("f(42, 1337")
        );
        Assert.Equal("Syntax error at 1:11..1:11: expect a ')'.", exception.Message);
    }

    [Fact]
    public void ParseNumberExpr()
    {
        var expr = _parser.ParseItem("42.1337");
        Assert.Equal("def __anon_expr() 42.1337", expr.ToString());
    }

    [Fact]
    public void ParseVariableExpr()
    {
        var expr = _parser.ParseItem("x");
        Assert.Equal("def __anon_expr() x", expr.ToString());
    }
}