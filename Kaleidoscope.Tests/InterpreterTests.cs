using Kaleidoscope.Codegen;

namespace Kaleidoscope.Tests;

public sealed class InterpreterTests
{
    [Fact]
    public void InterpretCode()
    {
        var interpreter = new Interpreter();
        interpreter.Run("def fact(n) if n < 2 then n else n * fact(n - 1)");

        var (_, result) = interpreter.Run("fact(5)");
        Assert.Equal(120.0, result);

        interpreter.Run("def binary$ 30 (x, y) x * y");
        (_, result) = interpreter.Run("3 + 2 $ 4");
        Assert.Equal(11.0, result);

        interpreter.Run("def unary-(x) 0 - x");
        (_, result) = interpreter.Run("-42");
        Assert.Equal(-42.0, result);

        var exception = Assert.Throws<CodegenException>(
            () => interpreter.Run("def fact(x) x")
        );
        Assert.Equal("Compilation error at 1:1..1:14: invalid redefinition of function fact.", exception.Message);

        exception = Assert.Throws<CodegenException>(
            () => interpreter.Run("f(3)")
        );
        Assert.Equal("Compilation error at 1:1..1:5: unknown function referenced.", exception.Message);

        exception = Assert.Throws<CodegenException>(
            () => interpreter.Run("fact(3, 4)")
        );
        Assert.Equal("Compilation error at 1:1..1:11: incorrect number of arguments passed.", exception.Message);

        exception = Assert.Throws<CodegenException>(
            () => interpreter.Run("def f(x) y")
        );
        Assert.Equal("Compilation error at 1:10..1:11: unknown variable y.", exception.Message);
    }
}