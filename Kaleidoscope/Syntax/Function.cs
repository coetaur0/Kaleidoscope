namespace Kaleidoscope.Syntax;

/// <summary>
/// A function definition.
/// </summary>
/// <param name="Prototype">The function's prototype.</param>
/// <param name="Body">The function's body.</param>
/// <param name="Range">The function's source range.</param>
public sealed record Function(Prototype Prototype, IExpr Body, Range Range) : IItem
{
    public override string ToString() => $"def {Prototype} {Body}";
}