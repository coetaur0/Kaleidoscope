namespace Kaleidoscope.Syntax;

/// <summary>
/// A variable expression.
/// </summary>
/// <param name="Name">The variable's name.</param>
/// <param name="Range">The variable's source range.</param>
public sealed record VariableExpr(string Name, Range Range) : IExpr
{
    public override string ToString() => $"{Name}";
}