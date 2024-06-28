namespace Kaleidoscope.Syntax;

/// <summary>
/// A number literal expression.
/// </summary>
/// <param name="Value">The literal's value.</param>
public sealed record NumberExpr(double Value) : IExpr
{
    public override string ToString() => $"{Value}";
}