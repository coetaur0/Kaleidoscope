namespace Kaleidoscope.Syntax;

/// <summary>
/// A function definition.
/// </summary>
/// <param name="Prototype">The function's prototype.</param>
/// <param name="Body">The function's body.</param>
public sealed record Function(Prototype Prototype, IExpr Body) : IItem
{
    public override string ToString() => $"def {Prototype} {Body}";
}