namespace Kaleidoscope.Syntax;

/// <summary>
/// A variable expression.
/// </summary>
/// <param name="Name">The variable's name.</param>
public sealed record VariableExpr(string Name) : IExpr
{
    public override string ToString() => $"{Name}";
}