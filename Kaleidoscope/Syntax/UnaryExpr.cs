namespace Kaleidoscope.Syntax;

/// <summary>
/// A unary expression.
/// </summary>
/// <param name="Op">The expression's operator.</param>
/// <param name="Operand">The expression's operand.</param>
/// <param name="Range">The expression's source range.</param>
public sealed record UnaryExpr(string Op, IExpr Operand, Range Range) : IExpr
{
    public T Accept<T>(IExprVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString() => $"{Op}{Operand}";
}