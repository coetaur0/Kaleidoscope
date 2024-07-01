namespace Kaleidoscope.Syntax;

/// <summary>
/// A number literal expression.
/// </summary>
/// <param name="Value">The literal's value.</param>
/// <param name="Range">The literal's source range.</param>
public sealed record NumberExpr(double Value, Range Range) : IExpr
{
    public T Accept<T>(IExprVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString() => $"{Value}";
}