namespace Kaleidoscope.Syntax;

/// <summary>
/// A conditional expression.
/// </summary>
/// <param name="Condition">The expression's condition.</param>
/// <param name="Then">The expression's then arm.</param>
/// <param name="Else">The expression's else arm.</param>
/// <param name="Range">The expression's source range.</param>
public sealed record IfExpr(IExpr Condition, IExpr Then, IExpr Else, Range Range) : IExpr
{
    public T Accept<T>(IExprVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString() => $"if {Condition} then {Then} else {Else}";
}