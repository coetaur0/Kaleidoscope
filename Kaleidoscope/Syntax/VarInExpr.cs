namespace Kaleidoscope.Syntax;

/// <summary>
/// A variable definition expression.
/// </summary>
/// <param name="Name">The name of the variable being defined.</param>
/// <param name="Value">The initial value of the variable.</param>
/// <param name="Body">The expression in which the variable is defined.</param>
/// <param name="Range">The expression's source range.</param>
public sealed record VarInExpr(string Name, IExpr Value, IExpr Body, Range Range) : IExpr
{
    public T Accept<T>(IExprVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString() => $"var {Name} = {Value} in {Body}";
}