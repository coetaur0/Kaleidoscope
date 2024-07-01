using System.Text;

namespace Kaleidoscope.Syntax;

/// <summary>
/// A binary operator.
/// </summary>
public enum BinaryOp
{
    LessThan,
    Add,
    Subtract,
    Multiply
}

/// <summary>
/// A binary expression.
/// </summary>
/// <param name="Op">The expression's operator.</param>
/// <param name="Lhs">The expression's left-hand side.</param>
/// <param name="Rhs">The expression's right-hand side.</param>
/// <param name="Range">The expression's source range.</param>
public sealed record BinaryExpr(BinaryOp Op, IExpr Lhs, IExpr Rhs, Range Range) : IExpr
{
    public T Accept<T>(IExprVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString()
    {
        var str = new StringBuilder($"({Lhs} ");
        var op = Op switch
        {
            BinaryOp.LessThan => "<",
            BinaryOp.Add => "+",
            BinaryOp.Subtract => "-",
            BinaryOp.Multiply => "*",
            _ => "?"
        };
        str.Append($"{op} {Rhs})");
        return str.ToString();
    }
}